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
    const [chosenIndex, setChosenIndex] = useState<number | null>(null);

    // It opens on the last step: that is where the stage's own technique runs, after any shared retrieval.
    const selectedIndex = chosenIndex !== null && chosenIndex < steps.length ? chosenIndex : steps.length - 1;
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
            onValueChange={(value) => setChosenIndex(Number(value))}
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
