import { describe, expect, it } from 'vitest';
import type { GoldenQuery } from '@/api/client';
import {
    applyGoldenQuery,
    countActiveFilters,
    defaultSearchState,
    matchesGoldenQuery,
    parseSearchState,
    serializeSearchState,
    toSearchRequest,
    type SearchState,
} from './searchState';

// The presets as they appear in assets/data/golden-queries.json.
const gq01: GoldenQuery = {
    id: 'GQ-01',
    title: 'Similarity is not compatibility',
    moment: 'The ontology flags the 45W barrel charger Incompatible on connector and wattage.',
    request: {
        query: 'power adapter for my laptop',
        filters: null,
        context: { targetProductId: 'PROD-0001' },
    },
    expectations: {},
};

const gq04: GoldenQuery = {
    id: 'GQ-04',
    title: 'Structured filters',
    moment: 'Stage 1 filters without a query.',
    request: {
        query: '',
        filters: { brand: 'Brakk', maxPrice: 100, specs: { voltageV: 18 } },
        context: null,
    },
    expectations: {},
};

const everyFieldChanged: SearchState = {
    stage: 'ontology',
    tab: 'under-the-hood',
    query: 'power adapter for my laptop',
    goldenQueryId: 'GQ-01',
    targetProductId: 'PROD-0001',
    filters: {
        brand: 'Voltline',
        categories: ['chargers', 'batteries'],
        minPrice: 10,
        maxPrice: 49.99,
        specs: { connector: 'usb-c', powerW: 65 },
    },
    expandSynonyms: false,
    applyConstraints: false,
    audience: 'expert',
    applyPedagogy: false,
};

describe('parseSearchState', () => {
    it('uses the defaults for an empty URL', () => {
        expect(parseSearchState(new URLSearchParams())).toEqual(defaultSearchState);
    });

    it('falls back to the default for values it does not recognise', () => {
        const params = new URLSearchParams(
            'stage=bm25&tab=slides&audience=child&maxPrice=cheap&minPrice=-5&expandSynonyms=maybe&spec.bad-key=x',
        );

        expect(parseSearchState(params)).toEqual(defaultSearchState);
    });

    it('turns numeric spec values back into numbers, so JSON containment still matches', () => {
        const state = parseSearchState(new URLSearchParams('spec.voltageV=18&spec.connector=usb-c'));

        expect(state.filters.specs).toEqual({ voltageV: 18, connector: 'usb-c' });
    });
});

describe('serializeSearchState', () => {
    it('writes only the values that differ from the defaults', () => {
        const state: SearchState = { ...defaultSearchState, stage: 'hybrid', query: 'power brick' };

        expect(serializeSearchState(state).toString()).toBe('stage=hybrid&q=power+brick');
    });

    it('round-trips every field through the URL', () => {
        const params = serializeSearchState(everyFieldChanged);

        expect(parseSearchState(new URLSearchParams(params.toString()))).toEqual(everyFieldChanged);
    });
});

describe('toSearchRequest', () => {
    it("asks for 50 results, so Stage 5's flagged items are in the one response", () => {
        const request = toSearchRequest(defaultSearchState);

        expect(request.page).toBe(1);
        expect(request.pageSize).toBe(50);
    });

    it('maps the filters, target device and toggles into the shared request', () => {
        const request = toSearchRequest(everyFieldChanged);

        expect(request).toEqual({
            query: 'power adapter for my laptop',
            page: 1,
            pageSize: 50,
            filters: {
                brand: 'Voltline',
                categories: ['chargers', 'batteries'],
                minPrice: 10,
                maxPrice: 49.99,
                specs: { connector: 'usb-c', powerW: 65 },
            },
            context: { targetProductId: 'PROD-0001' },
            options: {
                expandSynonyms: false,
                applyConstraints: false,
                audience: 'expert',
                applyPedagogy: false,
            },
        });
    });

    it('sends null rather than empty category and spec filters', () => {
        const { filters } = toSearchRequest(defaultSearchState);

        expect(filters?.categories).toBeNull();
        expect(filters?.specs).toBeNull();
    });

    it('gives every stage the same request: the tab is not part of it', () => {
        const onResults = toSearchRequest({ ...everyFieldChanged, tab: 'results' });
        const onTrace = toSearchRequest({ ...everyFieldChanged, tab: 'under-the-hood', stage: 'keyword' });

        expect(onTrace).toEqual(onResults);
    });
});

describe('countActiveFilters', () => {
    it('counts the brand, each category, the price range once and each spec', () => {
        expect(countActiveFilters(defaultSearchState.filters)).toBe(0);
        expect(countActiveFilters(everyFieldChanged.filters)).toBe(6);
        expect(countActiveFilters(applyGoldenQuery(defaultSearchState, gq04).filters)).toBe(3);
    });
});

describe('matchesGoldenQuery', () => {
    it('matches until an input moves away from the preset, whatever the stage or tab', () => {
        const loaded = { ...applyGoldenQuery(defaultSearchState, gq04), stage: 'keyword' as const };

        expect(matchesGoldenQuery(loaded, gq04)).toBe(true);
        expect(matchesGoldenQuery({ ...loaded, query: 'drill battery' }, gq04)).toBe(false);
        expect(
            matchesGoldenQuery(
                { ...loaded, filters: { ...loaded.filters, categories: ['power-tools'] } },
                gq04,
            ),
        ).toBe(false);
        expect(matchesGoldenQuery({ ...loaded, targetProductId: 'PROD-0001' }, gq04)).toBe(false);
    });
});

describe('applyGoldenQuery', () => {
    it("fills the query and target device from GQ-01's preset", () => {
        const state = applyGoldenQuery(defaultSearchState, gq01);

        expect(state.goldenQueryId).toBe('GQ-01');
        expect(state.query).toBe('power adapter for my laptop');
        expect(state.targetProductId).toBe('PROD-0001');
        expect(toSearchRequest(state).context).toEqual({ targetProductId: 'PROD-0001' });
    });

    it("fills the brand, price and numeric spec filters from GQ-04's preset", () => {
        const state = applyGoldenQuery(defaultSearchState, gq04);

        expect(state.filters).toEqual({
            brand: 'Brakk',
            categories: [],
            minPrice: null,
            maxPrice: 100,
            specs: { voltageV: 18 },
        });
    });

    it('replaces the previous inputs but keeps the stage, tab and toggles', () => {
        const state = applyGoldenQuery(everyFieldChanged, gq04);

        expect(state.targetProductId).toBeNull();
        expect(state.filters.categories).toEqual([]);
        expect(state.stage).toBe('ontology');
        expect(state.tab).toBe('under-the-hood');
        expect(state.applyConstraints).toBe(false);
        expect(state.audience).toBe('expert');
    });
});
