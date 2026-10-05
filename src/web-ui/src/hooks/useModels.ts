import { useContext, useEffect } from 'react';
import { ModelsContext, type ModelsContextValue } from '@/lib/models';

/** The model catalogue and the settings sheet's state. Needs a `ModelsProvider` above it. */
export function useModels(): ModelsContextValue {
    const context = useContext(ModelsContext);
    if (context === null) {
        throw new Error('useModels must be used inside a ModelsProvider.');
    }
    return context;
}

/**
 * Lists the models the first time a screen needs them (Stages 6–7), not on every page load: listing asks each
 * enabled provider, which can take a few seconds when a hosted one is slow.
 */
export function useModelCatalogue(isNeeded: boolean): ModelsContextValue {
    const models = useModels();
    const { catalogue, reloadCatalogue } = models;

    useEffect(() => {
        if (isNeeded && catalogue === null) {
            reloadCatalogue();
        }
    }, [isNeeded, catalogue, reloadCatalogue]);

    return models;
}
