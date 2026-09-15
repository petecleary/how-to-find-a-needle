import type { SearchResponse } from '@/api/client';
import { formatMilliseconds, formatPrice } from '@/lib/format';

// TODO(Phase 3): step 7 replaces these plain rows with ResultRow (signal, concept and compatibility
// badges) and Stage 5's grouping into in concept · out of concept · flagged.

const compatibilityLabels = {
    NotEvaluated: 'Not evaluated',
    Compatible: 'Compatible',
    Incompatible: 'Incompatible',
    Unknown: 'Unknown',
} as const;

export interface ResultsTabProps {
    response: SearchResponse;
}

/** The candidates the stage returned, in its order. */
export function ResultsTab({ response }: ResultsTabProps) {
    if (response.results.length === 0) {
        return (
            <div className="flex flex-col gap-1 rounded-card border-2 bg-card px-5 py-4">
                <p className="font-bold">No products matched</p>
                <p className="text-muted-foreground">Try another query, or clear the filters.</p>
            </div>
        );
    }

    const firstRank = (response.page - 1) * response.pageSize + 1;

    return (
        <section aria-label="Results" className="overflow-hidden rounded-card border-2 bg-card">
            <p className="border-b-2 px-4 py-2 text-sm text-muted-foreground">
                {response.totalResults} results · {formatMilliseconds(response.executionTimeMs)}
            </p>
            <ol>
                {response.results.map((product, index) => (
                    <li
                        key={product.id}
                        className="flex items-center gap-3 border-b-2 px-4 py-1.5 last:border-b-0"
                    >
                        <span className="flex size-[26px] flex-none items-center justify-center rounded-full bg-muted text-sm font-bold">
                            {firstRank + index}
                        </span>
                        <div className="flex min-w-0 flex-1 flex-col leading-tight">
                            <b className="truncate text-[17px]">{product.name}</b>
                            <span className="text-[13px] text-muted-foreground">
                                <span className="font-mono">{product.id}</span> · {product.brand} ·{' '}
                                {formatPrice(product.price)}
                            </span>
                        </div>
                        <span className="rounded-full border-2 px-2.5 text-sm font-bold whitespace-nowrap text-muted-foreground">
                            {compatibilityLabels[product.compatibility.status]}
                        </span>
                    </li>
                ))}
            </ol>
        </section>
    );
}
