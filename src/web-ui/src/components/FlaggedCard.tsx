import { Check, CircleQuestionMark, X, type LucideIcon } from 'lucide-react';
import type { ProductResult } from '@/api/client';
import { CategoryIcon } from '@/components/CategoryIcon';
import { CompatibilityBadge } from '@/components/CompatibilityBadge';
import { DeviceFits } from '@/components/DeviceFits';
import { formatPrice } from '@/lib/format';
import { operatorPrefix, type CheckResult, type RuleCheck } from '@/lib/traceDetails';
import { cn } from '@/lib/utils';

// FlaggedCard — a near miss: similar enough to rank high, but failing a domain rule against the device the
// shopper owns, or, with no device, against what the query asked for ("65W USB-C"). Every check is listed,
// passed and failed, with the value the product has and the value needed, so the audience sees exactly why
// it doesn't fit. "#2 before rules" is the rank from Stage 5's
// own fusion, before the rules moved it down; it is not the Hybrid stage's rank (ADR-0014).

const checkIcons: Record<CheckResult, { Icon: LucideIcon; label: string; className: string }> = {
    Pass: { Icon: Check, label: 'Passes', className: 'text-compatible-ink' },
    Fail: { Icon: X, label: 'Fails', className: 'text-incompatible-ink' },
    Unknown: { Icon: CircleQuestionMark, label: 'Unknown', className: 'text-unknown-ink' },
};

export interface FlaggedCardProps {
    product: ProductResult;
    /** This product's checks from the trace, or `null` if the trace has none (then the reasons are shown). */
    checks: RuleCheck[] | null;
    icon: string | null;
}

export function FlaggedCard({ product, checks, icon }: FlaggedCardProps) {
    const { fusedRank } = product.signals;

    return (
        <article
            aria-label={product.name}
            className="flex flex-col gap-1 rounded-card border-2 border-incompatible-ink bg-card px-4 py-2.5"
        >
            <div className="flex items-start gap-2">
                <CategoryIcon
                    name={icon}
                    aria-hidden="true"
                    className="mt-0.5 size-5 flex-none text-muted-foreground"
                />
                <h4 className="min-w-0 flex-1 text-[17px] leading-tight font-bold">{product.name}</h4>
                <CompatibilityBadge compatibility={product.compatibility} />
            </div>
            <p className="text-[13px] text-muted-foreground">
                <span className="font-mono">{product.id}</span> · {formatPrice(product.price)}
                {fusedRank == null ? null : (
                    <>
                        {' · '}
                        <b
                            title="Its rank in Stage 5's fusion, before the rules moved it down"
                            className="text-search-ink"
                        >
                            #{fusedRank} before rules
                        </b>
                    </>
                )}
            </p>
            {checks === null ? (
                <ul className="flex flex-col gap-0.5 text-sm">
                    {product.compatibility.reasons.map((reason) => (
                        <li key={reason}>{reason}</li>
                    ))}
                </ul>
            ) : (
                <ul className="flex flex-col gap-0.5">
                    {checks.map((check) => (
                        <CheckLine
                            key={`${check.rule}-${check.accessorySpec}-${check.deviceSpec}`}
                            check={check}
                        />
                    ))}
                </ul>
            )}
            <DeviceFits fits={product.compatibility.fits} />
        </article>
    );
}

function CheckLine({ check }: { check: RuleCheck }) {
    const { Icon, label, className } = checkIcons[check.result];

    return (
        <li title={check.definition} className="flex flex-wrap items-baseline gap-x-1.5 text-sm">
            <Icon aria-hidden="true" className={cn('size-4 flex-none self-center', className)} />
            <span className="sr-only">{label}:</span>
            <code className="font-mono text-[13px]">{check.accessorySpec}</code>
            <span className="text-muted-foreground">has</span>
            <b className={cn(check.result === 'Fail' && 'text-incompatible-ink')}>
                {check.accessoryValue ?? 'no value'}
            </b>
            <span className="text-muted-foreground">
                {check.source === 'Query' ? '· you asked for' : '· needs'}
            </span>
            <b>
                {operatorPrefix(check.operator)}
                {check.deviceValue ?? 'unknown'}
            </b>
        </li>
    );
}
