import { RotateCw, Search, TriangleAlert } from 'lucide-react';
import { useState } from 'react';
import { ApiError, type AnswerStage } from '@/api/client';
import { AnswerPanel } from '@/components/AnswerPanel';
import { CitationChip, type CitationStatus } from '@/components/CitationChip';
import { EvidenceSet } from '@/components/EvidenceSet';
import { ExplanationPanel } from '@/components/ExplanationPanel';
import { Button } from '@/components/ui/button';
import type { AnswerStream } from '@/hooks/useAnswerStream';
import type { PipelineSearch } from '@/hooks/usePipelineSearch';
import { citedProductIds } from '@/lib/citations';
import type { Audience } from '@/lib/searchState';
import { stageLabel } from '@/lib/stageGroup';
import { readEvidence, type EvidenceItem } from '@/lib/traceDetails';
import { cn } from '@/lib/utils';

// AnswerTab — Stages 6–7's generated text beside exactly what the model was given (ADR-0014 § Stage screen): the
// answer, Stage 7's explanation, and the evidence set. Citation chips tie the text to the evidence, so every claim
// can be checked against the product it names.

export interface AnswerTabProps {
    stage: AnswerStage;
    stream: AnswerStream;
    search: PipelineSearch;
    audience: Audience;
    applyPedagogy: boolean;
}

export function AnswerTab({ stage, stream, search, audience, applyPedagogy }: AnswerTabProps) {
    const [selectedProductId, setSelectedProductId] = useState<string | null>(null);

    if (stream.status === 'idle') {
        return (
            <div className="flex flex-col items-start gap-2 rounded-card border-2 bg-card px-5 py-4">
                <p className="flex items-center gap-2 font-bold">
                    <Search aria-hidden="true" className="size-5 text-muted-foreground" />
                    Nothing to answer yet
                </p>
                <p className="text-muted-foreground">
                    {stageLabel(stage)} needs a query. Type one and press Enter, or choose a golden query.
                </p>
            </div>
        );
    }

    const evidence = search.response === null ? null : readEvidence(search.response);
    const { answer, explanation } = stream.sections;
    const invalidIds = [
        ...new Set([
            ...(answer.final?.invalidCitations ?? []),
            ...(explanation.final?.invalidCitations ?? []),
        ]),
    ];
    const citedIds = new Set([...citedProductIds(answer.markdown), ...citedProductIds(explanation.markdown)]);
    const hasFailed = stream.status === 'error';

    function renderProductLink(productId: string) {
        const item = evidence?.items.find((candidate) => candidate.id === productId) ?? null;

        return (
            <CitationChip
                productId={productId}
                productName={item?.name ?? null}
                status={citationStatus(productId, item, invalidIds)}
                isSelected={productId === selectedProductId}
                onSelect={(id) => setSelectedProductId((current) => (current === id ? null : id))}
            />
        );
    }

    return (
        <div
            className={cn(
                'grid items-start gap-4',
                stage === 'pedagogy'
                    ? 'lg:grid-cols-[minmax(0,1fr)_minmax(0,1.5fr)_minmax(0,0.9fr)]'
                    : 'lg:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)]',
            )}
        >
            <div className="flex flex-col gap-3">
                {hasFailed && stream.error !== null ? (
                    <AnswerProblem error={stream.error} onRetry={stream.rerun} />
                ) : null}
                <AnswerPanel
                    section={answer}
                    meta={stream.meta}
                    isStreaming={stream.activeSection === 'answer'}
                    hasFailed={hasFailed}
                    timeToFirstTokenMs={stream.done?.timeToFirstTokenMs ?? null}
                    totalMs={stage === 'rag' ? (stream.done?.totalMs ?? null) : null}
                    renderProductLink={renderProductLink}
                />
            </div>

            {stage === 'pedagogy' ? (
                <ExplanationPanel
                    section={explanation}
                    audience={audience}
                    applyPedagogy={applyPedagogy}
                    isStreaming={stream.activeSection === 'explanation'}
                    isWaitingForAnswer={answer.final === null}
                    hasFailed={hasFailed}
                    totalMs={stream.done?.totalMs ?? null}
                    renderProductLink={renderProductLink}
                />
            ) : null}

            <EvidenceSet
                evidence={evidence}
                isLoading={search.status === 'loading'}
                citedIds={citedIds}
                invalidIds={invalidIds}
                selectedProductId={selectedProductId}
                onSelect={setSelectedProductId}
            />
        </div>
    );
}

// The API decides validity; until a section's final event arrives, a citation of an unknown product is only "checking".
function citationStatus(
    productId: string,
    item: EvidenceItem | null,
    invalidIds: readonly string[],
): CitationStatus {
    if (invalidIds.includes(productId)) {
        return 'NotInEvidence';
    }

    return item === null ? 'Checking' : item.role;
}

interface AnswerProblemProps {
    error: Error;
    onRetry: () => void;
}

// The answer failed, but the results didn't: they come from a separate request (ADR-0016).
function AnswerProblem({ error, onRetry }: AnswerProblemProps) {
    const apiError = error instanceof ApiError ? error : null;

    return (
        <div role="alert" className="flex flex-col items-start gap-2 rounded-card border-2 bg-card px-5 py-4">
            <p className="flex items-center gap-2 font-bold text-incompatible-ink">
                <TriangleAlert aria-hidden="true" className="size-5" />
                {apiError?.problem?.title ?? 'The answer failed'}
            </p>
            <p>{error.message}</p>
            <p className="text-sm text-muted-foreground">
                The results are unaffected: they come from a separate request.
            </p>
            <Button variant="outline" className="rounded-full border-2" onClick={onRetry}>
                <RotateCw aria-hidden="true" />
                Try again
            </Button>
        </div>
    );
}
