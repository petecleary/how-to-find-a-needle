import { describe, expect, it } from 'vitest';
import { isPresentationOn } from './presentation';

describe('isPresentationOn', () => {
    it('is on in talk mode and off elsewhere until the viewer chooses', () => {
        expect(isPresentationOn(null, '/talk/stage-ontology/results')).toBe(true);
        expect(isPresentationOn(null, '/talk')).toBe(true);
        expect(isPresentationOn(null, '/demo')).toBe(false);
        expect(isPresentationOn(null, '/talking-points')).toBe(false);
    });

    it("follows the viewer's choice on every page once they make one", () => {
        expect(isPresentationOn('off', '/talk/intro')).toBe(false);
        expect(isPresentationOn('on', '/demo')).toBe(true);
    });
});
