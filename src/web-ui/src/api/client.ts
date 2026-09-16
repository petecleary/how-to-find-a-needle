import type { components, paths } from './schema';

// A small typed wrapper around fetch, instead of a generated client (ADR-0014 § API types).
// Every type here comes from schema.d.ts, which is generated from the API's OpenAPI document,
// so a contract change shows up as a compile error rather than a surprise at runtime.

export type SearchRequest = components['schemas']['SearchRequest'];
export type SearchResponse = components['schemas']['SearchResponse'];
export type ProductResult = components['schemas']['ProductResult'];
export type CompatibilityResult = components['schemas']['CompatibilityResult'];
export type CompatibilityStatus = components['schemas']['CompatibilityStatus'];
export type ConceptMatch = components['schemas']['ConceptMatch'];
export type TraceStep = components['schemas']['TraceStep'];
export type GoldenQuery = components['schemas']['GoldenQuery'];
export type DemoDevice = components['schemas']['DemoDevice'];
export type TaxonomyNode = components['schemas']['TaxonomyNode'];
export type ValueVocabulary = components['schemas']['ValueVocabulary'];
export type ProblemDetails = components['schemas']['ProblemDetails'];
export type ValidationProblemError = components['schemas']['ValidationProblemError'];
export type AnswerFinal = components['schemas']['AnswerFinal'];
export type AnswerResponse = components['schemas']['AnswerResponse'];
export type ExplanationStructure = components['schemas']['ExplanationStructure'];

type SearchPath = Extract<keyof paths, `/api/search/${string}`>;

/**
 * The stages the API serves, read from the OpenAPI paths `/api/search/{stage}`. A stage appears here only once
 * its endpoint exists, so the UI can't call one that doesn't. The answer streams (`/api/search/rag/answer`) are
 * paths below a stage, not stages, so they are left out.
 */
export type SearchStage = SearchPath extends `/api/search/${infer Stage}`
    ? Stage extends `${string}/${string}`
        ? never
        : Stage
    : never;

/**
 * The same stages as a list the UI can check at runtime (types disappear when the code runs).
 * `client.test.ts` fails to compile if this list and `SearchStage` ever differ.
 */
export const searchStages = [
    'structured',
    'keyword',
    'vector',
    'hybrid',
    'ontology',
    'rag',
    'pedagogy',
] as const satisfies readonly SearchStage[];

export function isSearchStage(stage: string): stage is SearchStage {
    return searchStages.some((candidate) => candidate === stage);
}

/** The stages that also stream LLM text from `/api/search/{stage}/answer` (ADR-0016, ADR-0017). */
export const answerStages = ['rag', 'pedagogy'] as const satisfies readonly SearchStage[];

export type AnswerStage = (typeof answerStages)[number];

export function isAnswerStage(stage: string): stage is AnswerStage {
    return answerStages.some((candidate) => candidate === stage);
}

/** The JSON body of a path's 200 response, as the OpenAPI document describes it. */
type OkJson<Path extends keyof paths, Method extends 'get' | 'post'> = paths[Path][Method] extends {
    responses: { 200: { content: { 'application/json': infer Body } } };
}
    ? Body
    : never;

/**
 * A request the API answered with an error status. `message` is the ProblemDetails `detail` when there
 * is one: for a 503 that is the fix-it guidance, e.g. how to download a missing model (ADR-0003).
 */
export class ApiError extends Error {
    readonly status: number;
    /** The RFC 9457 body, or `null` when the response wasn't ProblemDetails (e.g. the proxy's 502). */
    readonly problem: ProblemDetails | null;
    /** One entry per failed validation rule on a 400; empty otherwise. */
    readonly validationErrors: ValidationProblemError[];

    constructor(
        status: number,
        statusText: string,
        problem: ProblemDetails | null,
        validationErrors: ValidationProblemError[],
    ) {
        super(problem?.detail ?? problem?.title ?? `${status} ${statusText}`.trim());
        this.name = 'ApiError';
        this.status = status;
        this.problem = problem;
        this.validationErrors = validationErrors;
    }
}

