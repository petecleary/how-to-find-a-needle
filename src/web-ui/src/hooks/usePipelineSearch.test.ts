// @vitest-environment jsdom
import { act, cleanup, renderHook, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, search, type SearchRequest, type SearchResponse, type SearchStage } from '@/api/client';
import { usePipelineSearch } from './usePipelineSearch';

vi.mock('@/api/client', async (importOriginal) => {
    const actual = await importOriginal<typeof import('@/api/client')>();
    return { ...actual, search: vi.fn() };
});

const searchMock = vi.mocked(search);

const request: SearchRequest = { query: 'power adapter for my laptop', pageSize: 50 };

function responseFor(stage: SearchStage): SearchResponse {
    return {
        stage,
        query: 'power adapter for my laptop',
        page: 1,
        pageSize: 50,
        totalResults: 0,
        executionTimeMs: 10,
        results: [],
        debugTrace: { steps: [] },
    };
}

/** A promise the test resolves by hand, to control which search answers first. */
function deferred<T>() {
    let resolve: (value: T) => void = () => {};
    const promise = new Promise<T>((settle) => {
        resolve = settle;
    });
    return { promise, resolve };
}

afterEach(() => {
    cleanup();
    searchMock.mockReset();
});

describe('usePipelineSearch', () => {
    it('searches the stage and returns its response', async () => {
        searchMock.mockResolvedValue(responseFor('hybrid'));

        const { result } = renderHook(() => usePipelineSearch('hybrid', request));

        expect(result.current.status).toBe('loading');
        await waitFor(() => expect(result.current.status).toBe('success'));
        expect(result.current.response?.stage).toBe('hybrid');
        expect(searchMock).toHaveBeenCalledWith('hybrid', request, expect.any(AbortSignal));
    });

    it('re-runs the same request when the stage changes', async () => {
        searchMock.mockImplementation((stage) => Promise.resolve(responseFor(stage)));
        const { result, rerender } = renderHook(({ stage }) => usePipelineSearch(stage, request), {
            initialProps: { stage: 'keyword' as SearchStage },
        });
        await waitFor(() => expect(result.current.status).toBe('success'));

        rerender({ stage: 'vector' });

        await waitFor(() => expect(result.current.response?.stage).toBe('vector'));
        expect(searchMock.mock.calls.map(([stage]) => stage)).toEqual(['keyword', 'vector']);
        expect(searchMock.mock.calls[1]?.[1]).toEqual(searchMock.mock.calls[0]?.[1]);
    });

    it('cancels the previous search when the stage changes, and ignores its late answer', async () => {
        const keywordSearch = deferred<SearchResponse>();
        const vectorSearch = deferred<SearchResponse>();
        searchMock.mockReturnValueOnce(keywordSearch.promise).mockReturnValueOnce(vectorSearch.promise);
        const { result, rerender } = renderHook(({ stage }) => usePipelineSearch(stage, request), {
            initialProps: { stage: 'keyword' as SearchStage },
        });

        rerender({ stage: 'vector' });
        await act(async () => vectorSearch.resolve(responseFor('vector')));
        await act(async () => keywordSearch.resolve(responseFor('keyword')));

        expect(searchMock.mock.calls[0]?.[2]?.aborted).toBe(true);
        expect(result.current.status).toBe('success');
        expect(result.current.response?.stage).toBe('vector');
    });

    it('does not search again when given an equal request object', async () => {
        searchMock.mockResolvedValue(responseFor('keyword'));
        const { result, rerender } = renderHook(({ current }) => usePipelineSearch('keyword', current), {
            initialProps: { current: { query: 'charger' } as SearchRequest },
        });
        await waitFor(() => expect(result.current.status).toBe('success'));

        rerender({ current: { query: 'charger' } });

        expect(searchMock).toHaveBeenCalledTimes(1);
        expect(result.current.status).toBe('success');
    });

    it('waits for a query on stages that need one, but not on Stage 1', () => {
        searchMock.mockResolvedValue(responseFor('structured'));

        const { result: keyword } = renderHook(() => usePipelineSearch('keyword', { query: '  ' }));
        renderHook(() => usePipelineSearch('structured', { query: '' }));

        expect(keyword.current.status).toBe('idle');
        expect(searchMock.mock.calls.map(([stage]) => stage)).toEqual(['structured']);
    });

    it("reports the API's error, with its fix-it guidance", async () => {
        const guidance = 'The embedding model is missing. Download it, then restart the AppHost.';
        searchMock.mockRejectedValue(new ApiError(503, 'Service Unavailable', { detail: guidance }, []));

        const { result } = renderHook(() => usePipelineSearch('vector', request));

        await waitFor(() => expect(result.current.status).toBe('error'));
        expect(result.current.error?.message).toBe(guidance);
        expect(result.current.response).toBeNull();
    });

    it('sends the same search again on rerun', async () => {
        searchMock.mockResolvedValue(responseFor('ontology'));
        const { result } = renderHook(() => usePipelineSearch('ontology', request));
        await waitFor(() => expect(result.current.status).toBe('success'));

        act(() => result.current.rerun());

        await waitFor(() => expect(result.current.status).toBe('success'));
        expect(searchMock).toHaveBeenCalledTimes(2);
    });
    it('reads every page of a ranked stage, so flagged items past the first 50 stay visible', async () => {
        const product = (id: string) => ({ id }) as unknown as SearchResponse['results'][number];
        searchMock.mockImplementation((stage, body) =>
            Promise.resolve({
                ...responseFor(stage),
                page: body.page ?? 1,
                totalResults: 3,
                results:
                    body.page === 2 ? [product('PROD-0003')] : [product('PROD-0001'), product('PROD-0002')],
            }),
        );

        const { result } = renderHook(() => usePipelineSearch('ontology', request));

        await waitFor(() => expect(result.current.status).toBe('success'));
        expect(result.current.response?.results.map((r) => r.id)).toEqual([
            'PROD-0001',
            'PROD-0002',
            'PROD-0003',
        ]);
        expect(searchMock).toHaveBeenCalledWith('ontology', { ...request, page: 2 }, expect.any(AbortSignal));
    });

    it('leaves structured search on its first page, because its total counts the whole filtered catalog', async () => {
        searchMock.mockResolvedValue({ ...responseFor('structured'), totalResults: 300 });

        const { result } = renderHook(() => usePipelineSearch('structured', request));

        await waitFor(() => expect(result.current.status).toBe('success'));
        expect(searchMock).toHaveBeenCalledTimes(1);
    });
});
