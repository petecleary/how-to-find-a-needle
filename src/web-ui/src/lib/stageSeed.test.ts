import { describe, expect, it } from 'vitest';
import goldenQueriesJson from '../../../PI.SearchApi/assets/data/golden-queries.json';
import type { GoldenQuery } from '@/api/client';
import { seededState, seedTab } from './stageSeed';

describe('seedTab', () => {
    it('lands on How it works unless the seed names another tab', () => {
        expect(seedTab({ stage: 'keyword' })).toBe('how-it-works');
        expect(seedTab({ stage: 'pedagogy', tab: 'answer' })).toBe('answer');
    });

    it('falls back when a seed names a tab its stage does not have', () => {
        // Stage 1 has no Going further tab (ADR-0018).
        expect(seedTab({ stage: 'structured', tab: 'going-further' })).toBe('how-it-works');
        expect(seedTab({ stage: 'keyword', tab: 'slides' })).toBe('how-it-works');
    });
});

describe('seededState', () => {
    // JSON types each query's shape separately, so the API's contract type needs a cast through unknown.
    const gq03 = goldenQueriesJson.find((query) => query.id === 'GQ-03') as unknown as GoldenQuery;

    it("fills a seed's golden query and options, and leaves the rest at the defaults", () => {
        const state = seededState(
            { stage: 'ontology', goldenQuery: 'GQ-03', options: { applyConstraints: false } },
            gq03,
        );

        expect(state).toMatchObject({
            stage: 'ontology',
            tab: 'how-it-works',
            goldenQueryId: 'GQ-03',
            query: 'power adapter for my laptop',
            targetProductId: 'PROD-0001',
            applyConstraints: false,
            expandSynonyms: true,
            model: null,
        });
    });

    it('uses the default stage for an unknown one', () => {
        expect(seededState({ stage: 'bm25' }, null).stage).toBe('structured');
    });
});
