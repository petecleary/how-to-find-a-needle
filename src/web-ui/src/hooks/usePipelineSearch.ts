import { useCallback, useEffect, useState } from 'react';
import { search, type SearchRequest, type SearchResponse, type SearchStage } from '@/api/client';

// usePipelineSearch — the experiment's one moving part: the same request, sent to whichever stage is
// selected. Switching stage re-runs the search with identical input, so any difference on screen comes
// from the technique, not the query (ADR-0003, ADR-0014 § State and data flow).
//
// Plain React state, no data-fetching library: the whole lifecycle (start, cancel, finish, fail) is
// visible in this file.

export type PipelineSearchStatus = 'idle' | 'loading' | 'success' | 'error';

export interface PipelineSearch {
    stage: SearchStage;
    request: SearchRequest;
    /** `idle` until there is something to search for; `loading` while the current request is in flight. */
    status: PipelineSearchStatus;
    /** The response to the current stage and request; `null` until it arrives. */
    response: SearchResponse | null;
    /** An `ApiError` carries the API's ProblemDetails, e.g. a 503's fix-it guidance. */
    error: Error | null;
    /** Sends the same search again, e.g. after fixing whatever caused a 503. */
    rerun: () => void;
}

// The outcome remembers which search it answers, so a slow answer to an old search is never shown
// for the new one.
type Outcome =
    { key: string; response: SearchResponse; error: null } | { key: string; response: null; error: Error };

/**
 * Ranked stages (2–7) return every page of what they retrieved, joined into one response, so no flagged item is
 * left on a page the screen never asks for. Hybrid fusion returns the union of the keyword and vector lists, so
 * Stage 5 can hold more than one page of 50: for GQ-08 in the 300-product catalog, its flagged chargers rank
 * 52nd to 67th. Stage 1 is left alone: its `totalResults` counts the whole filtered catalog, and it pages in SQL.
 */
async function searchEveryCandidate(
    stage: SearchStage,
    request: SearchRequest,
    signal: AbortSignal,
): Promise<SearchResponse> {
    const first = await search(stage, request, signal);
    if (stage === 'structured') {
        return first;
    }

    const results = [...first.results];
    for (let page = 2; results.length < first.totalResults; page++) {
        const next = await search(stage, { ...request, page }, signal);
        if (next.results.length === 0) {
            break;
        }
        results.push(...next.results);
    }

    return { ...first, results };
}

export function usePipelineSearch(stage: SearchStage, request: SearchRequest): PipelineSearch {
    const [attempt, setAttempt] = useState(0);
    const [outcome, setOutcome] = useState<Outcome | null>(null);

    // Keyed on the request's JSON rather than the object, so a caller that builds an equal request on
    // every render doesn't trigger a new search, but any changed value does.
    const requestJson = JSON.stringify(request);
    const key = `${stage}|${attempt}|${requestJson}`;

    // Stage 1 filters without a query; every other stage's validator rejects an empty one with a 400.
    const canSearch = stage === 'structured' || (request.query ?? '').trim() !== '';

    useEffect(() => {
        if (!canSearch) {
            return;
        }

        const controller = new AbortController();
        const body = JSON.parse(requestJson) as SearchRequest;

        searchEveryCandidate(stage, body, controller.signal).then(
            (response) => {
                if (!controller.signal.aborted) {
                    setOutcome({ key, response, error: null });
                }
            },
            (error: unknown) => {
                // An aborted search was cancelled on purpose (the stage or request changed): not an error to show.
                if (!controller.signal.aborted) {
                    setOutcome({
                        key,
                        response: null,
                        error: error instanceof Error ? error : new Error(String(error)),
                    });
                }
            },
        );

        // Runs when the stage or request changes, or the screen closes: cancel the search in flight.
        return () => controller.abort();
    }, [canSearch, key, requestJson, stage]);

    const rerun = useCallback(() => setAttempt((previous) => previous + 1), []);

    const current = canSearch && outcome?.key === key ? outcome : null;
    let status: PipelineSearchStatus;
    if (!canSearch) {
        status = 'idle';
    } else if (current === null) {
        status = 'loading';
    } else {
        status = current.error === null ? 'success' : 'error';
    }

    return {
        stage,
        request,
        status,
        response: current?.response ?? null,
        error: current?.error ?? null,
        rerun,
    };
}
