import { ChevronDown } from 'lucide-react';
import { useState } from 'react';
import type { ProductResult, SearchResponse, TaxonomyNode } from '@/api/client';
import { FlaggedCard } from '@/components/FlaggedCard';
import { ResultRow } from '@/components/ResultRow';
import { formatMilliseconds, formatResultRange } from '@/lib/format';
import { groupByConcept } from '@/lib/resultGroups';
import { scoreMeaning } from '@/lib/signals';
import { categoryIcon, conceptLabel } from '@/lib/taxonomy';
import { readRuleChecks, readWantedConcepts } from '@/lib/traceDetails';

// ConceptGroupedResults — Stage 5 with its rules on: in concept · out of concept (kept, moved down) · a
// Flagged column. The near miss that Hybrid ranked 2nd is still here, beside the list, with the checks it
// failed. Nothing is hidden: out-of-concept items start collapsed but are counted and one click away.

// Enough out-of-concept rows to show what was moved down, without pushing the flagged column off a 720px projector.
const outOfConceptPreviewSize = 4;

export interface ConceptGroupedResultsProps {
    response: SearchResponse;
    taxonomy: TaxonomyNode[] | null;
    targetProductId: string | null;
}

export function ConceptGroupedResults({ response, taxonomy, targetProductId }: ConceptGroupedResultsProps) {
    const [isOutOfConceptExpanded, setIsOutOfConceptExpanded] = useState(false);

    const groups = groupByConcept(response.results, targetProductId);
    const checks = readRuleChecks(response);
    const wantedLabels = readWantedConcepts(response).map((notation) => conceptLabel(taxonomy, notation));

    const firstRank = (response.page - 1) * response.pageSize + 1;
    const visibleOutOfConcept = isOutOfConceptExpanded
        ? groups.outOfConcept
        : groups.outOfConcept.slice(0, outOfConceptPreviewSize);
    const hiddenCount = groups.outOfConcept.length - visibleOutOfConcept.length;

    // Ranks follow the API's order across the groups, so the numbers show where Stage 5 put each item.
    // An out-of-concept row shows that badge instead of "Not evaluated"; a rule result (the Compatible SSD) stays.
    function renderRow(product: ProductResult, isOutOfConcept: boolean) {
        return (
            <ResultRow
                key={product.id}
                product={product}
                rank={firstRank + response.results.indexOf(product)}
                icon={categoryIcon(taxonomy, product.categories)}
                showConcept={isOutOfConcept}
                hideNotEvaluated={isOutOfConcept}
            />
        );
    }

    return (
        <div className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,26rem)]">
            <section aria-labelledby="in-concept-heading" className="flex flex-col gap-2">
                <p className="flex flex-wrap items-baseline gap-x-2 text-sm">
                    <span
                        id="in-concept-heading"
                        className="font-bold tracking-wide text-ontology-ink uppercase"
                    >
                        {wantedLabels.length > 0
                            ? `In concept: ${wantedLabels.join(', ')}`
                            : 'No concept matched'}
                    </span>
                    <span className="text-muted-foreground">
                        {formatResultRange(firstRank, response.results.length, response.totalResults)} ·{' '}
                        {scoreMeaning('ontology')} · {formatMilliseconds(response.executionTimeMs)}
                    </span>
                </p>
                <div className="overflow-hidden rounded-card border-2 bg-card">
                    {groups.inConcept.length > 0 ? (
                        <ol>{groups.inConcept.map((product) => renderRow(product, false))}</ol>
                    ) : (
                        <p className="px-4 py-2 text-muted-foreground">Nothing in concept.</p>
                    )}
                    {groups.outOfConcept.length > 0 ? (
                        <>
                            <h3 className="border-y-2 bg-muted px-4 py-1.5 text-sm font-bold tracking-wide text-muted-foreground uppercase">
                                Out of concept · kept, moved down · {groups.outOfConcept.length}
                            </h3>
                            <ol id="out-of-concept-rows">
                                {visibleOutOfConcept.map((product) => renderRow(product, true))}
                            </ol>
                            {groups.outOfConcept.length > outOfConceptPreviewSize ? (
                                <button
                                    type="button"
                                    aria-expanded={isOutOfConceptExpanded}
                                    aria-controls="out-of-concept-rows"
                                    onClick={() => setIsOutOfConceptExpanded(!isOutOfConceptExpanded)}
                                    className="flex w-full items-center gap-1.5 border-t-2 px-4 py-1.5 text-left text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
                                >
                                    <ChevronDown
                                        aria-hidden="true"
                                        className={isOutOfConceptExpanded ? 'size-4 rotate-180' : 'size-4'}
                                    />
                                    {isOutOfConceptExpanded ? 'Show fewer' : `Show ${hiddenCount} more`}
                                </button>
                            ) : null}
                        </>
                    ) : null}
                </div>
            </section>

            <section aria-labelledby="flagged-heading" className="flex flex-col gap-2">
                <p className="flex flex-wrap items-baseline gap-x-2 text-sm">
                    <span
                        id="flagged-heading"
                        className="font-bold tracking-wide text-incompatible-ink uppercase"
                    >
                        Flagged · {groups.flagged.length} incompatible
                    </span>
                    <span className="text-muted-foreground">never hidden</span>
                </p>
                {groups.flagged.length > 0 ? (
                    groups.flagged.map((product) => (
                        <FlaggedCard
                            key={product.id}
                            product={product}
                            checks={checks?.filter((check) => check.candidateId === product.id) ?? null}
                            icon={categoryIcon(taxonomy, product.categories)}
                        />
                    ))
                ) : (
                    <p className="rounded-card border-2 bg-card px-4 py-2.5 text-muted-foreground">
                        {targetProductId === null
                            ? 'Nothing flagged. Choose the device you own: without one, no rule can fail.'
                            : 'Nothing flagged: every product a rule applies to fits your device.'}
                    </p>
                )}
            </section>
        </div>
    );
}
