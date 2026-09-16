import { useContext } from 'react';
import { PresentationContext, type PresentationContextValue } from '@/lib/presentation';

/** Whether presentation mode is on, and a setter. Needs a `PresentationProvider` above it. */
export function usePresentation(): PresentationContextValue {
    const context = useContext(PresentationContext);
    if (context === null) {
        throw new Error('usePresentation must be used inside a PresentationProvider.');
    }
    return context;
}
