import { useCallback, useEffect, useMemo, useState, useSyncExternalStore, type ReactNode } from 'react';
import {
    readStoredPreference,
    resolveTheme,
    storePreference,
    ThemeContext,
    type ThemePreference,
} from '@/lib/theme';

const darkSchemeQuery = '(prefers-color-scheme: dark)';

function subscribeToSystemTheme(onChange: () => void): () => void {
    const mediaQuery = window.matchMedia(darkSchemeQuery);
    mediaQuery.addEventListener('change', onChange);
    return () => mediaQuery.removeEventListener('change', onChange);
}

function getSystemPrefersDark(): boolean {
    return window.matchMedia(darkSchemeQuery).matches;
}

export interface ThemeProviderProps {
    children: ReactNode;
}

/**
 * Applies the light or dark theme by toggling `.dark` on `<html>`, which switches every colour
 * token in `src/index.css`. Read the theme with `useTheme()`.
 */
export function ThemeProvider({ children }: ThemeProviderProps) {
    const [preference, setPreferenceState] = useState<ThemePreference>(readStoredPreference);
    // Re-renders when the operating system switches between light and dark.
    const systemPrefersDark = useSyncExternalStore(subscribeToSystemTheme, getSystemPrefersDark);
    const resolvedTheme = resolveTheme(preference, systemPrefersDark);

    useEffect(() => {
        document.documentElement.classList.toggle('dark', resolvedTheme === 'dark');
    }, [resolvedTheme]);

    const setPreference = useCallback((next: ThemePreference) => {
        setPreferenceState(next);
        storePreference(next);
    }, []);

    const value = useMemo(
        () => ({ preference, resolvedTheme, setPreference }),
        [preference, resolvedTheme, setPreference],
    );

    return <ThemeContext value={value}>{children}</ThemeContext>;
}
