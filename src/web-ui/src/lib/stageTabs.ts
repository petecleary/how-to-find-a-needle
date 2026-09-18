import { hasGoingFurther } from './content';
import type { StageTab } from './searchState';
import { stageGroup, type PipelineStage } from './stageGroup';

// Each stage splits into five tabs, so not everything is on screen at once (ADR-0014 § Stage screen).

export interface StageTabDefinition {
    tab: StageTab;
    label: string;
    /** The letter that jumps to this tab. */
    shortcut: string;
    /**
     * Shown in place of the count when the tab isn't available for a stage. An unavailable tab stays visible
     * and disabled rather than disappearing: a tab that comes and goes would move every other tab under the
     * presenter's hand as the stage changes.
     */
    unavailableHint?: string;
}

/** The tabs in the order ADR-0014 lists them. */
export const stageTabDefinitions: readonly StageTabDefinition[] = [
    { tab: 'how-it-works', label: 'How it works', shortcut: 'h' },
    { tab: 'results', label: 'Results', shortcut: 'r' },
    { tab: 'answer', label: 'Answer', shortcut: 'a', unavailableHint: 'Stages 6–7' },
    { tab: 'under-the-hood', label: 'Under the hood', shortcut: 'u' },
    { tab: 'going-further', label: 'Going further', shortcut: 'g', unavailableHint: 'Stages 2–7' },
];

/**
 * Two tabs are conditional. The Answer tab belongs to Stages 6–7 only: the earlier stages return results, not
 * generated text. Going further belongs to any stage with a `content/going-further/{stage}.md` file, which
 * Stage 1 doesn't have — nothing beyond SQL earns a panel (ADR-0018).
 */
export function isTabAvailable(tab: StageTab, stage: PipelineStage): boolean {
    if (tab === 'answer') {
        return stageGroup(stage) === 'pedagogy';
    }
    if (tab === 'going-further') {
        return hasGoingFurther(stage);
    }
    return true;
}

/** The tab a key jumps to (H, R, A, U or G, either case), or `null`. */
export function tabForShortcut(key: string): StageTab | null {
    const lowerKey = key.toLowerCase();
    return stageTabDefinitions.find((definition) => definition.shortcut === lowerKey)?.tab ?? null;
}
