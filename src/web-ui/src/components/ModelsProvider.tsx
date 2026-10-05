import { useCallback, useMemo, useRef, useState, type ReactNode } from 'react';
import { getModels, type ModelCatalogue } from '@/api/client';
import { ModelSettingsSheet } from '@/components/ModelSettingsSheet';
import type { ApiData } from '@/hooks/useApiData';
import { ModelsContext } from '@/lib/models';

export interface ModelsProviderProps {
    children: ReactNode;
}

/**
 * Shares the model catalogue between the Stage 6–7 picker and the Models and API keys sheet, so a key added in the
 * sheet shows up in the picker at once (ADR-0019). Read it with `useModels()`. Plain context: no global state library.
 */
export function ModelsProvider({ children }: ModelsProviderProps) {
    const [catalogue, setCatalogue] = useState<ApiData<ModelCatalogue> | null>(null);
    const [isSettingsOpen, setSettingsOpen] = useState(false);
    const current = useRef<AbortController | null>(null);

    const reloadCatalogue = useCallback(() => {
        // A newer list replaces an older one still loading, so a slow provider can't overwrite a fresher answer.
        current.current?.abort();
        const controller = new AbortController();
        current.current = controller;

        setCatalogue((previous) => ({ status: 'loading', data: previous?.data ?? null, error: null }));
        getModels(controller.signal).then(
            (data) => {
                if (!controller.signal.aborted) {
                    setCatalogue({ status: 'success', data, error: null });
                }
            },
            (error: unknown) => {
                if (!controller.signal.aborted) {
                    setCatalogue({
                        status: 'error',
                        data: null,
                        error: error instanceof Error ? error : new Error(String(error)),
                    });
                }
            },
        );
    }, []);

    const value = useMemo(
        () => ({ catalogue, reloadCatalogue, isSettingsOpen, setSettingsOpen }),
        [catalogue, reloadCatalogue, isSettingsOpen],
    );

    return (
        <ModelsContext value={value}>
            {children}
            <ModelSettingsSheet />
        </ModelsContext>
    );
}
