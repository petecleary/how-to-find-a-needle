import { Monitor, Moon, Sun } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useTheme } from '@/hooks/useTheme';
import { nextPreference, type ThemePreference } from '@/lib/theme';

const preferenceLabels: Record<ThemePreference, string> = {
    system: 'follow the system',
    light: 'light',
    dark: 'dark',
};

const preferenceIcons: Record<ThemePreference, typeof Sun> = {
    system: Monitor,
    light: Sun,
    dark: Moon,
};

/** The header's theme switch: cycles system → light → dark, and is remembered on this device. */
export function ThemeToggle() {
    const { preference, setPreference } = useTheme();
    const next = nextPreference(preference);
    const Icon = preferenceIcons[preference];
    const label = `Theme: ${preferenceLabels[preference]}. Switch to ${preferenceLabels[next]}.`;

    return (
        <Button
            variant="outline"
            size="icon"
            className="rounded-full border-2"
            onClick={() => setPreference(next)}
            aria-label={label}
            title={label}
        >
            <Icon aria-hidden="true" />
        </Button>
    );
}
