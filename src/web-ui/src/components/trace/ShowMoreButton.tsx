import { ChevronDown } from 'lucide-react';
import type { ShowMore } from '@/hooks/useShowMore';
import { cn } from '@/lib/utils';

export interface ShowMoreButtonProps<T> {
    showMore: ShowMore<T>;
}

/** "Show 40 more" under a long trace table, or "Show fewer" once it's open. */
export function ShowMoreButton<T>({ showMore }: ShowMoreButtonProps<T>) {
    if (!showMore.canExpand) {
        return null;
    }

    return (
        <button
            type="button"
            aria-expanded={showMore.isExpanded}
            onClick={showMore.toggle}
            className="flex items-center gap-1.5 self-start rounded-full px-2 py-0.5 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
        >
            <ChevronDown aria-hidden="true" className={cn('size-4', showMore.isExpanded && 'rotate-180')} />
            {showMore.isExpanded ? 'Show fewer' : `Show ${showMore.hiddenCount} more`}
        </button>
    );
}
