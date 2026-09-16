import { describe, expect, it } from 'vitest';
import { citedProductIds, linkCitations, withoutSentinel } from './citations';

describe('linkCitations', () => {
    it('turns each citation into a product link', () => {
        expect(linkCitations('Get the charger [PROD-0012].')).toBe(
            'Get the charger [PROD-0012](product:PROD-0012).',
        );
    });

    it('splits a grouped citation into one link per product', () => {
        expect(linkCitations('Both fit [PROD-0012, PROD-0011].')).toBe(
            'Both fit [PROD-0012](product:PROD-0012) [PROD-0011](product:PROD-0011).',
        );
    });

    it('leaves ordinary markdown links and unbracketed IDs alone', () => {
        const markdown = 'See [the guide](https://example.com) and PROD-0012.';

        expect(linkCitations(markdown)).toBe(markdown);
    });
});

describe('citedProductIds', () => {
    it('returns each cited ID once, in order', () => {
        expect(citedProductIds('[PROD-0014] not [PROD-0012, PROD-0014], but [PROD-0011]')).toEqual([
            'PROD-0014',
            'PROD-0012',
            'PROD-0011',
        ]);
    });
});

describe('withoutSentinel', () => {
    it('removes the INSUFFICIENT_EVIDENCE first line, bold or not', () => {
        expect(withoutSentinel('INSUFFICIENT_EVIDENCE\nNo chargers for this laptop.')).toBe(
            'No chargers for this laptop.',
        );
        expect(withoutSentinel('**INSUFFICIENT_EVIDENCE**\nNothing fits.')).toBe('Nothing fits.');
    });

    it('holds back a streaming first line that may become the sentinel', () => {
        expect(withoutSentinel('INSUFFIC')).toBe('');
        expect(withoutSentinel('I recommend')).toBe('I recommend');
    });

    it('leaves an ordinary answer unchanged', () => {
        expect(withoutSentinel('Get [PROD-0012].')).toBe('Get [PROD-0012].');
    });
});
