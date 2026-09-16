import type { StageTab } from './searchState';
import { stageGroup, type PipelineStage } from './stageGroup';

// Each stage splits into four tabs, so not everything is on screen at once (ADR-0014 § Stage screen).

export interface StageTabDefinition {
    tab: StageTab;
    label: string;
    /** The letter that jumps to this tab. */
    shortcut: string;
}

/** The tabs in the order ADR-0014 lists them. */
export const stageTabDefinitions: readonly StageTabDefinition[] = [
    { tab: 'how-it-works', label: 'How it works', shortcut: 'h' },
    { tab: 'results', label: 'Results', shortcut: 'r' },
    { tab: 'answer', label: 'Answer', shortcut: 'a' },
    { tab: 'under-the-hood', label: 'Under the hood', shortcut: 'u' },
];

/** The Answer tab belongs to Stages 6–7 only: the earlier stages return results, not generated text. */
export function isTabAvailable(tab: StageTab, stage: PipelineStage): boolean {
    return tab !== 'answer' || stageGroup(stage) === 'pedagogy';
}

/** The tab a key jumps to (H, R, A or U, either case), or `null`. */
export function tabForShortcut(key: string): StageTab | null {
    const lowerKey = key.toLowerCase();
    return stageTabDefinitions.find((definition) => definition.shortcut === lowerKey)?.tab ?? null;
}
