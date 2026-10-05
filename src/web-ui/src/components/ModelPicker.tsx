import { KeyRound } from 'lucide-react';
import type { ModelCatalogue } from '@/api/client';
import { Button } from '@/components/ui/button';
import {
    Select,
    SelectContent,
    SelectGroup,
    SelectItem,
    SelectLabel,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import type { ApiData } from '@/hooks/useApiData';
import { modelGroups, toModelOption } from '@/lib/models';

// ModelPicker — chooses the LLM for Stages 6–7 (ADR-0019). Changing it re-runs the answer on the same evidence, so the
// audience can see what the model contributes and what the pipeline does. Providers that couldn't be listed stay in
// the list with the reason, rather than disappearing.

export interface ModelPickerProps {
    /** Null until the models are requested. */
    catalogue: ApiData<ModelCatalogue> | null;
    /** The request's `options.model`; null means the API's default. */
    value: string | null;
    onChange: (model: string | null) => void;
    /** Opens the Models and API keys sheet. */
    onManage: () => void;
}

export function ModelPicker({ catalogue, value, onChange, onManage }: ModelPickerProps) {
    const data = catalogue?.data ?? null;
    const selected = value ?? data?.default ?? null;
    const groups = data === null ? [] : modelGroups(data, value);

    return (
        <div className="flex items-center gap-2">
            <span id="model-label" className="text-[15px] text-muted-foreground">
                Model
            </span>
            {data === null || selected === null ? (
                <span className="text-[15px] text-muted-foreground" role="status">
                    {catalogue?.status === 'error'
                        ? `Models unavailable: ${catalogue.error?.message ?? 'unknown error'}`
                        : 'Listing models…'}
                </span>
            ) : (
                <Select value={selected} onValueChange={(next) => onChange(toModelOption(next, data))}>
                    <SelectTrigger
                        aria-labelledby="model-label"
                        className="h-8 max-w-[18rem] rounded-full border-2 bg-card font-mono text-[14px] shadow-none dark:bg-card"
                    >
                        <SelectValue />
                    </SelectTrigger>
                    <SelectContent position="popper" align="start">
                        {groups.map((group) => (
                            <SelectGroup key={group.providerId}>
                                <SelectLabel>
                                    {group.providerName}
                                    {group.isLocal ? ' · local' : ''}
                                </SelectLabel>
                                {group.models.map((model) => (
                                    <SelectItem key={model.ref} value={model.ref} className="font-mono">
                                        {model.model}
                                        {model.ref === data.default ? ' (default)' : ''}
                                    </SelectItem>
                                ))}
                                {group.problem === null ? null : (
                                    // Disabled, but visible: "no key" and "not running" are things to fix, not to hide.
                                    <SelectItem value={`problem:${group.providerId}`} disabled>
                                        {group.problem}
                                    </SelectItem>
                                )}
                            </SelectGroup>
                        ))}
                    </SelectContent>
                </Select>
            )}
            <Button
                type="button"
                variant="outline"
                size="icon"
                aria-label="Models and API keys"
                title="Models and API keys"
                onClick={onManage}
                className="size-8 rounded-full border-2"
            >
                <KeyRound aria-hidden="true" className="size-4" />
            </Button>
        </div>
    );
}
