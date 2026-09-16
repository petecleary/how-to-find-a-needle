import { useCallback, useEffect, useReducer, useState } from 'react';
import {
    isAnswerSection,
    toAnswerEvent,
    type AnswerDone,
    type AnswerEvent,
    type AnswerMeta,
    type AnswerSection,
} from '@/api/answerEvents';
import {
    ApiError,
    isAnswerStage,
    streamAnswer,
    type AnswerFinal,
    type AnswerStage,
    type SearchRequest,
    type SearchStage,
} from '@/api/client';
import { createSseParser } from '@/lib/parseSseEvents';

// useAnswerStream — Stages 6–7's LLM text, streamed from /api/search/{stage}/answer while usePipelineSearch fetches the
// results with the same request, in parallel: results never wait for the LLM (ADR-0016). Text is shown as it arrives,
// and each section's `final` event then brings the API's validation: citations, warnings and Stage 7's structure.
//
// fetch with a hand-written SSE parser, because EventSource can only GET. Otherwise the same pattern as
// usePipelineSearch: changing the stage or the request aborts the stream, and the API stops the model (ADR-0015).

export type AnswerStreamStatus = 'idle' | 'streaming' | 'done' | 'error';

export interface AnswerSectionState {
    /** The text so far; after `final`, exactly what the API validated. */
    markdown: string;
    final: AnswerFinal | null;
}

export interface AnswerStream {
    /** `idle` for Stages 1–5 or without a query; `streaming` from the request until `done` or an error. */
    status: AnswerStreamStatus;
    meta: AnswerMeta | null;
    sections: Record<AnswerSection, AnswerSectionState>;
    /** The section whose text is arriving now; `null` before the first chunk and after the stream ends. */
    activeSection: AnswerSection | null;
    done: AnswerDone | null;
    /** An `ApiError` carries the API's ProblemDetails, e.g. the 503 guidance when the LLM isn't running. */
    error: Error | null;
    /** Streams the same answer again, e.g. after starting Ollama. */
    rerun: () => void;
}

interface StreamState {
    key: string;
    meta: AnswerMeta | null;
    sections: Record<AnswerSection, AnswerSectionState>;
    activeSection: AnswerSection | null;
    done: AnswerDone | null;
    error: Error | null;
}

type StreamAction = { type: 'event'; event: AnswerEvent } | { type: 'failed'; error: Error };

function freshState(key: string): StreamState {
    return {
        key,
        meta: null,
        sections: { answer: { markdown: '', final: null }, explanation: { markdown: '', final: null } },
        activeSection: null,
        done: null,
        error: null,
    };
}

// Every action names the stream it belongs to. An action for a different stream starts from a fresh state, so text
// from an earlier stage or query is never shown under the new one.
function reducer(state: StreamState, action: StreamAction & { key: string }): StreamState {
    const current = state.key === action.key ? state : freshState(action.key);

    if (action.type === 'failed') {
        return { ...current, activeSection: null, error: action.error };
    }

    const { event } = action;
    switch (event.name) {
        case 'meta':
            return { ...current, meta: event.data };
        case 'delta': {
            const { section, text } = event.data;
            const previous = current.sections[section];
            return {
                ...current,
                activeSection: section,
                sections: {
                    ...current.sections,
                    [section]: { ...previous, markdown: previous.markdown + text },
                },
            };
        }
        case 'final': {
            const { section } = event.data;
            if (!isAnswerSection(section)) {
                return current;
            }
            return {
                ...current,
                sections: {
                    ...current.sections,
                    [section]: { markdown: event.data.markdown, final: event.data },
                },
            };
        }
        case 'done':
            return { ...current, activeSection: null, done: event.data };
        case 'error':
            return {
                ...current,
                activeSection: null,
                error: new ApiError(event.data.status ?? 500, '', event.data, []),
            };
    }
}

export function useAnswerStream(stage: SearchStage, request: SearchRequest): AnswerStream {
    const [attempt, setAttempt] = useState(0);
    const [state, dispatch] = useReducer(reducer, '', freshState);

    // Keyed like usePipelineSearch: an equal request doesn't restart the stream, but any changed value does.
    const requestJson = JSON.stringify(request);
    const key = `${stage}|${attempt}|${requestJson}`;
    const canStream = isAnswerStage(stage) && (request.query ?? '').trim() !== '';

    useEffect(() => {
        if (!canStream || !isAnswerStage(stage)) {
            return;
        }

        const controller = new AbortController();
        const body = JSON.parse(requestJson) as SearchRequest;

        void readAnswerStream(stage, body, controller.signal, (action) => {
            // Events from a stream that has been replaced are dropped, never shown.
            if (!controller.signal.aborted) {
                dispatch({ ...action, key });
            }
        });

        // Runs when the stage or request changes, or the screen closes: stop reading, and the API stops generating.
        return () => controller.abort();
    }, [canStream, key, requestJson, stage]);

    const rerun = useCallback(() => setAttempt((previous) => previous + 1), []);

    const current = canStream && state.key === key ? state : freshState(key);
    let status: AnswerStreamStatus;
    if (!canStream) {
        status = 'idle';
    } else if (current.error !== null) {
        status = 'error';
    } else if (current.done !== null) {
        status = 'done';
    } else {
        status = 'streaming';
    }

    return {
        status,
        meta: current.meta,
        sections: current.sections,
        activeSection: current.activeSection,
        done: current.done,
        error: current.error,
        rerun,
    };
}

async function readAnswerStream(
    stage: AnswerStage,
    request: SearchRequest,
    signal: AbortSignal,
    send: (action: StreamAction) => void,
): Promise<void> {
    try {
        const response = await streamAnswer(stage, request, signal);
        if (response.body === null) {
            throw new Error('The answer response had no body to read.');
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        const parser = createSseParser();
        let hasFinished = false;

        for (;;) {
            const { done, value } = await reader.read();
            if (done) {
                break;
            }

            for (const sse of parser.push(decoder.decode(value, { stream: true }))) {
                const event = toAnswerEvent(sse.event, sse.data);
                // A malformed or unknown event is skipped: a new kind of event shouldn't break the panel.
                if (event === null) {
                    continue;
                }

                hasFinished ||= event.name === 'done' || event.name === 'error';
                send({ type: 'event', event });
            }
        }

        if (!hasFinished) {
            send({
                type: 'failed',
                error: new Error(
                    'The answer stream closed before it finished. Is `aspire run` still running?',
                ),
            });
        }
    } catch (error: unknown) {
        send({ type: 'failed', error: error instanceof Error ? error : new Error(String(error)) });
    }
}
