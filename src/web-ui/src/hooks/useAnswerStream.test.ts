// @vitest-environment jsdom
import { cleanup, renderHook, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, streamAnswer, type SearchRequest } from '@/api/client';
import { useAnswerStream } from './useAnswerStream';

vi.mock('@/api/client', async (importOriginal) => {
    const actual = await importOriginal<typeof import('@/api/client')>();
    return { ...actual, streamAnswer: vi.fn() };
});

const streamAnswerMock = vi.mocked(streamAnswer);

const request: SearchRequest = { query: 'power adapter for my laptop', pageSize: 50 };

const final = (section: string, markdown: string, invalidCitations: string[] = []) =>
    `event: final\ndata: ${JSON.stringify({ section, markdown, citations: ['PROD-0012'], invalidCitations, insufficientEvidence: false, warnings: [], structure: null })}\n\n`;

/** A streamed response whose body arrives in the given chunks, split wherever the test likes. */
function eventStream(chunks: string[]): Response {
    const encoder = new TextEncoder();
    const body = new ReadableStream<Uint8Array>({
        start(controller) {
            for (const chunk of chunks) {
                controller.enqueue(encoder.encode(chunk));
            }
            controller.close();
        },
    });

    return new Response(body, { headers: { 'Content-Type': 'text/event-stream' } });
}

afterEach(() => {
    cleanup();
    streamAnswerMock.mockReset();
});

describe('useAnswerStream', () => {
    it('is idle on Stages 1–5 and never calls the answer endpoint', () => {
        const { result } = renderHook(() => useAnswerStream('ontology', request));

        expect(result.current.status).toBe('idle');
        expect(streamAnswerMock).not.toHaveBeenCalled();
    });

    it('accumulates deltas, applies each final and finishes on done', async () => {
        streamAnswerMock.mockResolvedValue(
            eventStream([
                'event: meta\ndata: {"stage":"pedagogy","provider":"ollama","model":"qwen3.6:35b","evidence":["PROD-0012"]}\n\n',
                'event: delta\ndata: {"section":"answer","text":"Get the "}\n\nevent: del',
                'ta\ndata: {"section":"answer","text":"charger [PROD-0012]."}\n\n',
                final('answer', 'Get the charger [PROD-0012].'),
                'event: delta\ndata: {"section":"explanation","text":"## Decision"}\n\n',
                final('explanation', '## Decision', ['PROD-0099']),
                'event: done\ndata: {"timeToFirstTokenMs":59,"totalMs":6009,"trace":[]}\n\n',
            ]),
        );

        const { result } = renderHook(() => useAnswerStream('pedagogy', request));

        await waitFor(() => expect(result.current.status).toBe('done'));
        expect(result.current.meta?.model).toBe('qwen3.6:35b');
        expect(result.current.sections.answer.markdown).toBe('Get the charger [PROD-0012].');
        expect(result.current.sections.answer.final?.citations).toEqual(['PROD-0012']);
        expect(result.current.sections.explanation.final?.invalidCitations).toEqual(['PROD-0099']);
        expect(result.current.done?.timeToFirstTokenMs).toBe(59);
        expect(result.current.activeSection).toBeNull();
        expect(streamAnswerMock).toHaveBeenCalledWith('pedagogy', request, expect.any(AbortSignal));
    });

    it('reports an error event, such as the LLM stopping mid-stream, with its ProblemDetails', async () => {
        streamAnswerMock.mockResolvedValue(
            eventStream([
                'event: delta\ndata: {"section":"answer","text":"Get"}\n\n',
                'event: error\ndata: {"status":503,"title":"LLM unavailable","detail":"Is Ollama running?"}\n\n',
            ]),
        );

        const { result } = renderHook(() => useAnswerStream('rag', request));

        await waitFor(() => expect(result.current.status).toBe('error'));
        expect(result.current.error).toBeInstanceOf(ApiError);
        expect(result.current.error?.message).toBe('Is Ollama running?');
        expect(result.current.sections.answer.markdown).toBe('Get');
    });

    it('reports a stream that closes without done', async () => {
        streamAnswerMock.mockResolvedValue(
            eventStream(['event: delta\ndata: {"section":"answer","text":"Get"}\n\n']),
        );

        const { result } = renderHook(() => useAnswerStream('rag', request));

        await waitFor(() => expect(result.current.status).toBe('error'));
        expect(result.current.error?.message).toMatch(/closed before it finished/);
    });

    it('aborts the stream when the stage changes, and starts again with nothing from the old one', async () => {
        const signals: AbortSignal[] = [];
        streamAnswerMock.mockImplementation((_stage, _request, signal) => {
            if (signal !== undefined) {
                signals.push(signal);
            }
            // Never finishes, like a model still writing.
            return Promise.resolve(new Response(new ReadableStream<Uint8Array>()));
        });

        const { result, rerender } = renderHook(({ stage }) => useAnswerStream(stage, request), {
            initialProps: { stage: 'rag' as const as 'rag' | 'pedagogy' },
        });
        await waitFor(() => expect(signals).toHaveLength(1));

        rerender({ stage: 'pedagogy' });

        await waitFor(() => expect(signals).toHaveLength(2));
        expect(signals[0]?.aborted).toBe(true);
        expect(signals[1]?.aborted).toBe(false);
        expect(result.current.status).toBe('streaming');
        expect(result.current.sections.answer.markdown).toBe('');
    });
});
