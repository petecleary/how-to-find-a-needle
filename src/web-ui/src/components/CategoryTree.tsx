import { ChevronRight } from 'lucide-react';
import { useId, useState } from 'react';
import type { TaxonomyNode } from '@/api/client';
import { Checkbox } from '@/components/ui/checkbox';
import { hasSelectedNarrower } from '@/lib/filters';
import { cn } from '@/lib/utils';

// CategoryTree — the SKOS concept scheme as checkboxes. Ticking a broader concept ticks everything under
// it, because the API matches a category and all its narrower concepts (ADR-0013): the tree shows what the
// filter will really match, rather than leaving the audience to guess.

export interface CategoryTreeProps {
    taxonomy: TaxonomyNode[];
    /** The selected notations, as sent in `filters.categories`. */
    selected: string[];
    onToggle: (notation: string) => void;
}

export function CategoryTree({ taxonomy, selected, onToggle }: CategoryTreeProps) {
    return (
        <ul className="flex flex-col">
            {taxonomy.map((concept) => (
                <CategoryNode
                    key={concept.notation}
                    concept={concept}
                    selected={selected}
                    onToggle={onToggle}
                    includedBy={null}
                />
            ))}
        </ul>
    );
}

interface CategoryNodeProps {
    concept: TaxonomyNode;
    selected: string[];
    onToggle: (notation: string) => void;
    /** The selected broader concept that already includes this one, if any. */
    includedBy: TaxonomyNode | null;
}

function CategoryNode({ concept, selected, onToggle, includedBy }: CategoryNodeProps) {
    const checkboxId = useId();
    const [isOpen, setIsOpen] = useState(() => hasSelectedNarrower(concept, selected));

    const isSelected = selected.includes(concept.notation);
    const isIncluded = includedBy !== null;
    const hasNarrower = concept.narrower.length > 0;

    return (
        <li>
            <div className="flex items-center gap-1.5 py-0.5">
                {hasNarrower ? (
                    <button
                        type="button"
                        aria-expanded={isOpen}
                        aria-label={`${isOpen ? 'Hide' : 'Show'} narrower concepts of ${concept.label}`}
                        onClick={() => setIsOpen(!isOpen)}
                        className="rounded-full p-0.5 text-muted-foreground hover:bg-muted hover:text-foreground"
                    >
                        <ChevronRight
                            aria-hidden="true"
                            className={cn('size-4 transition-transform', isOpen && 'rotate-90')}
                        />
                    </button>
                ) : (
                    <span aria-hidden="true" className="w-5 flex-none" />
                )}
                <Checkbox
                    id={checkboxId}
                    checked={isSelected || isIncluded}
                    disabled={isIncluded}
                    onCheckedChange={() => onToggle(concept.notation)}
                    className="border-2"
                />
                <label
                    htmlFor={checkboxId}
                    title={concept.definition ?? undefined}
                    className={cn('text-[15px]', isIncluded && 'text-muted-foreground')}
                >
                    {concept.label}
                    {isIncluded ? <span className="sr-only">, included by {includedBy.label}</span> : null}
                </label>
            </div>
            {hasNarrower && isOpen ? (
                <ul className="flex flex-col pl-5">
                    {concept.narrower.map((child) => (
                        <CategoryNode
                            key={child.notation}
                            concept={child}
                            selected={selected}
                            onToggle={onToggle}
                            includedBy={includedBy ?? (isSelected ? concept : null)}
                        />
                    ))}
                </ul>
            ) : null}
        </li>
    );
}
