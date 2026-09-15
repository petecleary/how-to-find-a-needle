import { useId } from 'react';
import type { ValueVocabulary } from '@/api/client';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group';
import { changeSpecKey, setVocabularyValue, specSelection } from '@/lib/filters';
import type { SearchFilterState } from '@/lib/searchState';

// SpecVocabularyFilter — one SKOS value vocabulary as a spec filter. Each value is a concept: the filter
// sends its notation ("usb-c") and shows its synonyms ("Type-C") as hints, because shoppers and product data
// use different words for the same thing. The spec key comes from the domain rules (ADR-0013), so the
// audience can see that connectors are called `connector` on a charger and `chargingPort` on a laptop.

// Radix radio items need a string value; notations are lower-case words and hyphens, so this can't clash.
const anyValue = ':any';

export interface SpecVocabularyFilterProps {
    vocabulary: ValueVocabulary;
    filters: SearchFilterState;
    onChange: (filters: SearchFilterState) => void;
}

export function SpecVocabularyFilter({ vocabulary, filters, onChange }: SpecVocabularyFilterProps) {
    const baseId = useId();
    const selection = specSelection(filters, vocabulary);

    if (selection === null) {
        return (
            <fieldset className="flex flex-col gap-1">
                <legend className="font-bold">{vocabulary.label}</legend>
                <p className="text-xs text-muted-foreground">
                    No rule uses this vocabulary yet, so there is no spec key to filter on.
                </p>
            </fieldset>
        );
    }

    const { key } = selection;

    return (
        <fieldset className="flex flex-col gap-1.5">
            <legend className="font-bold">{vocabulary.label}</legend>

            {vocabulary.specs.length > 1 ? (
                <div className="flex flex-wrap items-center gap-1.5 text-xs text-muted-foreground">
                    <span id={`${baseId}-key`}>Spec key</span>
                    <ToggleGroup
                        type="single"
                        variant="outline"
                        size="sm"
                        value={key}
                        aria-labelledby={`${baseId}-key`}
                        // Radix reports "" when the pressed item is pressed again; a key must stay chosen.
                        onValueChange={(next) =>
                            next !== '' && onChange(changeSpecKey(filters, vocabulary, next))
                        }
                    >
                        {vocabulary.specs.map((specKey) => (
                            <ToggleGroupItem
                                key={specKey}
                                value={specKey}
                                className="h-6 px-2 font-mono text-xs"
                            >
                                {specKey}
                            </ToggleGroupItem>
                        ))}
                    </ToggleGroup>
                </div>
            ) : (
                <p className="text-xs text-muted-foreground">
                    Spec key <code className="font-mono">{key}</code>
                </p>
            )}

            <RadioGroup
                aria-label={`${vocabulary.label} on ${key}`}
                value={selection.value === null ? anyValue : String(selection.value)}
                onValueChange={(value) =>
                    onChange(setVocabularyValue(filters, vocabulary, key, value === anyValue ? null : value))
                }
                className="gap-1.5"
            >
                <div className="flex items-center gap-2">
                    <RadioGroupItem id={`${baseId}-any`} value={anyValue} className="border-2" />
                    <label htmlFor={`${baseId}-any`} className="text-[15px]">
                        Any
                    </label>
                </div>
                {vocabulary.values.map((entry) => {
                    const itemId = `${baseId}-${entry.notation}`;

                    return (
                        <div key={entry.notation} className="flex items-start gap-2">
                            <RadioGroupItem id={itemId} value={entry.notation} className="mt-1 border-2" />
                            <label htmlFor={itemId} className="flex flex-col text-[15px] leading-tight">
                                {entry.label}
                                {entry.altLabels.length > 0 ? (
                                    <span className="text-xs text-muted-foreground">
                                        also: {entry.altLabels.join(', ')}
                                    </span>
                                ) : null}
                            </label>
                        </div>
                    );
                })}
            </RadioGroup>
        </fieldset>
    );
}
