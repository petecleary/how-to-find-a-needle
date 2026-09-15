import { useContext } from 'react';
import { ThemeContext, type ThemeContextValue } from '@/lib/theme';

/** The current theme preference, the theme applied, and a setter. Needs a `ThemeProvider` above it. */
export function useTheme(): ThemeContextValue {
    const context = useContext(ThemeContext);
    if (context === null) {
        throw new Error('useTheme must be used inside a ThemeProvider.');
    }
    return context;
}
