import type { GoldenQuery, SearchRequest } from '@/api/client';
import { pipelineStages, type PipelineStage } from './stageGroup';

// Demo state lives in the URL (ADR-0014 § State and data flow), e.g.
//   /demo?stage=ontology&tab=results&q=power+adapter+for+my+laptop&gq=GQ-03&device=PROD-0001
// The presenter can bookmark a moment, and the browser's back button undoes a change.
// Only values that differ from the defaults are written, so a URL shows exactly what was changed.

export const stageTabs = ['how-it-works', 'results', 'answer', 'under-the-hood', 'going-further'] as const;
export type StageTab = (typeof stageTabs)[number];

export const audiences = ['novice', 'enthusiast', 'expert'] as const;
export type Audience = (typeof audiences)[number];

/**
 * A spec filter value. The API matches specs by JSON containment, so numbers must stay numbers:
 * `voltageV: 18` matches a product's 18 but not "18" (ADR-0007).
 */
export type SpecValue = string | number;

export interface SearchFilterState {
    brand: string | null;
    /** Taxonomy notations; a parent concept also matches its narrower concepts (ADR-0013). */
    categories: string[];
    minPrice: number | null;
    maxPrice: number | null;
    specs: Record<string, SpecValue>;
}

/** Everything the stage screen needs to rebuild itself from a URL. */
export interface SearchState {
    stage: PipelineStage;
    tab: StageTab;
    query: string;
    /** The golden query the inputs came from, if any (e.g. "GQ-03"). */
    goldenQueryId: string | null;
    /** The product the shopper owns: Stage 5 checks candidates against it. */
    targetProductId: string | null;
    filters: SearchFilterState;
    /** Stage 5's before/after switches (ADR-0013). */
    expandSynonyms: boolean;
    applyConstraints: boolean;
    /** Stage 7 reads these; every stage's trace lists them (ADR-0017). */
    audience: Audience;
    applyPedagogy: boolean;
}

/**
 * Why 50, the API's maximum: Stage 5 keeps every flagged item but orders it after the out-of-concept
 * ones. At the default page size of 10, GQ-03's incompatible chargers wouldn't be on page 1, and the
 * near-miss moment would be invisible. Fusion can retrieve more than 50, so `usePipelineSearch` reads
 * the remaining pages too; switching tabs never refetches (ADR-0014).
 */
export const resultsPageSize = 50;

// The defaults match the API's own (SearchOptions.cs), so an unchanged option is never written to the URL.
export const defaultSearchState: SearchState = {
    stage: 'structured',
    // Every stage opens on How it works: the technique is explained before its results are argued about.
    tab: 'how-it-works',
    query: '',
    goldenQueryId: null,
    targetProductId: null,
    filters: { brand: null, categories: [], minPrice: null, maxPrice: null, specs: {} },
    expandSynonyms: true,
    applyConstraints: true,
    audience: 'novice',
    applyPedagogy: true,
};

const specParamPrefix = 'spec.';
// The same rule as the API's validator (SearchRequestRules.cs), so a hand-edited URL can't cause a 400.
const specKeyPattern = /^[a-zA-Z][a-zA-Z0-9]*$/;
const numberPattern = /^-?\d+(\.\d+)?$/;

/** Reads the state from a URL's query string. Anything missing or unrecognised falls back to the default. */
export function parseSearchState(params: URLSearchParams): SearchState {
    return {
        stage: oneOf(params.get('stage'), pipelineStages, defaultSearchState.stage),
        tab: oneOf(params.get('tab'), stageTabs, defaultSearchState.tab),
        query: params.get('q') ?? '',
        goldenQueryId: nonEmpty(params.get('gq')),
        targetProductId: nonEmpty(params.get('device')),
        filters: {
            brand: nonEmpty(params.get('brand')),
            categories: params.getAll('category').filter((notation) => notation !== ''),
            minPrice: parsePrice(params.get('minPrice')),
            maxPrice: parsePrice(params.get('maxPrice')),
            specs: parseSpecs(params),
        },
        expandSynonyms: parseBoolean(params.get('expandSynonyms'), defaultSearchState.expandSynonyms),
        applyConstraints: parseBoolean(params.get('applyConstraints'), defaultSearchState.applyConstraints),
        audience: oneOf(params.get('audience'), audiences, defaultSearchState.audience),
        applyPedagogy: parseBoolean(params.get('applyPedagogy'), defaultSearchState.applyPedagogy),
    };
}

