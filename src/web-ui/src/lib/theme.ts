import { createContext } from 'react';

// Light and dark themes both ship. The default follows the operating system; the header toggle
// overrides it, and the choice is remembered in localStorage (ADR-0014 § Visual design).

/** What the viewer chose: follow the system, or force a theme. */
export type ThemePreference = 'system' | 'light' | 'dark';

/** The theme actually applied to the page. */
export type ResolvedTheme = 'light' | 'dark';

/** Also read by the inline script in `index.html`, which applies the theme before React loads. */
export const themeStorageKey = 'needle-theme';

const preferenceOrder: ThemePreference[] = ['system', 'light', 'dark'];

export function resolveTheme(preference: ThemePreference, systemPrefersDark: boolean): ResolvedTheme {
    if (preference === 'system') {
        return systemPrefersDark ? 'dark' : 'light';
    }
    return preference;
}

/** The toggle cycles system → light → dark → system. */
export function nextPreference(preference: ThemePreference): ThemePreference {
    const nextIndex = (preferenceOrder.indexOf(preference) + 1) % preferenceOrder.length;
    return preferenceOrder[nextIndex] ?? 'system';
}

export function isThemePreference(value: unknown): value is ThemePreference {
    return value === 'system' || value === 'light' || value === 'dark';
}

// Storage can throw (blocked site data, some private windows), so a failure just means
// "follow the system" rather than a broken page.
export function readStoredPreference(): ThemePreference {
    try {
        const stored = localStorage.getItem(themeStorageKey);
        return isThemePreference(stored) ? stored : 'system';
    } catch {
        return 'system';
    }
}

export function storePreference(preference: ThemePreference): void {
    try {
        localStorage.setItem(themeStorageKey, preference);
    } catch {
        // Not remembered this time; the theme still applies for this visit.
    }
}

export interface ThemeContextValue {
    preference: ThemePreference;
    resolvedTheme: ResolvedTheme;
    setPreference: (preference: ThemePreference) => void;
}

export const ThemeContext = createContext<ThemeContextValue | null>(null);
