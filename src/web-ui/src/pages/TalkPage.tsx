import { useState } from 'react';
import { Navigate, useNavigate, useParams } from 'react-router';
import { getGoldenQueries } from '@/api/client';
import { AppHeader } from '@/components/AppHeader';
import { Markdown } from '@/components/Markdown';
import { StageScreen } from '@/components/StageScreen';
import { TalkCaption } from '@/components/TalkCaption';
import { TalkControls } from '@/components/TalkControls';
import { useApiData } from '@/hooks/useApiData';
import { useTalkKeys } from '@/hooks/useTalkKeys';
import type { SearchState, StageTab } from '@/lib/searchState';
import { isPipelineStage, pipelineStages, stageNumber, type PipelineStage } from '@/lib/stageGroup';
import { stageTabDefinitions } from '@/lib/stageTabs';
import {
    findStageStep,
    findTalkStep,
    nextPosition,
    positionPath,
    previousPosition,
    stepTab,
    talkMarkdown,
    talkStartPath,
    talkStepState,
    talkSteps,
    type TalkStep,
} from '@/lib/talk';
import { parseTalkTab, talkPath } from '@/lib/talkRoute';

/**
 * `/talk/:step/:tab?`: the talk, replacing slides (ADR-0014 § Pages). Intro and summary steps are full-width
 * content; stage steps show the live stage screen with the step's golden query and options. ← / → move one
 * step, landing on that step's tab; H / R / A / U / G jump to a tab within it.
 */
export function TalkPage() {
    const { step: stepId, tab: tabParam } = useParams();
    const navigate = useNavigate();

    const step = findTalkStep(stepId);
    const tab = parseTalkTab(tabParam);
    const position = step === null ? null : { stepId: step.id, tab };
    const next = position === null ? null : nextPosition(talkSteps, position);
    const previous = position === null ? null : previousPosition(talkSteps, position);

    useTalkKeys(next === null ? null : positionPath(next), previous === null ? null : positionPath(previous));

    if (step === null) {
        return <Navigate to={talkStartPath()} replace />;
    }

    // A stage step always shows a tab, so a bookmark or → lands on a definite place; other steps have none.
    if (step.kind === 'stage' && tab === null) {
        return <Navigate to={talkPath(step.id, stepTab(step) ?? undefined)} replace />;
    }
    if (step.kind !== 'stage' && tab !== null) {
        return <Navigate to={talkPath(step.id)} replace />;
    }

    const stepIndex = talkSteps.indexOf(step);
    const tabLabel = stageTabDefinitions.find((definition) => definition.tab === tab)?.label;
    const label = `Step ${stepIndex + 1} of ${talkSteps.length}${tabLabel === undefined ? '' : ` · ${tabLabel}`}`;

    const headerPosition =
        step.stage !== undefined && isPipelineStage(step.stage)
            ? `Stage ${stageNumber(step.stage)} of ${pipelineStages.length}`
            : step.title;

    return (
        <div className="flex min-h-screen flex-col">
            <AppHeader position={headerPosition} />
            {step.kind === 'stage' && tab !== null ? (
                <TalkStageStep
                    key={step.id}
                    step={step}
                    tab={tab}
                    onChooseTab={(nextTab) => navigate(talkPath(step.id, nextTab))}
                    onChooseStage={(nextStage) => {
                        // The stepper is a way through the talk, not just a stage switch: it moves the
                        // position, so the caption and the step's preset arrive with the stage.
                        const stageStep = findStageStep(nextStage);
                        if (stageStep !== null) {
                            navigate(talkPath(stageStep.id, stepTab(stageStep) ?? undefined));
                        }
                    }}
                />
            ) : (
                <TalkContentStep step={step} />
            )}
            <TalkControls
                previousPath={previous === null ? null : positionPath(previous)}
                nextPath={next === null ? null : positionPath(next)}
                label={label}
            />
        </div>
    );
}

interface TalkStageStepProps {
    step: TalkStep;
    tab: StageTab;
    onChooseTab: (tab: StageTab) => void;
    onChooseStage: (stage: PipelineStage) => void;
}

// The step starts from its golden query and options. The presenter can still change anything live (a toggle,
// the query); those changes last until the talk moves to another step, which starts fresh.
function TalkStageStep({ step, tab, onChooseTab, onChooseStage }: TalkStageStepProps) {
    const goldenQueries = useApiData(getGoldenQueries);
    const [editedState, setEditedState] = useState<SearchState | null>(null);

    if (step.goldenQuery !== undefined && goldenQueries.status === 'loading') {
        return <main className="flex-1 px-6 py-4 text-muted-foreground">Loading {step.goldenQuery}…</main>;
    }

    const preset = goldenQueries.data?.find((goldenQuery) => goldenQuery.id === step.goldenQuery) ?? null;
    const state: SearchState = { ...(editedState ?? talkStepState(step, preset)), tab };
    const markdown = talkMarkdown(step);
    const stepStage = step.stage !== undefined && isPipelineStage(step.stage) ? step.stage : state.stage;

    function handleStateChange(next: SearchState) {
        setEditedState(next);
        if (next.tab !== tab) {
            onChooseTab(next.tab);
        }
    }

    return (
        <StageScreen
            state={state}
            onStateChange={handleStateChange}
            goldenQueries={goldenQueries}
            filterLayout="drawer"
            onChooseStage={onChooseStage}
            caption={
                markdown === null ? null : (
                    <TalkCaption
                        stage={stepStage}
                        markdown={markdown}
                        onOpenHowItWorks={() => onChooseTab('how-it-works')}
                    />
                )
            }
        />
    );
}

function TalkContentStep({ step }: { step: TalkStep }) {
    const markdown = talkMarkdown(step);

    return (
        <main className="mx-auto flex w-full max-w-5xl flex-1 flex-col gap-6 px-6 py-10">
            <h1 className="text-5xl font-bold">{step.title}</h1>
            {markdown === null ? (
                <p role="alert" className="text-incompatible-ink">
                    content/{step.file} doesn't exist.
                </p>
            ) : (
                <Markdown className="gap-5 text-2xl leading-relaxed">{markdown}</Markdown>
            )}
        </main>
    );
}
