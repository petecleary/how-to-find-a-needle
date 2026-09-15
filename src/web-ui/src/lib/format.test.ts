import { describe, expect, it } from 'vitest';
import { formatMilliseconds, formatPrice, formatResultRange } from './format';

describe('formatResultRange', () => {
    it('shows which results are on the page, out of all that matched', () => {
        expect(formatResultRange(1, 50, 60)).toBe('1–50 of 60');
        expect(formatResultRange(1, 4, 4)).toBe('1–4 of 4');
        expect(formatResultRange(1, 0, 0)).toBe('0 of 0');
    });
});

describe('formatPrice', () => {
    it('formats GBP the en-GB way', () => {
        expect(formatPrice(1599.99)).toBe('£1,599.99');
        expect(formatPrice(49.9)).toBe('£49.90');
    });
});

describe('formatMilliseconds', () => {
    it('rounds to two decimal places', () => {
        expect(formatMilliseconds(19.968)).toBe('19.97 ms');
    });
});
