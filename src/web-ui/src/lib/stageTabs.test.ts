import { describe, expect, it } from 'vitest';
import { pipelineStages } from './stageGroup';
import { isTabAvailable, stageTabDefinitions, tabForShortcut } from './stageTabs';

describe('stageTabDefinitions', () => {
    it('lists the five tabs with the H, R, A, U and G shortcuts', () => {
        expect(stageTabDefinitions.map(({ tab, shortcut }) => `${shortcut}:${tab}`)).toEqual([
            'h:how-it-works',
            'r:results',
            'a:answer',
            'u:under-the-hood',
            'g:going-further',
        ]);
    });

    it('gives every conditional tab a hint to show while it is disabled', () => {
        const conditional = stageTabDefinitions.filter(({ tab }) =>
            pipelineStages.some((stage) => !isTabAvailable(tab, stage)),
        );

        expect(conditional.map(({ tab }) => tab)).toEqual(['answer', 'going-further']);
        expect(conditional.filter(({ unavailableHint }) => unavailableHint === undefined)).toEqual([]);
    });
});

describe('isTabAvailable', () => {
    it('offers the Answer tab only on Stages 6–7', () => {
        expect(isTabAvailable('answer', 'ontology')).toBe(false);
        expect(isTabAvailable('answer', 'rag')).toBe(true);
        expect(isTabAvailable('answer', 'pedagogy')).toBe(true);
    });

    // Stage 1 has no content/going-further/structured.md: nothing beyond SQL earns a panel (ADR-0018).
    it('offers the Going further tab on every stage but the first', () => {
        expect(isTabAvailable('going-further', 'structured')).toBe(false);
        expect(
            pipelineStages.filter(
                (stage) => stage !== 'structured' && !isTabAvailable('going-further', stage),
            ),
        ).toEqual([]);
    });

    it('offers every other tab on every stage', () => {
        expect(isTabAvailable('results', 'structured')).toBe(true);
        expect(isTabAvailable('under-the-hood', 'hybrid')).toBe(true);
    });
});

describe('tabForShortcut', () => {
    it('finds the tab for a letter, in either case', () => {
        expect(tabForShortcut('u')).toBe('under-the-hood');
        expect(tabForShortcut('R')).toBe('results');
        expect(tabForShortcut('g')).toBe('going-further');
        expect(tabForShortcut('x')).toBeNull();
    });
});
