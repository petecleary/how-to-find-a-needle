import type { ProductResult } from '@/api/client';
import { CategoryIcon } from '@/components/CategoryIcon';
import { CompatibilityBadge } from '@/components/CompatibilityBadge';
import { ConceptBadge } from '@/components/ConceptBadge';
import { SignalBadges } from '@/components/SignalBadges';
import { formatPrice } from '@/lib/format';
import type { SignalBadge } from '@/lib/signals';
import { cn } from '@/lib/utils';

export interface ResultRowProps {
    product: ProductResult;
    /** The position in the stage's order, counting from 1. */
    rank: number;
    /** The Lucide icon name of the product's first category. */
    icon: string | null;
    signals?: SignalBadge[];
    showConcept?: boolean;
    /** Leave out the "Not evaluated" badge where the group already says why, e.g. out of concept on Stage 5. */
    hideNotEvaluated?: boolean;
}

/** One candidate: rank, category icon, name, brand and price, then its signal, concept and compatibility badges. */
export function ResultRow({
    product,
    rank,
    icon,
    signals = [],
    showConcept = false,
    hideNotEvaluated = false,
}: ResultRowProps) {
    const { compatibility } = product;
    // Reasons that aren't a rule passing are shown, not only on hover: a missing spec (Unknown), or
    // "This is your target device" on the laptop Stage 5 moved down.
    const isUnknown = compatibility.status === 'Unknown';
    const visibleReasons = isUnknown || compatibility.status === 'NotEvaluated' ? compatibility.reasons : [];

    return (
        <li className="flex flex-wrap items-center gap-x-3 gap-y-1 border-b-2 px-4 py-1.5 last:border-b-0">
            <span className="flex size-[26px] flex-none items-center justify-center rounded-full bg-muted text-sm font-bold">
                {rank}
            </span>
            <CategoryIcon name={icon} aria-hidden="true" className="size-5 flex-none text-muted-foreground" />
            <div className="flex min-w-48 flex-1 flex-col leading-tight">
                <b className="text-[17px]">{product.name}</b>
                <span className="text-[13px] text-muted-foreground">
                    <span className="font-mono">{product.id}</span> · {product.brand} ·{' '}
                    {formatPrice(product.price)}
                </span>
                {visibleReasons.map((reason) => (
                    <span
                        key={reason}
                        className={cn(
                            'text-[13px]',
                            isUnknown ? 'text-unknown-ink' : 'text-muted-foreground',
                        )}
                    >
                        {reason}
                    </span>
                ))}
            </div>
            <SignalBadges badges={signals} />
            {showConcept ? <ConceptBadge conceptMatch={product.signals.conceptMatch} /> : null}
            {hideNotEvaluated && compatibility.status === 'NotEvaluated' ? null : (
                <CompatibilityBadge compatibility={compatibility} />
            )}
        </li>
    );
}
