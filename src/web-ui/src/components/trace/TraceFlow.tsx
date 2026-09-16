import { ArrowRight } from 'lucide-react';
import { Tabs as TabsPrimitive } from 'radix-ui';
import { Fragment } from 'react';
import type { TraceStep } from '@/api/client';
import { isPipelineStage, stageColourClasses } from '@/lib/stageGroup';
import { traceStepShortLabel } from '@/lib/traceStepKind';
import { cn } from '@/lib/utils';

// TraceFlow — the pipeline as it actually ran: one chip per trace step, coloured by the stage that ran it.
// Stage 5's flow shows Keyword, Vector and RRF chips in Search purple between its own green steps, so the
// audience sees that Stage 5 is Stage 4 with better input and rules after. A Radix tablist: ←/→ move along it.

export interface TraceFlowProps {
    steps: TraceStep[];
    selectedIndex: number;
}

export function TraceFlow({ steps, selectedIndex }: TraceFlowProps) {
    return (
        <TabsPrimitive.List
            aria-label="Trace steps"
            className="flex flex-wrap items-center gap-x-1.5 gap-y-2"
        >
            {steps.map((step, index) => {
                const colours = isPipelineStage(step.stage) ? stageColourClasses(step.stage) : null;
                const isSelected = index === selectedIndex;

                return (
                    <Fragment key={`${index}-${step.title}`}>
                        {index > 0 ? (
                            <ArrowRight
                                aria-hidden="true"
                                className="size-4 flex-none text-muted-foreground"
                            />
                        ) : null}
                        <TabsPrimitive.Trigger
                            value={String(index)}
                            title={step.title}
                            className={cn(
                                'rounded-full border-2 px-3 py-1 text-[15px] font-bold whitespace-nowrap',
                                colours === null ? 'border-border' : colours.border,
                                isSelected
                                    ? colours === null
                                        ? 'bg-foreground text-background'
                                        : [colours.fill, colours.onFill]
                                    : colours === null
                                      ? 'text-foreground'
                                      : colours.ink,
                            )}
                        >
                            {index + 1} {traceStepShortLabel(step)}
                        </TabsPrimitive.Trigger>
                    </Fragment>
                );
            })}
        </TabsPrimitive.List>
    );
}
