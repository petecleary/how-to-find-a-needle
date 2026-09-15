import { describe, expect, it } from 'vitest';
import { formatMilliseconds, formatPrice } from './format';

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
