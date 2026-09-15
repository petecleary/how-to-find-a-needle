import { useRef, type KeyboardEvent } from 'react';
import { isSearchStage, type SearchStage } from '@/api/client';
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

// PipelineStepper — the seven stages under the triad question each one answers, so the audience can see
// where in the argument they are. An ARIA tablist: ←/→ (and Home/End) move to a stage and search it.

export interface PipelineStepperProps {
    stage: PipelineStage;
    onChooseStage: (stage: SearchStage) => void;
}

export function PipelineStepper({ stage, onChooseStage }: PipelineStepperProps) {
    const tabRefs = useRef(new Map<PipelineStage, HTMLButtonElement>());

    // TODO(Phase 4): Stages 6–7 become selectable when their endpoints exist (SearchStage grows with the API).
    const selectableStages = pipelineStages.filter(isSearchStage);

    function handleKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
        const index = selectableStages.findIndex((candidate) => candidate === stage);
        const count = selectableStages.length;
        let next: SearchStage | undefined;

        switch (event.key) {
            case 'ArrowRight':
                next = selectableStages[(index + 1) % count];
                break;
            case 'ArrowLeft':
                next = selectableStages[(index - 1 + count) % count];
                break;
            case 'Home':
                next = selectableStages[0];
                break;
            case 'End':
                next = selectableStages.at(-1);
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
                                    const isSelectable = isSearchStage(candidate);
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
                                            aria-disabled={!isSelectable}
                                            aria-describedby={captionId}
                                            tabIndex={isSelected ? 0 : -1}
                                            title={
                                                isSelectable
                                                    ? undefined
                                                    : 'Stages 6–7 need an LLM: built in Phase 4'
                                            }
                                            onClick={() => {
                                                if (isSearchStage(candidate)) {
                                                    onChooseStage(candidate);
                                                }
                                            }}
                                            onKeyDown={handleKeyDown}
                                            className={cn(
                                                'flex items-center gap-2 rounded-full border-2 border-transparent py-1 pr-3 pl-1 text-[17px] font-bold whitespace-nowrap',
                                                isSelected
                                                    ? [colours.fill, colours.onFill]
                                                    : 'hover:bg-muted',
                                                !isSelectable &&
                                                    'cursor-not-allowed opacity-50 hover:bg-transparent',
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
