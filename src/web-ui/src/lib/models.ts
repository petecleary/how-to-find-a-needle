import { createContext } from 'react';
import type { ModelCatalogue, ModelInfo } from '@/api/client';
import type { ApiData } from '@/hooks/useApiData';

// Bring your own model (ADR-0019). The model is a request option, like the audience: it lives in the URL as
// `provider/model` and is sent as `options.model`. The browser never holds a key; the API does.

export interface ModelsContextValue {
    /** The models each enabled provider offers, listed live by the API. Null until something asks for them. */
    catalogue: ApiData<ModelCatalogue> | null;
    /** Lists the models again, e.g. after a key was added or Ollama was started. */
    reloadCatalogue: () => void;
    isSettingsOpen: boolean;
    setSettingsOpen: (isOpen: boolean) => void;
}

export const ModelsContext = createContext<ModelsContextValue | null>(null);

/** One picker option: a model, or a provider whose models couldn't be listed (shown with the reason, never hidden). */
export interface ModelGroup {
    providerId: string;
    providerName: string;
    isLocal: boolean;
    models: ModelInfo[];
    problem: string | null;
}

/**
 * The picker's groups. A model named in the URL but not listed (a model you haven't pulled, a provider now off) is
 * still offered under its provider, so the picker shows what the request will actually send.
 */
export function modelGroups(catalogue: ModelCatalogue, selected: string | null): ModelGroup[] {
    const groups: ModelGroup[] = catalogue.providers.map((provider) => ({
        providerId: provider.id,
        providerName: provider.name,
        isLocal: provider.isLocal,
        models: [...provider.models],
        problem: provider.problem ?? null,
    }));

    const isListed = groups.some((group) => group.models.some((model) => model.ref === selected));
    if (selected !== null && !isListed) {
        // Split at the first slash only, as the API does: model names may contain slashes.
        const slash = selected.indexOf('/');
        const providerId = selected.slice(0, slash);
        const model: ModelInfo = { ref: selected, model: selected.slice(slash + 1), isDefault: false };
        const group = groups.find((candidate) => candidate.providerId === providerId);

        if (group === undefined) {
            groups.push({
                providerId,
                providerName: providerId,
                isLocal: false,
                models: [model],
                problem: null,
            });
        } else {
            group.models.unshift(model);
        }
    }

    return groups;
}

/**
 * The value to send: null when the choice is the API's default, so an unchanged URL stays clean and a request
 * without a model behaves exactly as before (ADR-0019).
 */
export function toModelOption(ref: string, catalogue: ModelCatalogue | null): string | null {
    return catalogue !== null && ref === catalogue.default ? null : ref;
}
