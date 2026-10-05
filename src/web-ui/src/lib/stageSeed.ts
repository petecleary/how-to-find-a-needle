import type { GoldenQuery } from '@/api/client';
import {
    applyGoldenQuery,
    defaultSearchState,
    stageTabs,
    type Audience,
    type SearchState,
    type StageTab,
} from './searchState';
import { isPipelineStage } from './stageGroup';
import { isTabAvailable } from './stageTabs';

// A stage seed is what a slide names for the demo window (lib/slides.ts): a stage, a golden query, preset options
// and the tab to land on. Turning a seed into a full SearchState here means the demo, when it follows the deck,
// starts each stage screen exactly as an ordinary /demo URL would.

/** The options a seed may preset; anything left out keeps the API's default. */
export interface StageSeedOptions {
    expandSynonyms?: boolean;
    applyConstraints?: boolean;
    audience?: Audience;
    applyPedagogy?: boolean;
}

/** What seeds a stage screen. Every field is optional: content is JSON, so values are checked when used. */
export interface StageSeed {
    stage?: string;
    goldenQuery?: string;
    options?: StageSeedOptions;
    /** The one tab to land on. Defaults to How it works. */
    tab?: string;
}

export function isStageTab(value: string): value is StageTab {
    return stageTabs.some((tab) => tab === value);
}

/**
 * The tab a seed lands on: its own `tab` when that tab exists and the stage has it, or How it works — the
 * technique is explained before its results are argued about.
 */
export function seedTab(seed: StageSeed): StageTab {
    const stage = seed.stage !== undefined && isPipelineStage(seed.stage) ? seed.stage : null;

    if (seed.tab !== undefined && isStageTab(seed.tab)) {
        if (stage === null || isTabAvailable(seed.tab, stage)) {
            return seed.tab;
        }
    }

    return 'how-it-works';
}

/**
 * The stage screen's starting state for a seed: its stage, its preset options, its tab and, when the golden
 * query is given, that query's inputs. Everything else is the default.
 */
export function seededState(seed: StageSeed, preset: GoldenQuery | null): SearchState {
    const stage =
        seed.stage !== undefined && isPipelineStage(seed.stage) ? seed.stage : defaultSearchState.stage;
    const state: SearchState = {
        ...defaultSearchState,
        ...seed.options,
        stage,
        tab: seedTab(seed),
    };

    return preset === null ? state : applyGoldenQuery(state, preset);
}