/** Writes the state as a query string, leaving out every value that equals its default. */
export function serializeSearchState(state: SearchState): URLSearchParams {
    const params = new URLSearchParams();
    const defaults = defaultSearchState;

    setIfChanged(params, 'stage', state.stage, defaults.stage);
    setIfChanged(params, 'tab', state.tab, defaults.tab);
    setIfChanged(params, 'q', state.query, defaults.query);
    setIfChanged(params, 'gq', state.goldenQueryId ?? '', '');
    setIfChanged(params, 'device', state.targetProductId ?? '', '');
    setIfChanged(params, 'brand', state.filters.brand ?? '', '');
    for (const notation of state.filters.categories) {
        params.append('category', notation);
    }
    setIfChanged(params, 'minPrice', state.filters.minPrice?.toString() ?? '', '');
    setIfChanged(params, 'maxPrice', state.filters.maxPrice?.toString() ?? '', '');
    for (const key of Object.keys(state.filters.specs).sort()) {
        params.set(`${specParamPrefix}${key}`, String(state.filters.specs[key]));
    }
    setIfChanged(params, 'expandSynonyms', String(state.expandSynonyms), String(defaults.expandSynonyms));
    setIfChanged(
        params,
        'applyConstraints',
        String(state.applyConstraints),
        String(defaults.applyConstraints),
    );
    setIfChanged(params, 'audience', state.audience, defaults.audience);
    setIfChanged(params, 'applyPedagogy', String(state.applyPedagogy), String(defaults.applyPedagogy));

    return params;
}

/**
 * The one request every stage receives (ADR-0003). The tab isn't part of it: all four tabs read the
 * same response. Stage-specific options are always sent, and stages ignore the ones that don't apply.
 */
export function toSearchRequest(state: SearchState): SearchRequest {
    const { filters } = state;
    const hasSpecs = Object.keys(filters.specs).length > 0;

    return {
        query: state.query.trim(),
        page: 1,
        pageSize: resultsPageSize,
        filters: {
            brand: filters.brand,
            categories: filters.categories.length > 0 ? filters.categories : null,
            minPrice: filters.minPrice,
            maxPrice: filters.maxPrice,
            specs: hasSpecs ? filters.specs : null,
        },
        context: { targetProductId: state.targetProductId },
        options: {
            expandSynonyms: state.expandSynonyms,
            applyConstraints: state.applyConstraints,
            audience: state.audience,
            applyPedagogy: state.applyPedagogy,
        },
    };
}

/**
 * Fills the inputs from a golden query's preset: its query, target device and filters. The stage, tab
 * and toggles stay as they are, so the presenter can load a talk moment on any stage.
 */
export function applyGoldenQuery(state: SearchState, goldenQuery: GoldenQuery): SearchState {
    const { request } = goldenQuery;

    return {
        ...state,
        goldenQueryId: goldenQuery.id,
        query: request.query,
        targetProductId: request.context?.targetProductId ?? null,
        filters: {
            brand: request.filters?.brand ?? null,
            categories: [],
            minPrice: null,
            maxPrice: request.filters?.maxPrice ?? null,
            specs: toSpecValues(request.filters?.specs),
        },
    };
}

/**
 * True while the inputs (query, target device and filters) still equal a golden query's preset. Once
 * something is changed it's an ordinary search, and the picker stops naming the preset. The URL form is
 * compared, so spec keys in a different order still count as equal.
 */
export function matchesGoldenQuery(state: SearchState, goldenQuery: GoldenQuery): boolean {
    const asPreset = { ...state, goldenQueryId: goldenQuery.id };
    const preset = applyGoldenQuery(asPreset, goldenQuery);

    return serializeSearchState(asPreset).toString() === serializeSearchState(preset).toString();
}

/** How many filters are set, for the Filters button: the brand, each category, the price range (once) and each spec. */
export function countActiveFilters(filters: SearchFilterState): number {
    const brand = filters.brand === null ? 0 : 1;
    const priceRange = filters.minPrice !== null || filters.maxPrice !== null ? 1 : 0;

    return brand + filters.categories.length + priceRange + Object.keys(filters.specs).length;
}

function oneOf<T extends string>(value: string | null, allowed: readonly T[], fallback: T): T {
    return allowed.find((candidate) => candidate === value) ?? fallback;
}

function nonEmpty(value: string | null): string | null {
    return value === null || value.trim() === '' ? null : value;
}

function parseBoolean(value: string | null, fallback: boolean): boolean {
    if (value === 'true') return true;
    if (value === 'false') return false;
    return fallback;
}

function parsePrice(value: string | null): number | null {
    if (value === null || !numberPattern.test(value)) return null;
    const price = Number(value);
    return price >= 0 ? price : null;
}

function parseSpecs(params: URLSearchParams): Record<string, SpecValue> {
    const specs: Record<string, SpecValue> = {};

    for (const [name, value] of params) {
        if (!name.startsWith(specParamPrefix) || value === '') continue;

        const key = name.slice(specParamPrefix.length);
        if (!specKeyPattern.test(key)) continue;

        // A URL carries only text, so a numeric value becomes a number again. Vocabulary values are
        // notations like "usb-c", never bare numbers, so nothing that should stay text is converted.
        specs[key] = numberPattern.test(value) ? Number(value) : value;
    }

    return specs;
}

function toSpecValues(specs: Record<string, unknown> | null | undefined): Record<string, SpecValue> {
    const values: Record<string, SpecValue> = {};

    for (const [key, value] of Object.entries(specs ?? {})) {
        if (typeof value === 'string' || typeof value === 'number') {
            values[key] = value;
        }
    }

    return values;
}

function setIfChanged(params: URLSearchParams, name: string, value: string, defaultValue: string): void {
    if (value !== defaultValue) {
        params.set(name, value);
    }
}
