import { TriangleAlert } from 'lucide-react';
import { cn } from '@/lib/utils';

// CitationChip — one [PROD-…] citation in the model's text. Its colour is that product's verdict in the evidence set,
// so a citation of the near miss reads red at a glance, and a citation of a product the model was never given is
// marked invalid once the API's validation says so (ADR-0016). Clicking it shows the product in the evidence set.

export type CitationStatus =
    'Compatible' | 'Incompatible' | 'Unknown' | 'NotChecked' | 'TargetDevice' | 'NotInEvidence' | 'Checking';

// Status colours, not brand colours (ADR-0014): red for Incompatible, never the Pedagogy orange.
const styles: Record<CitationStatus, { className: string; description: string }> = {
    Compatible: {
        className: 'border-compatible-ink bg-compatible-tint text-compatible-ink',
        description: 'Compatible',
    },
    Incompatible: {
        className: 'border-incompatible-ink bg-incompatible-tint text-incompatible-ink',
        description: 'Incompatible',
    },
    Unknown: {
        className: 'border-unknown-ink bg-unknown-tint text-unknown-ink',
        description: 'compatibility unknown',
    },
    NotChecked: { className: 'border-border text-foreground', description: 'no rule checked' },
    TargetDevice: { className: 'border-border text-foreground', description: 'your device' },
    NotInEvidence: {
        className: 'border-dashed border-incompatible-ink text-incompatible-ink',
        description: 'not in the evidence: the model was never given this product',
    },
    Checking: {
        className: 'border-border text-muted-foreground',
        description: 'not checked against the evidence yet',
    },
};

export interface CitationChipProps {
    productId: string;
    productName: string | null;
    status: CitationStatus;
    isSelected: boolean;
    onSelect: (productId: string) => void;
}

export function CitationChip({ productId, productName, status, isSelected, onSelect }: CitationChipProps) {
    const { className, description } = styles[status];
    const label = `${productId}${productName === null ? '' : `, ${productName}`}: ${description}`;

    return (
        <button
            type="button"
            title={label}
            aria-label={`${label}. Show it in the evidence set`}
            aria-pressed={isSelected}
            onClick={() => onSelect(productId)}
            className={cn(
                'mx-0.5 inline-flex items-center gap-1 rounded-full border-2 px-2 align-baseline font-mono text-[0.8em] leading-snug font-bold whitespace-nowrap',
                className,
                isSelected && 'ring-2 ring-ring ring-offset-1',
            )}
        >
            {status === 'NotInEvidence' ? <TriangleAlert aria-hidden="true" className="size-3.5" /> : null}
            {productId}
        </button>
    );
}
