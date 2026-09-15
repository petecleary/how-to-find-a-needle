import type { SearchResponse } from '@/api/client';
import { formatMilliseconds } from '@/lib/format';
import { isPipelineStage, stageColourClasses, stageLabel } from '@/lib/stageGroup';
import { cn } from '@/lib/utils';

// TODO(Phase 3): step 8 replaces this list with TraceFlow and a purpose-built renderer for each trace step.

export interface UnderTheHoodTabProps {
    response: SearchResponse;
}

/** The trace: every step that produced the results, coloured by the stage that ran it. */
export function UnderTheHoodTab({ response }: UnderTheHoodTabProps) {
    return (
        <ol className="flex flex-col gap-2">
            {response.debugTrace.steps.map((step, index) => {
                const knownStage = isPipelineStage(step.stage) ? step.stage : null;
                const colours = knownStage === null ? null : stageColourClasses(knownStage);

                return (
                    <li key={`${index}-${step.title}`} className="rounded-card border-2 bg-card px-4 py-2">
                        <div className="flex flex-wrap items-center gap-2">
                            <span
                                className={cn(
                                    'rounded-full px-2.5 text-sm font-bold',
                                    colours === null
                                        ? 'bg-muted text-muted-foreground'
                                        : [colours.tint, colours.ink],
                                )}
                            >
                                {knownStage === null ? step.stage : stageLabel(knownStage)}
                            </span>
                            <b>{step.title}</b>
                            <span className="ml-auto font-mono text-sm text-muted-foreground">
                                {formatMilliseconds(step.durationMs)}
                            </span>
                        </div>
                        {step.notes !== undefined && step.notes.length > 0 ? (
                            <ul className="mt-1 list-disc pl-5 text-sm text-muted-foreground">
                                {step.notes.map((note) => (
                                    <li key={note}>{note}</li>
                                ))}
                            </ul>
                        ) : null}
                    </li>
                );
            })}
        </ol>
    );
}
