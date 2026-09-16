import { describe, expect, it } from 'vitest';
import type { ProductResult } from '@/api/client';
import { gq01OntologyResponse } from '@/test/responses';
import { scoreMeaning, signalBadges } from './signals';

function product(id: string): ProductResult {
    const found = gq01OntologyResponse.results.find((result) => result.id === id);
    if (found === undefined) {
        throw new Error(`${id} is not in the GQ-01 fixture`);
    }
    return found;
}

const values = (stage: Parameters<typeof signalBadges>[0], id: string) =>
    signalBadges(stage, product(id)).map((badge) => `${badge.label} ${badge.value}`);

describe('signalBadges', () => {
    it("shows both retrievers' ranks and the RRF sum on Hybrid", () => {
        expect(values('hybrid', 'PROD-0012')).toEqual(['KW #3', 'VEC #1', 'RRF 0.03227']);
    });

    it('shows a dash for a retriever that missed the item', () => {
        expect(values('hybrid', 'PROD-0036')).toEqual(['KW –', 'VEC #9', 'RRF 0.01449']);
    });

    it('shows no signals on Structured, which has no score', () => {
        expect(signalBadges('structured', product('PROD-0012'))).toEqual([]);
        expect(scoreMeaning('structured')).toBe('no score: ordered by price, then ID');
    });
});
