import type { SearchResponse, TraceStep } from '@/api/client';
import { ClassificationTable } from '@/components/trace/ClassificationTable';
import { ConceptMatches } from '@/components/trace/ConceptMatches';
import { DistanceTable } from '@/components/trace/DistanceTable';
import { EmbeddingView } from '@/components/trace/EmbeddingView';
import { ExpansionView } from '@/components/trace/ExpansionView';
import { JsonFallback } from '@/components/trace/JsonFallback';
import { RrfTable } from '@/components/trace/RrfTable';
import { RuleChecks } from '@/components/trace/RuleChecks';
import { SqlBlock } from '@/components/trace/SqlBlock';
import { TraceSection } from '@/components/trace/TraceSection';
import { TsQueryView } from '@/components/trace/TsQueryView';
import { formatMilliseconds } from '@/lib/format';
import { isPipelineStage, stageColourClasses, stageLabel } from '@/lib/stageGroup';
import {
    productNames,
    readClassificationDetails,
    readConstrainDetails,
    readDistanceDetails,
    readEmbeddingDetails,
    readExpansionDetails,
    readKeywordDetails,
    readRrfDetails,
    readStructuredDetails,
    readUnderstandDetails,
} from '@/lib/traceDetails';
import { traceStepKind } from '@/lib/traceStepKind';
import { cn } from '@/lib/utils';

export interface TraceStepViewProps {
    step: TraceStep;
    /** The step's position in the trace, counting from 0. */
    index: number;
    response: SearchResponse;
}

/** One trace step: its title and timing, its purpose-built view, its SQL and parameters, and its notes. */
export function TraceStepView({ step, index, response }: TraceStepViewProps) {
    const knownStage = isPipelineStage(step.stage) ? step.stage : null;
    const colours = knownStage === null ? null : stageColourClasses(knownStage);
    const kind = traceStepKind(step);
    const notes = step.notes ?? [];

    // Stage 1's view is its SQL, so the statement comes first; elsewhere the SQL follows what it produced.
    const sqlBlock = step.sql ? <SqlBlock sql={step.sql} parameters={step.parameters ?? null} /> : null;

    return (
        <div className="flex flex-col gap-4">
            <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
                <h3 className="text-xl font-bold">
                    {index + 1} · {step.title}
                </h3>
                <span
                    className={cn(
                        'rounded-full px-2.5 text-sm font-bold',
                        colours === null ? 'bg-muted text-muted-foreground' : [colours.tint, colours.ink],
                    )}
                >
                    {knownStage === null ? step.stage : stageLabel(knownStage)}
                </span>
                <span className="ml-auto font-mono text-sm text-muted-foreground">
                    {formatMilliseconds(step.durationMs)}
                </span>
            </div>

            {kind === 'sql' ? sqlBlock : null}
            <StepDetails step={step} response={response} />
            {kind === 'sql' ? null : sqlBlock}

            {notes.length > 0 ? (
                <TraceSection title="Notes">
                    <ul className="flex list-disc flex-col gap-1 pl-5">
                        {notes.map((note) => (
                            <li key={note}>{note}</li>
                        ))}
                    </ul>
                </TraceSection>
            ) : null}
        </div>
    );
}

function StepDetails({ step, response }: Omit<TraceStepViewProps, 'index'>) {
    const details = step.details ?? {};
    const names = productNames(response);

    switch (traceStepKind(step)) {
        case 'sql': {
            const counts = readStructuredDetails(details);
            return (
                <>
                    <p>
                        <b>{counts.rowCount ?? '—'}</b> rows returned of <b>{counts.totalCount ?? '—'}</b>{' '}
                        that match the filters.
                    </p>
                    {counts.countSql === null ? null : (
                        <SqlBlock title="Count query" sql={counts.countSql} parameters={null} />
                    )}
                </>
            );
        }
        case 'tsquery':
            return <TsQueryView details={readKeywordDetails(details)} productNames={names} />;
        case 'embedding':
            return <EmbeddingView details={readEmbeddingDetails(details)} />;
        case 'distances':
            return <DistanceTable details={readDistanceDetails(details)} productNames={names} />;
        case 'rrf':
            return <RrfTable details={readRrfDetails(details)} response={response} />;
        case 'concept-matches':
            return <ConceptMatches details={readUnderstandDetails(details)} />;
        case 'expansion':
            return <ExpansionView details={readExpansionDetails(details)} />;
        case 'classification':
            return <ClassificationTable details={readClassificationDetails(details)} productNames={names} />;
        case 'rule-checks':
            return <RuleChecks details={readConstrainDetails(details)} productNames={names} />;
        case 'json':
            return <JsonFallback details={step.details} />;
    }
}
