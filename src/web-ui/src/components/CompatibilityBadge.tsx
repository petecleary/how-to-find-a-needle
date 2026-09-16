import { Check, CircleQuestionMark, Minus, X, type LucideIcon } from 'lucide-react';
import type { CompatibilityResult, CompatibilityStatus } from '@/api/client';
import { cn } from '@/lib/utils';

// Status colours are not brand colours (ADR-0014 § Visual design): Incompatible is red, never orange, and
// every badge carries an icon and text, so it reads without colour. Hover shows the rule reasons.

const badges: Record<CompatibilityStatus, { label: string; Icon: LucideIcon; className: string }> = {
    Compatible: {
        label: 'Compatible',
        Icon: Check,
        className: 'border-transparent bg-compatible-tint text-compatible-ink',
    },
    Incompatible: {
        label: 'Incompatible',
        Icon: X,
        className: 'border-transparent bg-incompatible-tint text-incompatible-ink',
    },
    Unknown: {
        label: 'Unknown',
        Icon: CircleQuestionMark,
        className: 'border-transparent bg-unknown-tint text-unknown-ink',
    },
    // Stages 1–4 have no idea what "compatible" means; the badge says so on every row.
    NotEvaluated: { label: 'Not evaluated', Icon: Minus, className: 'text-muted-foreground' },
};

export interface CompatibilityBadgeProps {
    compatibility: CompatibilityResult;
}

export function CompatibilityBadge({ compatibility }: CompatibilityBadgeProps) {
    const { label, Icon, className } = badges[compatibility.status];

    return (
        <span
            title={compatibility.reasons.length > 0 ? compatibility.reasons.join('\n') : undefined}
            className={cn(
                'inline-flex flex-none items-center gap-1 rounded-full border-2 px-2.5 py-0.5 text-sm font-bold whitespace-nowrap',
                className,
            )}
        >
            <Icon aria-hidden="true" className="size-4" />
            {label}
        </span>
    );
}
