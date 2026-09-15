import { stageTabs, type StageTab } from './searchState';

// Talk position lives in the route, /talk/:step/:tab? (ADR-0014), e.g. /talk/stage-hybrid/results.
// A bookmark reopens the same step and tab, and the browser's back button walks back through the talk.
// The steps themselves come from content/talk.json (Phase 3 step 10).

/** The route for a talk step, and optionally one of its tabs. */
export function talkPath(stepId: string, tab?: StageTab): string {
    const stepPath = `/talk/${encodeURIComponent(stepId)}`;
    return tab === undefined ? stepPath : `${stepPath}/${tab}`;
}

/** The tab named in the route, or `null` when there is none or it isn't a known tab. */
export function parseTalkTab(value: string | undefined): StageTab | null {
    return stageTabs.find((tab) => tab === value) ?? null;
}
