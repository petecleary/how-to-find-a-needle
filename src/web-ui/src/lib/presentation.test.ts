import { describe, expect, it } from 'vitest';
import { isPresentationOn } from './presentation';

describe('isPresentationOn', () => {
    it('is off away from the slides until the viewer chooses', () => {
        expect(isPresentationOn(null, '/demo')).toBe(false);
        expect(isPresentationOn(null, '/')).toBe(false);
    });

    it('is on for the slide deck', () => {
        expect(isPresentationOn(null, '/slides/hybrid-rrf')).toBe(true);
        expect(isPresentationOn(null, '/slides')).toBe(true);
        expect(isPresentationOn(null, '/slideshow')).toBe(false);
    });

    it("follows the viewer's choice on every page once they make one", () => {
        expect(isPresentationOn('off', '/slides/intro')).toBe(false);
        expect(isPresentationOn('on', '/demo')).toBe(true);
    });
});
