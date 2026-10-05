// presenterSync — how the slide deck tells the demo window which slide is showing (ADR-0014 § Slides mode).
//
// The deck and the demo are two windows of the same browser, on the same origin. BroadcastChannel delivers a
// message from one to every other window on that origin that has opened a channel with the same name. That's
// all this needs: no server round trip, no shared state left behind in storage, and nothing to clean up after
// the talk. The message is only the slide's id; the demo looks the slide up in the same content/slides.json.

export const presenterChannelName = 'needle-presenter';

export interface SlideMessage {
    type: 'slide';
    slideId: string;
}

/**
 * A channel to the other presenter windows, or `null` when the browser has no BroadcastChannel. Then the deck
 * and the demo still work; the demo just doesn't follow.
 */
export function openPresenterChannel(): BroadcastChannel | null {
    try {
        return typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel(presenterChannelName);
    } catch {
        return null;
    }
}

/** A message from another window is untrusted input: check its shape before acting on it. */
export function isSlideMessage(data: unknown): data is SlideMessage {
    if (typeof data !== 'object' || data === null) {
        return false;
    }

    const message = data as Record<string, unknown>;
    return message.type === 'slide' && typeof message.slideId === 'string';
}

/** Tells any following demo window that this slide is now showing. */
export function postSlide(channel: BroadcastChannel, slideId: string): void {
    const message: SlideMessage = { type: 'slide', slideId };
    channel.postMessage(message);
}
