import type { SearchResponse, TaxonomyNode } from '@/api/client';
import { ConceptGroupedResults } from '@/components/ConceptGroupedResults';
import { ResultRow } from '@/components/ResultRow';
import { formatMilliseconds, formatResultRange } from '@/lib/format';
import { scoreMeaning, signalBadges } from '@/lib/signals';
import { isPipelineStage } from '@/lib/stageGroup';
import { categoryIcon } from '@/lib/taxonomy';

// ResultsTab — the candidates a stage returned, in that stage's order. Stages 1–4 show one list with each
// technique's signals, so the same query can be compared stage by stage. Stage 5 with its rules on splits
// the list into its groups, so the audience sees the near miss kept and flagged, not removed (ADR-0013).

export interface ResultsTabProps {
    response: SearchResponse;
    /** For category labels and icons; `null` while it loads. */
    taxonomy: TaxonomyNode[] | null;
    targetProductId: string | null;
    /** Stage 5's rules switch. With it off, Stage 5 shows one list with its concept badges, nothing demoted. */
    applyConstraints: boolean;
}

export function ResultsTab({ response, taxonomy, targetProductId, applyConstraints }: ResultsTabProps) {
    if (response.results.length === 0) {
        return (
            <div className="flex flex-col gap-1 rounded-card border-2 bg-card px-5 py-4">
                <p className="font-bold">No products matched</p>
                <p className="text-muted-foreground">Try another query, or clear the filters.</p>
            </div>
        );
    }

    const stage = isPipelineStage(response.stage) ? response.stage : null;
    // Stages 6–7 retrieve nothing new: they return Stage 5's results and explain them (ADR-0016), so they show them the same way.
    const showsStageFiveResults = stage === 'ontology' || stage === 'rag' || stage === 'pedagogy';

    if (showsStageFiveResults && applyConstraints) {
        return (
            <ConceptGroupedResults
                response={response}
                taxonomy={taxonomy}
                targetProductId={targetProductId}
            />
        );
    }

    const firstRank = (response.page - 1) * response.pageSize + 1;
    const meaning = stage === null ? null : scoreMeaning(stage);

    return (
        <section aria-label="Results" className="overflow-hidden rounded-card border-2 bg-card">
            <p className="border-b-2 px-4 py-2 text-sm text-muted-foreground">
                {formatResultRange(firstRank, response.results.length, response.totalResults)} ·{' '}
                {formatMilliseconds(response.executionTimeMs)}
                {meaning === null ? null : ` · ${meaning}`}
            </p>
            <ol>
                {response.results.map((product, index) => (
                    <ResultRow
                        key={product.id}
                        product={product}
                        rank={firstRank + index}
                        icon={categoryIcon(taxonomy, product.categories)}
                        signals={stage === null ? [] : signalBadges(stage, product)}
                        showConcept={showsStageFiveResults}
                    />
                ))}
            </ol>
        </section>
    );
}
