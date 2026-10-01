import { useEffect } from 'react';
import { openPresenterChannel, postSlide } from '@/lib/presenterSync';

/**
 * Tells any demo window that is following the deck which slide is showing, each time the slide changes
 * (ADR-0014 § Slides mode). The channel is opened for the slide and closed when the next one replaces it.
 */
export function useBroadcastSlide(slideId: string | null): void {
    useEffect(() => {
        if (slideId === null) {
            return;
        }

        const channel = openPresenterChannel();
        if (channel === null) {
            return;
        }

        postSlide(channel, slideId);
        return () => channel.close();
    }, [slideId]);
}
