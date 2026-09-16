import { useRef, type KeyboardEvent } from 'react';
import {
    pipelineStages,
    stageColourClasses,
    stageGroup,
    stageLabel,
    stageNumber,
    triadGroupClasses,
    triadGroups,
    type PipelineStage,
} from '@/lib/stageGroup';
import { cn } from '@/lib/utils';

// PipelineStepper — the seven stages under the triad question each one answers, so the audience can see where in the
// argument they are. An ARIA tablist: ←/→ (and Home/End) move to a stage and search it.

export interface PipelineStepperProps {
    stage: PipelineStage;
    onChooseStage: (stage: PipelineStage) => void;
}

export function PipelineStepper({ stage, onChooseStage }: PipelineStepperProps) {
    const tabRefs = useRef(new Map<PipelineStage, HTMLButtonElement>());

    function handleKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
        const index = pipelineStages.indexOf(stage);
        const count = pipelineStages.length;
        let next: PipelineStage | undefined;

        switch (event.key) {
            case 'ArrowRight':
                next = pipelineStages[(index + 1) % count];
                break;
            case 'ArrowLeft':
                next = pipelineStages[(index - 1 + count) % count];
                break;
            case 'Home':
                next = pipelineStages[0];
                break;
            case 'End':
                next = pipelineStages.at(-1);
                break;
            default:
                return;
        }

        event.preventDefault();
        if (next !== undefined) {
            onChooseStage(next);
            tabRefs.current.get(next)?.focus();
        }
    }

    return (
        <div role="tablist" aria-label="Pipeline stages" className="flex flex-wrap items-end gap-4">
            {triadGroups.map(({ group, label, question }) => {
                const groupColours = triadGroupClasses(group);
                const captionId = `pipeline-group-${group}`;

                return (
                    <div
                        key={group}
                        role="none"
                        className={cn('flex flex-col gap-0.5 border-t-4 pt-1', groupColours.border)}
                    >
                        <span id={captionId} className="text-[13px] whitespace-nowrap text-muted-foreground">
                            <b className={cn('tracking-[.08em] uppercase', groupColours.ink)}>{label}</b> ·{' '}
                            {question}
                        </span>
                        <div role="none" className="flex">
                            {pipelineStages
                                .filter((candidate) => stageGroup(candidate) === group)
                                .map((candidate) => {
                                    const isSelected = candidate === stage;
                                    const colours = stageColourClasses(candidate);

                                    return (
                                        <button
                                            key={candidate}
                                            ref={(element) => {
                                                if (element === null) {
                                                    tabRefs.current.delete(candidate);
                                                } else {
                                                    tabRefs.current.set(candidate, element);
                                                }
                                            }}
                                            type="button"
                                            role="tab"
                                            aria-selected={isSelected}
                                            aria-describedby={captionId}
                                            tabIndex={isSelected ? 0 : -1}
                                            onClick={() => onChooseStage(candidate)}
                                            onKeyDown={handleKeyDown}
                                            className={cn(
                                                'flex items-center gap-2 rounded-full border-2 border-transparent py-1 pr-3 pl-1 text-[17px] font-bold whitespace-nowrap',
                                                isSelected
                                                    ? [colours.fill, colours.onFill]
                                                    : 'hover:bg-muted',
                                            )}
                                        >
                                            <span
                                                className={cn(
                                                    'flex size-[26px] items-center justify-center rounded-full text-base',
                                                    isSelected
                                                        ? 'bg-white/30'
                                                        : ['border-2', colours.border, colours.ink],
                                                )}
                                            >
                                                {stageNumber(candidate)}
                                            </span>
                                            {stageLabel(candidate)}
                                        </button>
                                    );
                                })}
                        </div>
                    </div>
                );
            })}
        </div>
    );
}
