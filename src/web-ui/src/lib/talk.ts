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
import { isPipelineStage, stageGroup } from './stageGroup';
import { talkPath } from './talkRoute';

// Talk mode replaces slides (ADR-0014 § Pages). content/talk.json lists the steps in order; each has a
// markdown file, and a stage step also names its stage, a golden query and any preset options. → walks through
// a stage step's tabs, then on to the next step. The position lives in the route, /talk/:step/:tab?.

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
    /** The tabs → walks through on a stage step, in order. */
    tabs?: string[];
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
 * The tabs → walks through on a step. A stage step uses its own `tabs`, or How it works → Results → Under the
 * hood; Stages 6–7 add Answer after Results. Intro and summary steps have none.
 */
export function stepTabs(step: TalkStep): StageTab[] {
    if (step.kind !== 'stage') {
        return [];
    }

    if (step.tabs !== undefined) {
        return step.tabs.filter(isStageTab);
    }

    const hasAnswer =
        step.stage !== undefined && isPipelineStage(step.stage) && stageGroup(step.stage) === 'pedagogy';
    return hasAnswer
        ? ['how-it-works', 'results', 'answer', 'under-the-hood']
        : ['how-it-works', 'results', 'under-the-hood'];
}

/** Where → goes from a position, or `null` at the end of the talk. */
export function nextPosition(steps: readonly TalkStep[], position: TalkPosition): TalkPosition | null {
    const index = steps.findIndex((step) => step.id === position.stepId);
    const step = steps[index];
    if (step === undefined) {
        return null;
    }

    const tabs = stepTabs(step);
    const tabIndex = position.tab === null ? -1 : tabs.indexOf(position.tab);
    const nextTab = tabIndex === -1 ? undefined : tabs[tabIndex + 1];

    if (nextTab !== undefined) {
        return { stepId: step.id, tab: nextTab };
    }

    // The last tab, or a tab the step doesn't list (the presenter jumped to it with a letter key): next step.
    const nextStep = steps[index + 1];
    return nextStep === undefined ? null : { stepId: nextStep.id, tab: stepTabs(nextStep)[0] ?? null };
}

/** Where ← goes from a position, or `null` at the start of the talk. */
export function previousPosition(steps: readonly TalkStep[], position: TalkPosition): TalkPosition | null {
    const index = steps.findIndex((step) => step.id === position.stepId);
    const step = steps[index];
    if (step === undefined) {
        return null;
    }

    const tabs = stepTabs(step);
    const tabIndex = position.tab === null ? -1 : tabs.indexOf(position.tab);

    if (tabIndex > 0) {
        return { stepId: step.id, tab: tabs[tabIndex - 1] ?? null };
    }

    // From a tab the step doesn't list, ← goes back to the step's first tab rather than leaving the step.
    if (tabIndex === -1 && position.tab !== null && tabs[0] !== undefined) {
        return { stepId: step.id, tab: tabs[0] };
    }

    const previousStep = steps[index - 1];
    return previousStep === undefined
        ? null
        : { stepId: previousStep.id, tab: stepTabs(previousStep).at(-1) ?? null };
}

/** The route for a position. */
export function positionPath(position: TalkPosition): string {
    return talkPath(position.stepId, position.tab ?? undefined);
}

/** Where *Start the talk* goes. */
export function talkStartPath(steps: readonly TalkStep[] = talkSteps): string {
    const first = steps[0];
    return first === undefined ? '/' : positionPath({ stepId: first.id, tab: stepTabs(first)[0] ?? null });
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
        tab: stepTabs(step)[0] ?? defaultSearchState.tab,
    };

    return preset === null ? state : applyGoldenQuery(state, preset);
}
