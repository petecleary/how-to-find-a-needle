import { Folder, FolderCheck, FolderMinus, type LucideIcon } from 'lucide-react';
import type { ConceptMatch } from '@/api/client';
import { cn } from '@/lib/utils';

// Stage 5's classification (ADR-0013): whether any of a product's categories sits under a concept the
// query asked for. It is reported even with the rules off, so the presenter can show it before turning
// the demotion on.

const badges: Record<
    NonNullable<ConceptMatch>,
    { label: string; Icon: LucideIcon; description: string; className: string }
> = {
    InConcept: {
        label: 'In concept',
        Icon: FolderCheck,
        description: 'A category is a concept the query asked for, or narrower than one',
        className: 'text-ontology-ink',
    },
    OutOfConcept: {
        label: 'Out of concept',
        Icon: FolderMinus,
        description: 'No category sits under a concept the query asked for',
        className: 'text-muted-foreground',
    },
    NoConcept: {
        label: 'No concept',
        Icon: Folder,
        description: 'The query matched no taxonomy concept, so nothing was classified',
        className: 'text-muted-foreground',
    },
};

export interface ConceptBadgeProps {
    conceptMatch: ConceptMatch | undefined;
}

export function ConceptBadge({ conceptMatch }: ConceptBadgeProps) {
    if (conceptMatch == null) {
        return null;
    }

    const { label, Icon, description, className } = badges[conceptMatch];

    return (
        <span
            title={description}
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
