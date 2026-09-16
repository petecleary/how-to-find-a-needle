import { describe, expect, it } from 'vitest';
import { parseTalkTab, talkPath } from './talkRoute';

describe('talkPath', () => {
    it('builds the route for a step, with or without a tab', () => {
        expect(talkPath('stage-hybrid')).toBe('/talk/stage-hybrid');
        expect(talkPath('stage-hybrid', 'under-the-hood')).toBe('/talk/stage-hybrid/under-the-hood');
    });
});

describe('parseTalkTab', () => {
    it('reads a known tab and rejects anything else', () => {
        expect(parseTalkTab('results')).toBe('results');
        expect(parseTalkTab('slides')).toBeNull();
        expect(parseTalkTab(undefined)).toBeNull();
    });

    it('round-trips with talkPath', () => {
        const tab = talkPath('stage-ontology', 'answer').split('/').at(-1);

        expect(parseTalkTab(tab)).toBe('answer');
    });
});
