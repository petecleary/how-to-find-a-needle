import type { GoldenQuery } from '@/api/client';
import talkJson from '../../content/talk.json';
import {
    applyGoldenQuery,
    defaultSearchState,
    stageTabs,
    type Audience,
    type SearchState,
    type StageTab,
} from './searchState';
import { isPipelineStage, type PipelineStage } from './stageGroup';
import { isTabAvailable } from './stageTabs';
import { talkPath } from './talkRoute';

// Talk mode replaces slides (ADR-0014 § Pages). content/talk.json lists the steps in order; each has a
// markdown file, and a stage step also names its stage, a golden query and any preset options.
//
// → moves one step, landing on that step's `tab`. A talk is a sequence of arguments, not of panels, so the
// arrow keys change the argument; the letter keys (H R A U G) move between tabs within a step, and never
// change where → leads. The position lives in the route, /talk/:step/:tab?.

export type TalkStepKind = 'intro' | 'stage' | 'summary';

export interface TalkStepOptions {
    expandSynonyms?: boolean;
    applyConstraints?: boolean;
    audience?: Audience;
    applyPedagogy?: boolean;
}

export interface TalkStep {
    id: string;
    kind: TalkStepKind;
    title: string;
    /** Path under content/, e.g. "talk/intro.md". A stage step's file is the caption above its tabs. */
    file: string;
    stage?: string;
    goldenQuery?: string;
    options?: TalkStepOptions;
    /** The one tab a stage step lands on. Defaults to Results, or Answer in Stages 6–7. */
    tab?: string;
}

// JSON types strings loosely, so the kinds and stages are checked at runtime by talk.test.ts instead.
export const talkSteps: readonly TalkStep[] = talkJson as TalkStep[];

export interface TalkPosition {
    stepId: string;
    /** The tab on a stage step; `null` on intro and summary steps. */
    tab: StageTab | null;
}

const talkFiles = import.meta.glob<string>('../../content/talk/*.md', {
    query: '?raw',
    import: 'default',
    eager: true,
});

/** Each talk file's markdown, by its path under content/: `{ "talk/intro.md": "Every catalogue…" }`. */
export const talkFileMarkdown: Readonly<Record<string, string>> = Object.fromEntries(
    Object.entries(talkFiles).map(([path, markdown]) => [path.replace(/^.*\/content\//, ''), markdown]),
);

export function talkMarkdown(step: TalkStep): string | null {
    return talkFileMarkdown[step.file] ?? null;
}

export function findTalkStep(
    id: string | undefined,
    steps: readonly TalkStep[] = talkSteps,
): TalkStep | null {
    return steps.find((step) => step.id === id) ?? null;
}

export function isStageTab(value: string): value is StageTab {
    return stageTabs.some((tab) => tab === value);
}

/**
 * The tab a stage step lands on: its own `tab` when that tab exists and the stage has it, or How it works —
 * the technique is explained before its results are argued about. Intro and summary steps have none.
 */
export function stepTab(step: TalkStep): StageTab | null {
    if (step.kind !== 'stage') {
        return null;
    }

    const stage = step.stage !== undefined && isPipelineStage(step.stage) ? step.stage : null;

    if (step.tab !== undefined && isStageTab(step.tab)) {
        if (stage === null || isTabAvailable(step.tab, stage)) {
            return step.tab;
        }
    }

    return 'how-it-works';
}

/**
 * The step that presents a stage, so clicking the stepper in talk mode jumps to that moment in the talk —
 * its caption, its golden query and its preset options — rather than changing the stage inside the step.
 */
export function findStageStep(stage: PipelineStage, steps: readonly TalkStep[] = talkSteps): TalkStep | null {
    return steps.find((step) => step.kind === 'stage' && step.stage === stage) ?? null;
}

/** Where a step sits in the talk, or `null` when the id isn't one of the steps. */
function positionOf(steps: readonly TalkStep[], offset: number, stepId: string): TalkPosition | null {
    const index = steps.findIndex((step) => step.id === stepId);
    const step = index === -1 ? undefined : steps[index + offset];

    return step === undefined ? null : { stepId: step.id, tab: stepTab(step) };
}

/**
 * Where → goes, or `null` at the end of the talk: the next step, whatever tab the presenter is looking at.
 * Pressing a letter key mid-step doesn't change it, so the count of arrow presses to the next stage is fixed.
 */
export function nextPosition(steps: readonly TalkStep[], position: TalkPosition): TalkPosition | null {
    return positionOf(steps, 1, position.stepId);
}

/** Where ← goes, or `null` at the start of the talk. */
export function previousPosition(steps: readonly TalkStep[], position: TalkPosition): TalkPosition | null {
    return positionOf(steps, -1, position.stepId);
}

/** The route for a position. */
export function positionPath(position: TalkPosition): string {
    return talkPath(position.stepId, position.tab ?? undefined);
}

/** Where *Start the talk* goes. */
export function talkStartPath(steps: readonly TalkStep[] = talkSteps): string {
    const first = steps[0];
    return first === undefined ? '/' : positionPath({ stepId: first.id, tab: stepTab(first) });
}

/**
 * The stage screen's starting state for a stage step: its stage, its preset options and, once the golden
 * queries have loaded, its golden query's query, device and filters.
 */
export function talkStepState(step: TalkStep, preset: GoldenQuery | null): SearchState {
    const stage =
        step.stage !== undefined && isPipelineStage(step.stage) ? step.stage : defaultSearchState.stage;
    const state: SearchState = {
        ...defaultSearchState,
        ...step.options,
        stage,
        tab: stepTab(step) ?? defaultSearchState.tab,
    };

    return preset === null ? state : applyGoldenQuery(state, preset);
}
