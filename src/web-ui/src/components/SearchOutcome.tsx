import { LoaderCircle, RotateCw, Search, TriangleAlert } from 'lucide-react';
import type { ReactNode } from 'react';
import { ApiError, type SearchResponse } from '@/api/client';
import { Button } from '@/components/ui/button';
import type { PipelineSearch } from '@/hooks/usePipelineSearch';
import { stageLabel } from '@/lib/stageGroup';

// SearchOutcome — what a tab shows until its response arrives: nothing to search for yet, searching, or
// the API's error with its fix-it guidance. Failures are shown, never hidden (root CLAUDE.md).

export interface SearchOutcomeProps {
    search: PipelineSearch;
    /** Renders the tab once the response has arrived. */
    children: (response: SearchResponse) => ReactNode;
}

export function SearchOutcome({ search, children }: SearchOutcomeProps) {
    const cardClass = 'flex flex-col items-start gap-2 rounded-card border-2 bg-card px-5 py-4';

    if (search.status === 'success' && search.response !== null) {
        return children(search.response);
    }

    if (search.status === 'idle') {
        return (
            <div className={cardClass}>
                <p className="flex items-center gap-2 font-bold">
                    <Search aria-hidden="true" className="size-5 text-muted-foreground" />
                    Nothing to search for yet
                </p>
                <p className="text-muted-foreground">
                    {stageLabel(search.stage)} needs a query. Type one and press Enter, or choose a golden
                    query.
                </p>
            </div>
        );
    }

    if (search.status === 'loading' || search.error === null) {
        return (
            <div role="status" className={cardClass}>
                <p className="flex items-center gap-2 font-bold">
                    <LoaderCircle aria-hidden="true" className="size-5 animate-spin text-muted-foreground" />
                    Searching with {stageLabel(search.stage)}…
                </p>
            </div>
        );
    }

    const apiError = search.error instanceof ApiError ? search.error : null;
    const title =
        apiError === null
            ? 'The Search API could not be reached'
            : (apiError.problem?.title ??
              `The ${stageLabel(search.stage)} search failed (${apiError.status})`);
    // fetch rejects without a status when nothing answered, e.g. the API isn't running.
    const message = apiError === null ? 'Is `aspire run` still running?' : apiError.message;

    return (
        <div role="alert" className={cardClass}>
            <p className="flex items-center gap-2 font-bold text-incompatible-ink">
                <TriangleAlert aria-hidden="true" className="size-5" />
                {title}
            </p>
            <p>{message}</p>
            {apiError !== null && apiError.validationErrors.length > 0 ? (
                <ul className="list-disc pl-5 text-muted-foreground">
                    {apiError.validationErrors.map((failure) => (
                        <li key={`${failure.name}: ${failure.reason}`}>
                            <span className="font-mono">{failure.name}</span>: {failure.reason}
                        </li>
                    ))}
                </ul>
            ) : null}
            <Button variant="outline" className="rounded-full border-2" onClick={search.rerun}>
                <RotateCw aria-hidden="true" />
                Try again
            </Button>
        </div>
    );
}
