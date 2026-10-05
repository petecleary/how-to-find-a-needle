import { useCallback, useEffect, useState } from 'react';
import {
    ApiError,
    getProviders,
    removeProviderKey,
    setProviderKey,
    testProvider,
    updateProvider,
    type ConnectionTest,
    type LlmProviderStatus,
} from '@/api/client';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Switch } from '@/components/ui/switch';
import { useModels } from '@/hooks/useModels';

// ModelSettingsSheet — bring your own model (ADR-0019): turn providers on, point them at a server, and paste a key
// for this session. The API keeps the key in memory and only ever reports where a key came from, so nothing here
// can read a key back. After every change the provider is tested, so the result is visible straight away.

export function ModelSettingsSheet() {
    const { isSettingsOpen, setSettingsOpen, reloadCatalogue } = useModels();
    const [providers, setProviders] = useState<LlmProviderStatus[] | null>(null);
    const [loadError, setLoadError] = useState<string | null>(null);

    const loadProviders = useCallback(async () => {
        try {
            setProviders(await getProviders());
            setLoadError(null);
        } catch (error) {
            setLoadError(error instanceof Error ? error.message : String(error));
        }
    }, []);

    // Read fresh each time the sheet opens: a key set with user-secrets or a restarted Ollama shows up.
    useEffect(() => {
        if (!isSettingsOpen) {
            return;
        }

        const controller = new AbortController();
        getProviders(controller.signal).then(
            (list) => {
                setProviders(list);
                setLoadError(null);
            },
            (error: unknown) => {
                if (!controller.signal.aborted) {
                    setLoadError(error instanceof Error ? error.message : String(error));
                }
            },
        );

        return () => controller.abort();
    }, [isSettingsOpen]);

    const handleChanged = useCallback(async () => {
        await loadProviders();
        reloadCatalogue();
    }, [loadProviders, reloadCatalogue]);

    return (
        <Sheet open={isSettingsOpen} onOpenChange={setSettingsOpen}>
            <SheetContent className="w-full overflow-y-auto sm:max-w-lg">
                <SheetHeader>
                    <SheetTitle>Models and API keys</SheetTitle>
                    <SheetDescription>
                        Choose which providers Stages 6 and 7 can use. Keys pasted here stay in the API&apos;s
                        memory until it stops and are never saved; to keep one, use{' '}
                        <code>dotnet user-secrets</code>. Other settings are saved in{' '}
                        <code>~/.needle/settings.json</code>.
                    </SheetDescription>
                </SheetHeader>
                <div className="flex flex-col gap-4 px-4 pb-6">
                    {loadError === null ? null : (
                        <p role="alert" className="text-destructive">
                            {loadError}
                        </p>
                    )}
                    {providers === null && loadError === null ? (
                        <p className="text-muted-foreground">Loading providers…</p>
                    ) : null}
                    {providers?.map((provider) => (
                        <ProviderCard key={provider.id} provider={provider} onChanged={handleChanged} />
                    ))}
                </div>
            </SheetContent>
        </Sheet>
    );
}

interface ProviderCardProps {
    provider: LlmProviderStatus;
    onChanged: () => Promise<void>;
}

