import { useTheme } from '@/hooks/useTheme';
import type { ResolvedTheme } from '@/lib/theme';
import greenFilled from '../../assets/images/logo_green.png';
import greenOutline from '../../assets/images/logo_green_boarder.png';
import orangeFilled from '../../assets/images/logo_orange.png';
import orangeOutline from '../../assets/images/logo_orange_border.png';
import purpleFilled from '../../assets/images/logo_purple.png';
import purpleOutline from '../../assets/images/logo_purple_boarder.png';

export type LogoColour = 'green' | 'purple' | 'orange';

// The filled logo sits on the light theme; the outline logo reads better on dark (ADR-0014 § Visual design).
const logoSources: Record<LogoColour, Record<ResolvedTheme, string>> = {
    green: { light: greenFilled, dark: greenOutline },
    purple: { light: purpleFilled, dark: purpleOutline },
    orange: { light: orangeFilled, dark: orangeOutline },
};

// The source images are 141 × 148 px.
const heightPerWidth = 148 / 141;

export interface LogoProps {
    colour?: LogoColour;
    /** Width in CSS pixels. */
    size?: number;
    className?: string;
}

/** The Pi & Mash logo, switched to the right variant for the current theme. */
export function Logo({ colour = 'green', size = 40, className }: LogoProps) {
    const { resolvedTheme } = useTheme();

    return (
        <img
            src={logoSources[colour][resolvedTheme]}
            alt="Pi & Mash"
            width={size}
            height={Math.round(size * heightPerWidth)}
            className={className}
        />
    );
}
