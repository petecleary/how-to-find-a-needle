import type { SignalBadge } from '@/lib/signals';

export interface SignalBadgesProps {
    badges: SignalBadge[];
}

/** Each technique's own rank or score for one result, e.g. `KW #1 · VEC #12 · RRF 0.02951`. */
export function SignalBadges({ badges }: SignalBadgesProps) {
    if (badges.length === 0) {
        return null;
    }

    return (
        <ul className="flex flex-none flex-wrap items-center gap-1.5">
            {badges.map((badge) => (
                <li
                    key={badge.label}
                    title={badge.description}
                    className="rounded-full border-2 px-2 py-0.5 font-mono text-sm whitespace-nowrap"
                >
                    <b className="font-sans text-search-ink">{badge.label}</b> {badge.value}
                    <span className="sr-only">: {badge.description}</span>
                </li>
            ))}
        </ul>
    );
}