function ProviderCard({ provider, onChanged }: ProviderCardProps) {
    const [baseUrl, setBaseUrl] = useState(provider.baseUrl ?? '');
    const [key, setKey] = useState('');
    const [extraModels, setExtraModels] = useState(provider.extraModels.join(', '));
    const [result, setResult] = useState<ConnectionTest | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [isBusy, setIsBusy] = useState(false);
    const takesExtraModels = provider.id === 'azure' || provider.id === 'compat';
    const takesKey = provider.keyVariable !== null;

    // Every change is followed by a test, so "did that work?" is answered on the spot.
    async function act(change: () => Promise<void>) {
        setIsBusy(true);
        setError(null);
        try {
            await change();
            setResult(await testProvider(provider.id));
            await onChanged();
        } catch (failure) {
            setError(describeFailure(failure));
        } finally {
            setIsBusy(false);
        }
    }

    return (
        <section
            aria-labelledby={`provider-${provider.id}`}
            className="flex flex-col gap-3 rounded-[18px] border-2 bg-card p-4"
        >
            <div className="flex items-start justify-between gap-3">
                <div>
                    <h3 id={`provider-${provider.id}`} className="text-lg font-bold">
                        {provider.name}
                    </h3>
                    <p className="text-sm text-muted-foreground">{provider.detail}</p>
                    <p className="text-sm">{describeKey(provider)}</p>
                </div>
                <label className="flex items-center gap-2 text-sm font-bold whitespace-nowrap">
                    <Switch
                        checked={provider.enabled}
                        disabled={isBusy}
                        onCheckedChange={(checked) =>
                            void act(() => updateProvider(provider.id, { enabled: checked }))
                        }
                    />
                    In picker
                </label>
            </div>

            <form
                className="flex items-end gap-2"
                onSubmit={(event) => {
                    event.preventDefault();
                    void act(() => updateProvider(provider.id, { baseUrl: baseUrl.trim() }));
                }}
            >
                <label className="flex flex-1 flex-col gap-1 text-sm">
                    Base URL
                    <Input
                        value={baseUrl}
                        onChange={(event) => setBaseUrl(event.target.value)}
                        placeholder={
                            provider.defaultBaseUrl ?? 'https://<resource>.openai.azure.com/openai/v1'
                        }
                        className="font-mono"
                    />
                </label>
                <Button type="submit" variant="outline" disabled={isBusy} className="rounded-full border-2">
                    Save
                </Button>
            </form>

            {takesKey ? (
                <form
                    className="flex items-end gap-2"
                    onSubmit={(event) => {
                        event.preventDefault();
                        const pasted = key;
                        // Cleared straight away: the key shouldn't linger in the page's state once the API has it.
                        setKey('');
                        void act(() => setProviderKey(provider.id, pasted));
                    }}
                >
                    <label className="flex flex-1 flex-col gap-1 text-sm">
                        API key (this session only)
                        <Input
                            type="password"
                            autoComplete="off"
                            value={key}
                            onChange={(event) => setKey(event.target.value)}
                            placeholder={`or set ${provider.keyVariable}`}
                        />
                    </label>
                    <Button
                        type="submit"
                        variant="outline"
                        disabled={isBusy || key.trim() === ''}
                        className="rounded-full border-2"
                    >
                        Use key
                    </Button>
                </form>
            ) : null}

            {takesExtraModels ? (
                <form
                    className="flex items-end gap-2"
                    onSubmit={(event) => {
                        event.preventDefault();
                        void act(() =>
                            updateProvider(provider.id, {
                                extraModels: extraModels.split(',').map((name) => name.trim()),
                            }),
                        );
                    }}
                >
                    <label className="flex flex-1 flex-col gap-1 text-sm">
                        {provider.id === 'azure' ? 'Deployment names (comma-separated)' : 'Extra model names'}
                        <Input
                            value={extraModels}
                            onChange={(event) => setExtraModels(event.target.value)}
                            className="font-mono"
                        />
                    </label>
                    <Button
                        type="submit"
                        variant="outline"
                        disabled={isBusy}
                        className="rounded-full border-2"
                    >
                        Save
                    </Button>
                </form>
            ) : null}

            <div className="flex flex-wrap items-center gap-2">
                <Button
                    type="button"
                    variant="outline"
                    disabled={isBusy}
                    onClick={() => void act(async () => {})}
                    className="rounded-full border-2"
                >
                    Test connection
                </Button>
                {provider.keySource === 'Session' ? (
                    <Button
                        type="button"
                        variant="ghost"
                        disabled={isBusy}
                        onClick={() => void act(() => removeProviderKey(provider.id))}
                        className="rounded-full"
                    >
                        Forget session key
                    </Button>
                ) : null}
                <span role="status" className="text-sm">
                    {isBusy ? 'Checking…' : (error ?? result?.message ?? '')}
                </span>
            </div>
        </section>
    );
}

// A 400 names each broken rule (e.g. "Base URL must be an absolute http or https URL"); show those, not the title.
function describeFailure(failure: unknown): string {
    if (failure instanceof ApiError && failure.validationErrors.length > 0) {
        return failure.validationErrors.map((error) => error.reason).join(' ');
    }

    return failure instanceof Error ? failure.message : String(failure);
}

function describeKey(provider: LlmProviderStatus): string {
    if (provider.keyVariable === null) {
        return 'No key needed.';
    }

    switch (provider.keySource) {
        case 'Session':
            return 'Key: pasted for this session.';
        case 'Configuration':
            return `Key: from ${provider.keyVariable}.`;
        default:
            return provider.needsKey ? 'No key yet.' : 'No key (optional).';
    }
}
