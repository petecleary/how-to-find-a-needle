import { ArrowRight } from 'lucide-react';
import { TraceSection, traceChipClass, traceTableClass } from '@/components/trace/TraceSection';
import type { FilterDetails } from '@/lib/traceDetails';

// FilterSummary — the structured pre-filters a step ran (ADR-0007). Stages 2-7 fold Stage 1's WHERE fragments
// into their own statement, so this view shows when a short result list is the filter's doing rather than the
// technique's, and where a broad category quietly became its narrower concepts.

export interface FilterSummaryProps {
    details: FilterDetails;
    /** True on Stage 1, where the filters are the whole technique rather than a pre-filter under a ranking. */
    isStructuredStage: boolean;
}

export function FilterSummary({ details, isStructuredStage }: FilterSummaryProps) {
    const expansions = Object.entries(details.categoryExpansion);

    return (
        <TraceSection
            title={isStructuredStage ? 'Filters' : 'Structured pre-filters (Stage 1)'}
            className="gap-3"
        >
            {isStructuredStage ? null : (
                <p>
                    The same fragments Stage 1 builds, ANDed into this step's WHERE clause below. They narrow
                    the catalogue first; this stage's ranking only orders what survives them.
                </p>
            )}

            <div className="overflow-x-auto">
                <table className={traceTableClass}>
                    <thead>
                        <tr>
                            <th scope="col">Filter</th>
                            <th scope="col">Condition</th>
                            <th scope="col">From the request</th>
                        </tr>
                    </thead>
                    <tbody>
                        {details.applied.map(({ filter, condition, requested }) => (
                            <tr key={filter}>
                                <td className="font-mono whitespace-nowrap">{filter}</td>
                                <td className="font-mono">{condition}</td>
                                <td className="font-mono break-all">{JSON.stringify(requested) ?? '—'}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            {expansions.length === 0 ? null : (
                <div className="flex flex-col gap-1.5">
                    {expansions.map(([category, concepts]) => (
                        <p key={category} className="flex flex-wrap items-center gap-1.5">
                            <span className="font-mono text-[13px]">{category}</span>
                            <ArrowRight aria-hidden="true" className="size-3.5 text-muted-foreground" />
                            <span className="sr-only">matches</span>
                            {concepts.map((concept) => (
                                <span key={concept} className={traceChipClass}>
                                    {concept}
                                </span>
                            ))}
                        </p>
                    ))}
                    <p className="text-xs text-muted-foreground">
                        A broad category matches its narrower concepts too, so @categories holds every one of
                        them. The ontology decides that, not a hard-coded list.
                    </p>
                </div>
            )}
        </TraceSection>
    );
}
