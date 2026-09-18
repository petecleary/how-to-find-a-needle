import { LoaderCircle } from 'lucide-react';
import { Tabs as TabsPrimitive } from 'radix-ui';
import { useState } from 'react';
import type { SearchResponse, TraceStep } from '@/api/client';
import { TraceFlow } from '@/components/trace/TraceFlow';
import { TraceStepView } from '@/components/trace/TraceStepView';
import { formatMilliseconds } from '@/lib/format';

// UnderTheHoodTab — the trace (ADR-0003): every step that produced the results, as a flow of chips, and the
// selected step drawn by a view built for what it holds. "The trace is a feature": the SQL, the tsquery, the
// distances, the RRF maths, the rule checks and, on Stages 6–7, the prompts and validation are the talk's evidence,
// not debugging output.

export interface UnderTheHoodTabProps {
    response: SearchResponse;
    /** Stages 6–7: the answer's own steps (prompt, generation, validation), from the stream's `done` event. */
    answerTrace?: TraceStep[];
    /** True while the answer is still streaming, so its steps aren't here yet. */
    isGenerating?: boolean;
}

export function UnderTheHoodTab({ response, answerTrace = [], isGenerating = false }: UnderTheHoodTabProps) {
    // The results' steps, then the answer's: the pipeline in the order it ran.
    const steps = [...response.debugTrace.steps, ...answerTrace];

    // The shape of this trace, which is the stage's pipeline. It changes when the stage does, and not when the
    // same stage runs a different query.
    const traceShape = steps.map((traceStep) => traceStep.title).join(' → ');
    const [chosen, setChosen] = useState<{ traceShape: string; index: number } | null>(null);

    // It opens on the first step and the presenter walks forward, so the trace is read as the story of how the
    // results were made. A choice made against a different pipeline means nothing, so changing stage starts
    // again at the beginning; re-running the same stage with a new query keeps the step being compared.
    const selectedIndex =
        chosen !== null && chosen.traceShape === traceShape && chosen.index < steps.length ? chosen.index : 0;
    const step = steps[selectedIndex];

    if (step === undefined) {
        return (
            <p className="rounded-card border-2 bg-card px-5 py-4 text-muted-foreground">
                This stage wrote no trace steps.
            </p>
        );
    }

    return (
        <TabsPrimitive.Root
            value={String(selectedIndex)}
            onValueChange={(value) => setChosen({ traceShape, index: Number(value) })}
            className="flex flex-col gap-3"
        >
            <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
                <TraceFlow steps={steps} selectedIndex={selectedIndex} />
                <span className="ml-auto text-sm text-muted-foreground">
                    {steps.length} {steps.length === 1 ? 'step' : 'steps'} ·{' '}
                    {formatMilliseconds(response.executionTimeMs)} for the results
                </span>
            </div>
            {isGenerating ? (
                <p role="status" className="flex items-center gap-2 text-sm text-muted-foreground">
                    <LoaderCircle aria-hidden="true" className="size-4 animate-spin" />
                    Generating: the prompt, generation and validation steps appear here when the answer is
                    done.
                </p>
            ) : null}
            <TabsPrimitive.Content
                value={String(selectedIndex)}
                className="rounded-card border-2 bg-card px-5 py-4"
            >
                <TraceStepView step={step} index={selectedIndex} response={response} />
            </TabsPrimitive.Content>
        </TabsPrimitive.Root>
    );
}
