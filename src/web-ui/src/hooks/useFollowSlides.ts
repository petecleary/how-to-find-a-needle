import { useEffect } from 'react';
import { isSlideMessage, openPresenterChannel } from '@/lib/presenterSync';

/**
 * While `isFollowing`, calls `onSlide` with each slide id the deck announces (ADR-0014 § Slides mode).
 * Pass a stable `onSlide` (wrapped in `useCallback`), or the channel is reopened on every render.
 */
export function useFollowSlides(isFollowing: boolean, onSlide: (slideId: string) => void): void {
    useEffect(() => {
        if (!isFollowing) {
            return;
        }

        const channel = openPresenterChannel();
        if (channel === null) {
            return;
        }

        channel.onmessage = (event: MessageEvent<unknown>) => {
            if (isSlideMessage(event.data)) {
                onSlide(event.data.slideId);
            }
        };
        return () => channel.close();
    }, [isFollowing, onSlide]);
}
