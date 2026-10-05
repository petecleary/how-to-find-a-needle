import { afterEach, describe, expect, it, vi } from 'vitest';
import { isSlideMessage, openPresenterChannel, postSlide } from './presenterSync';

afterEach(() => {
    vi.unstubAllGlobals();
});

describe('presenterSync', () => {
    it('delivers the slide id from the deck to a following window', async () => {
        const deck = openPresenterChannel();
        const demo = openPresenterChannel();
        if (deck === null || demo === null) throw new Error('BroadcastChannel is missing from this runtime');

        const received = new Promise<unknown>((resolve) => {
            demo.onmessage = (event: MessageEvent<unknown>) => resolve(event.data);
        });
        postSlide(deck, 'hybrid-rrf');

        await expect(received).resolves.toEqual({ type: 'slide', slideId: 'hybrid-rrf' });
        deck.close();
        demo.close();
    });

    it('returns null, rather than throwing, when the browser has no BroadcastChannel', () => {
        vi.stubGlobal('BroadcastChannel', undefined);

        expect(openPresenterChannel()).toBeNull();
    });

    it('accepts only well-formed slide messages', () => {
        expect(isSlideMessage({ type: 'slide', slideId: 'title' })).toBe(true);
        expect(isSlideMessage({ type: 'slide' })).toBe(false);
        expect(isSlideMessage({ type: 'other', slideId: 'title' })).toBe(false);
        expect(isSlideMessage('slide')).toBe(false);
        expect(isSlideMessage(null)).toBe(false);
    });
});
