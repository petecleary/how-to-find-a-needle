import { createContext } from 'react';

// Presentation mode is for a projector (ADR-0014): larger type and fewer controls. It is on by default in talk
// mode and off elsewhere. The header toggle overrides that everywhere, and the choice is remembered on this
// device, like the theme.

export type PresentationChoice = 'on' | 'off';

export const presentationStorageKey = 'needle-presentation';

/** Whether presentation mode applies: the viewer's choice if they made one, otherwise on in talk mode only. */
export function isPresentationOn(choice: PresentationChoice | null, pathname: string): boolean {
    if (choice === null) {
        return pathname === '/talk' || pathname.startsWith('/talk/');
    }

    return choice === 'on';
}

// Storage can throw (blocked site data, some private windows), so a failure means "use the default".
export function readStoredPresentation(): PresentationChoice | null {
    try {
        const stored = localStorage.getItem(presentationStorageKey);
        return stored === 'on' || stored === 'off' ? stored : null;
    } catch {
        return null;
    }
}

export function storePresentation(choice: PresentationChoice): void {
    try {
        localStorage.setItem(presentationStorageKey, choice);
    } catch {
        // Not remembered this time; the choice still applies for this visit.
    }
}

export interface PresentationContextValue {
    isPresentationMode: boolean;
    setPresentationMode: (isOn: boolean) => void;
}

export const PresentationContext = createContext<PresentationContextValue | null>(null);
