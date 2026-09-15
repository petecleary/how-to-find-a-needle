import { describe, expect, it } from 'vitest';
import { isTabAvailable, stageTabDefinitions, tabForShortcut } from './stageTabs';

describe('stageTabDefinitions', () => {
    it('lists the four tabs with the H, R, A and U shortcuts', () => {
        expect(stageTabDefinitions.map(({ tab, shortcut }) => `${shortcut}:${tab}`)).toEqual([
            'h:how-it-works',
            'r:results',
            'a:answer',
            'u:under-the-hood',
        ]);
    });
});

describe('isTabAvailable', () => {
    it('offers the Answer tab only on Stages 6–7', () => {
        expect(isTabAvailable('answer', 'ontology')).toBe(false);
        expect(isTabAvailable('answer', 'rag')).toBe(true);
        expect(isTabAvailable('answer', 'pedagogy')).toBe(true);
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
        expect(tabForShortcut('x')).toBeNull();
    });
});
