import { describe, expect, it } from 'vitest';
import { isThemePreference, nextPreference, resolveTheme } from './theme';

describe('resolveTheme', () => {
    it('follows the system setting by default', () => {
        expect(resolveTheme('system', true)).toBe('dark');
        expect(resolveTheme('system', false)).toBe('light');
    });

    it('lets an explicit choice override the system setting', () => {
        expect(resolveTheme('light', true)).toBe('light');
        expect(resolveTheme('dark', false)).toBe('dark');
    });
});

describe('nextPreference', () => {
    it('cycles system → light → dark → system', () => {
        expect(nextPreference('system')).toBe('light');
        expect(nextPreference('light')).toBe('dark');
        expect(nextPreference('dark')).toBe('system');
    });
});

describe('isThemePreference', () => {
    it('rejects values that are not a known preference', () => {
        expect(isThemePreference('dark')).toBe(true);
        expect(isThemePreference('sepia')).toBe(false);
        expect(isThemePreference(null)).toBe(false);
    });
});
