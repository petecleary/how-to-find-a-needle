import { useCallback, useLayoutEffect, useMemo, useState, type ReactNode } from 'react';
import { useLocation } from 'react-router';
import {
    isPresentationOn,
    PresentationContext,
    readStoredPresentation,
    storePresentation,
} from '@/lib/presentation';

export interface PresentationProviderProps {
    children: ReactNode;
}

/**
 * Applies presentation mode by toggling `.presentation` on `<html>`, which index.css enlarges and the
 * `presentation:` variant uses to hide non-essential controls. Read it with `usePresentation()`.
 */
export function PresentationProvider({ children }: PresentationProviderProps) {
    const { pathname } = useLocation();
    const [choice, setChoice] = useState(readStoredPresentation);
    const isPresentationMode = isPresentationOn(choice, pathname);

    // Before the browser paints, so moving into talk mode never flashes at the smaller size.
    useLayoutEffect(() => {
        document.documentElement.classList.toggle('presentation', isPresentationMode);
    }, [isPresentationMode]);

    const setPresentationMode = useCallback((isOn: boolean) => {
        const next = isOn ? 'on' : 'off';
        setChoice(next);
        storePresentation(next);
    }, []);

    const value = useMemo(
        () => ({ isPresentationMode, setPresentationMode }),
        [isPresentationMode, setPresentationMode],
    );

    return <PresentationContext value={value}>{children}</PresentationContext>;
}
