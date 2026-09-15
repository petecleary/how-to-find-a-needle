import { useState } from 'react';

export interface ShowMore<T> {
    visible: readonly T[];
    hiddenCount: number;
    isExpanded: boolean;
    canExpand: boolean;
    toggle: () => void;
}

/**
 * The first few items of a long list, with the rest one click away. Trace tables list every candidate (50 on
 * Stage 5); showing them all at once pushes the interesting rows off a 720px projector. Nothing is dropped.
 */
export function useShowMore<T>(items: readonly T[], previewSize: number): ShowMore<T> {
    const [isExpanded, setIsExpanded] = useState(false);
    const visible = isExpanded ? items : items.slice(0, previewSize);

    return {
        visible,
        hiddenCount: items.length - visible.length,
        isExpanded,
        canExpand: items.length > previewSize,
        toggle: () => setIsExpanded(!isExpanded),
    };
}