/** `POST /api/search/{stage}`: the same request body for every stage (ADR-0003). */
export function search(
    stage: SearchStage,
    request: SearchRequest,
    signal?: AbortSignal,
): Promise<SearchResponse> {
    return sendAsync<SearchResponse>(`/api/search/${stage}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
        signal,
    });
}

/**
 * `POST /api/search/{stage}/answer` as Server-Sent Events: the same request body as the search, sent at the same
 * time (ADR-0016). Returns the open response so the caller can read its body as it streams (`useAnswerStream`). A
 * failure before streaming starts, such as a 503 when the LLM isn't configured, is an `ApiError` like any other.
 */
export async function streamAnswer(
    stage: AnswerStage,
    request: SearchRequest,
    signal?: AbortSignal,
): Promise<Response> {
    const response = await fetch(`/api/search/${stage}/answer`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
        body: JSON.stringify(request),
        signal,
    });

    if (!response.ok) {
        throw await toApiError(response);
    }

    return response;
}

/** `GET /api/demo/queries`: the golden queries, each a talk moment with a preset request. */
export function getGoldenQueries(signal?: AbortSignal): Promise<OkJson<'/api/demo/queries', 'get'>> {
    return sendAsync('/api/demo/queries', { signal });
}

/** `GET /api/demo/devices`: products that can be the target device. */
export function getDemoDevices(signal?: AbortSignal): Promise<OkJson<'/api/demo/devices', 'get'>> {
    return sendAsync('/api/demo/devices', { signal });
}

/** `GET /api/taxonomy`: the category tree the filters are built from. */
export function getTaxonomy(signal?: AbortSignal): Promise<OkJson<'/api/taxonomy', 'get'>> {
    return sendAsync('/api/taxonomy', { signal });
}

/** `GET /api/vocabularies`: allowed spec values, with synonyms, for the spec filters. */
export function getVocabularies(signal?: AbortSignal): Promise<OkJson<'/api/vocabularies', 'get'>> {
    return sendAsync('/api/vocabularies', { signal });
}

/** `GET /api/brands`: every brand in the catalogue, for the brand filter. */
export function getBrands(signal?: AbortSignal): Promise<OkJson<'/api/brands', 'get'>> {
    return sendAsync('/api/brands', { signal });
}

// No retries: a failure should be visible, not quietly hidden (root CLAUDE.md, "No hidden magic").
// An aborted request rejects with the browser's AbortError, which callers ignore when they cancelled it.
async function sendAsync<Body>(path: string, init: RequestInit): Promise<Body> {
    const response = await fetch(path, init);

    if (!response.ok) {
        throw await toApiError(response);
    }

    return (await response.json()) as Body;
}

async function toApiError(response: Response): Promise<ApiError> {
    const body = await readJsonOrNull(response);
    const problem = isProblemDetails(body) ? body : null;
    const validationErrors = hasValidationErrors(body) ? body.errors : [];

    return new ApiError(response.status, response.statusText, problem, validationErrors);
}

async function readJsonOrNull(response: Response): Promise<unknown> {
    const contentType = response.headers.get('Content-Type') ?? '';
    if (!contentType.includes('json')) {
        return null;
    }

    try {
        return await response.json();
    } catch {
        return null;
    }
}

function isProblemDetails(value: unknown): value is ProblemDetails {
    return (
        typeof value === 'object' &&
        value !== null &&
        ('title' in value || 'detail' in value || 'status' in value)
    );
}

function hasValidationErrors(value: unknown): value is { errors: ValidationProblemError[] } {
    if (typeof value !== 'object' || value === null || !('errors' in value) || !Array.isArray(value.errors)) {
        return false;
    }

    return value.errors.every(
        (error: unknown) =>
            typeof error === 'object' &&
            error !== null &&
            'name' in error &&
            typeof error.name === 'string' &&
            'reason' in error &&
            typeof error.reason === 'string',
    );
}
