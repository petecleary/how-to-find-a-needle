import { afterEach, describe, expect, expectTypeOf, it, vi } from 'vitest';
import {
    ApiError,
    getGoldenQueries,
    isSearchStage,
    search,
    searchStages,
    type SearchResponse,
    type SearchStage,
} from './client';

function jsonResponse(body: unknown, status = 200, contentType = 'application/json'): Response {
    return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } });
}

function stubFetch(response: Response) {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(response);
    vi.stubGlobal('fetch', fetchMock);
    return fetchMock;
}

async function catchApiError(promise: Promise<unknown>): Promise<ApiError> {
    const error: unknown = await promise.catch((caught: unknown) => caught);
    expect(error).toBeInstanceOf(ApiError);
    return error as ApiError;
}

const emptyResponse: SearchResponse = {
    stage: 'hybrid',
    query: 'power adapter for my laptop',
    page: 1,
    pageSize: 50,
    totalResults: 0,
    executionTimeMs: 12.5,
    results: [],
    debugTrace: { steps: [] },
};

afterEach(() => {
    vi.unstubAllGlobals();
});

describe('search', () => {
    it('only accepts stages the API document has an endpoint for', () => {
        expectTypeOf<SearchStage>().toEqualTypeOf<
            'structured' | 'keyword' | 'vector' | 'hybrid' | 'ontology'
        >();
    });

    it('keeps the runtime stage list identical to the generated stage type', () => {
        expectTypeOf<(typeof searchStages)[number]>().toEqualTypeOf<SearchStage>();
        expect(isSearchStage('hybrid')).toBe(true);
        expect(isSearchStage('rag')).toBe(false);
    });

    it('POSTs the same JSON request to the stage endpoint and returns the response', async () => {
        const fetchMock = stubFetch(jsonResponse(emptyResponse));
        const request = { query: 'power adapter for my laptop', pageSize: 50 };

        const response = await search('hybrid', request);

        expect(response).toEqual(emptyResponse);
        const [path, init] = fetchMock.mock.calls[0] ?? [];
        expect(path).toBe('/api/search/hybrid');
        expect(init?.method).toBe('POST');
        expect(init?.body).toBe(JSON.stringify(request));
    });

    it('passes the abort signal through, so switching stage can cancel the request', async () => {
        const fetchMock = stubFetch(jsonResponse(emptyResponse));
        const controller = new AbortController();

        await search('keyword', { query: 'charger' }, controller.signal);

        expect(fetchMock.mock.calls[0]?.[1]?.signal).toBe(controller.signal);
    });

    it('turns a 400 into an ApiError with each failed validation rule', async () => {
        stubFetch(
            jsonResponse(
                {
                    type: 'https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1',
                    title: 'Bad Request',
                    status: 400,
                    instance: '/api/search/keyword',
                    traceId: '0HN:00000001',
                    detail: "'page Size' must be between 1 and 50. You entered 999.",
                    errors: [
                        {
                            name: 'pageSize',
                            reason: "'page Size' must be between 1 and 50. You entered 999.",
                        },
                    ],
                },
                400,
                'application/problem+json',
            ),
        );

        const error = await catchApiError(search('keyword', { query: 'charger', pageSize: 999 }));

        expect(error.status).toBe(400);
        expect(error.message).toBe("'page Size' must be between 1 and 50. You entered 999.");
        expect(error.validationErrors.map((failure) => failure.name)).toEqual(['pageSize']);
    });

    it("surfaces a 503's fix-it guidance as the error message", async () => {
        const guidance =
            'The Nomic embedding model is missing. Run the download script, then restart the AppHost.';
        stubFetch(
            jsonResponse(
                { title: 'Embedding model unavailable', status: 503, detail: guidance },
                503,
                'application/problem+json',
            ),
        );

        const error = await catchApiError(search('vector', { query: 'charger' }));

        expect(error.status).toBe(503);
        expect(error.message).toBe(guidance);
        expect(error.problem?.title).toBe('Embedding model unavailable');
        expect(error.validationErrors).toEqual([]);
    });

    it('still reports the status when the error body is not ProblemDetails', async () => {
        stubFetch(new Response('upstream unavailable', { status: 502, statusText: 'Bad Gateway' }));

        const error = await catchApiError(search('ontology', { query: 'charger' }));

        expect(error.status).toBe(502);
        expect(error.problem).toBeNull();
        expect(error.message).toBe('502 Bad Gateway');
    });
});

describe('getGoldenQueries', () => {
    it('GETs the golden queries', async () => {
        const fetchMock = stubFetch(jsonResponse([]));

        const queries = await getGoldenQueries();

        expect(queries).toEqual([]);
        expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/demo/queries');
        expect(fetchMock.mock.calls[0]?.[1]?.method).toBeUndefined();
    });
});
