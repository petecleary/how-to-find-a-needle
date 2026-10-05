import { describe, expect, it } from 'vitest';
import type { ModelCatalogue } from '@/api/client';
import { modelGroups, toModelOption } from './models';

const catalogue: ModelCatalogue = {
    default: 'ollama/qwen3.6:35b',
    providers: [
        {
            id: 'ollama',
            name: 'Ollama',
            isLocal: true,
            models: [
                { ref: 'ollama/gemma4:31b', model: 'gemma4:31b', isDefault: false },
                { ref: 'ollama/qwen3.6:35b', model: 'qwen3.6:35b', isDefault: true },
            ],
            problem: null,
        },
        {
            id: 'anthropic',
            name: 'Anthropic',
            isLocal: false,
            models: [],
            problem:
                'No API key. Add one in Models and API keys, or set ANTHROPIC_API_KEY with dotnet user-secrets.',
        },
    ],
};

describe('modelGroups', () => {
    it('keeps a provider that could not be listed, with its reason', () => {
        const groups = modelGroups(catalogue, null);

        expect(groups.map((group) => group.providerId)).toEqual(['ollama', 'anthropic']);
        expect(groups[1]?.problem).toContain('ANTHROPIC_API_KEY');
    });

    it('still offers a model from the URL that is not listed, under its provider', () => {
        const groups = modelGroups(catalogue, 'ollama/llama3.3:70b');

        expect(groups[0]?.models[0]).toEqual({
            ref: 'ollama/llama3.3:70b',
            model: 'llama3.3:70b',
            isDefault: false,
        });
    });

    it('splits at the first slash only, as the API does', () => {
        const groups = modelGroups(catalogue, 'compat/meta-llama/llama-3.1-8b');
        const compat = groups.find((group) => group.providerId === 'compat');

        expect(compat?.models[0]?.model).toBe('meta-llama/llama-3.1-8b');
    });
});

describe('toModelOption', () => {
    it('sends null for the default, so an unchanged URL stays clean', () => {
        expect(toModelOption('ollama/qwen3.6:35b', catalogue)).toBeNull();
        expect(toModelOption('ollama/gemma4:31b', catalogue)).toBe('ollama/gemma4:31b');
    });
});
