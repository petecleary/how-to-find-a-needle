import { describe, expect, it } from 'vitest';
import { gq01OntologyResponse } from '@/test/responses';
import { groupByConcept } from './resultGroups';

const ids = (products: { id: string }[]) => products.map((product) => product.id);

describe('groupByConcept, with GQ-01 on Stage 5', () => {
    const { results } = gq01OntologyResponse;
    const groups = groupByConcept(results, 'PROD-0001');

    it('keeps the compatible USB-C chargers in concept', () => {
        expect(ids(groups.inConcept)).toEqual(['PROD-0012', 'PROD-0013', 'PROD-0011']);
    });

    it('flags every incompatible item, including the near-miss chargers Hybrid ranked high', () => {
        expect(ids(groups.flagged)).toEqual([
            'PROD-0014',
            'PROD-0015',
            'PROD-0016',
            'PROD-0017',
            'PROD-0049',
            'PROD-0025',
            'PROD-0023',
        ]);
    });

    it('moves the target device and out-of-concept items down without dropping them', () => {
        expect(ids(groups.outOfConcept)).toContain('PROD-0001');
        expect(groups.inConcept.length + groups.outOfConcept.length + groups.flagged.length).toBe(50);
    });

    it("never re-ranks: the groups, joined, are the API's order", () => {
        expect(ids([...groups.inConcept, ...groups.outOfConcept, ...groups.flagged])).toEqual(ids(results));
    });
});
