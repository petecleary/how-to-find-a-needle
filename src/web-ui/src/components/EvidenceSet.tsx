import { LoaderCircle, TriangleAlert } from 'lucide-react';
import { useEffect, useRef } from 'react';
import { CompatibilityBadge } from '@/components/CompatibilityBadge';
import type { EvidenceDetails } from '@/lib/traceDetails';
import { cn } from '@/lib/utils';

// EvidenceSet — exactly what the model was given (ADR-0016): the target device and each product with its verdict.
// Citation chips jump here, so a claim can be checked against the product it names. It comes from the results
// response's evidence step, so it is on screen before the first token.

export interface EvidenceSetProps {
    evidence: EvidenceDetails | null;
    isLoading: boolean;
    citedIds: ReadonlySet<string>;
    invalidIds: readonly string[];
    selectedProductId: string | null;
    onSelect: (productId: string | null) => void;
}

export function EvidenceSet({
    evidence,
    isLoading,
    citedIds,
    invalidIds,
    selectedProductId,
    onSelect,
}: EvidenceSetProps) {
    const rowRefs = useRef(new Map<string, HTMLLIElement>());

    // Clicking a chip in the text brings its product into view, however long the list.
    useEffect(() => {
        if (selectedProductId !== null) {
            rowRefs.current
                .get(selectedProductId)
                ?.scrollIntoView?.({ block: 'nearest', behavior: 'smooth' });
        }
    }, [selectedProductId]);

    return (
        <section aria-labelledby="evidence-heading" className="overflow-hidden rounded-card border-2 bg-card">
            <div className="border-b-2 px-4 py-2">
                <h2
                    id="evidence-heading"
                    className="text-sm font-bold tracking-[.08em] text-muted-foreground uppercase"
                >
                    Evidence set{evidence === null ? '' : ` · ${evidence.items.length}`}
                </h2>
                <p className="text-sm text-muted-foreground">
                    All the model was given. Citation chips jump here.
                </p>
            </div>

            {evidence === null ? (
                <p className="flex items-center gap-2 px-4 py-3 text-muted-foreground">
                    {isLoading ? <LoaderCircle aria-hidden="true" className="size-4 animate-spin" /> : null}
                    {isLoading
                        ? 'Loading the evidence…'
                        : 'The evidence comes with the results: see the Results tab.'}
                </p>
            ) : (
                <ol>
                    {evidence.items.map((item) => {
                        const isSelected = item.id === selectedProductId;
                        const isCited = citedIds.has(item.id);

                        return (
                            <li
                                key={item.id}
                                ref={(element) => {
                                    if (element === null) {
                                        rowRefs.current.delete(item.id);
                                    } else {
                                        rowRefs.current.set(item.id, element);
                                    }
                                }}
                                aria-current={isSelected ? 'true' : undefined}
                                className={cn(
                                    'border-b-2 last:border-b-0',
                                    isSelected && 'bg-pedagogy-tint',
                                    item.role === 'Incompatible' && !isSelected && 'bg-incompatible-tint/40',
                                )}
                            >
                                <button
                                    type="button"
                                    onClick={() => onSelect(isSelected ? null : item.id)}
                                    className="flex w-full items-center gap-2 px-4 py-1.5 text-left"
                                >
                                    <span className="flex min-w-0 flex-1 flex-col leading-tight">
                                        <b className="truncate">{item.name}</b>
                                        <span className="font-mono text-[13px] text-muted-foreground">
                                            {item.id}
                                            {isCited ? <b className="ml-2 text-foreground">cited</b> : null}
                                        </span>
                                    </span>
                                    {item.role === 'TargetDevice' ? (
                                        <span className="text-sm text-muted-foreground">target device</span>
                                    ) : (
                                        <CompatibilityBadge compatibility={item.compatibility} />
                                    )}
                                </button>
                                {isSelected ? (
                                    <p className="px-4 pb-2 text-sm text-muted-foreground">
                                        {item.whyIncluded}
                                        {item.compatibility.reasons.length > 0
                                            ? ` ${item.compatibility.reasons.join(' ')}`
                                            : ''}
                                    </p>
                                ) : null}
                            </li>
                        );
                    })}
                </ol>
            )}

            {invalidIds.length > 0 ? (
                <p className="flex gap-2 border-t-2 px-4 py-2 text-sm text-incompatible-ink">
                    <TriangleAlert aria-hidden="true" className="mt-0.5 size-4 flex-none" />
                    Cited but not in the evidence: {invalidIds.join(', ')}
                </p>
            ) : null}
        </section>
    );
}
