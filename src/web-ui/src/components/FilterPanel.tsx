import { Funnel, X } from 'lucide-react';
import type { ReactNode } from 'react';
import type { TaxonomyNode, ValueVocabulary } from '@/api/client';
import { CategoryTree } from '@/components/CategoryTree';
import { CommittedInput } from '@/components/CommittedInput';
import { SpecVocabularyFilter } from '@/components/SpecVocabularyFilter';
import { Button } from '@/components/ui/button';
import type { ApiData } from '@/hooks/useApiData';
import {
    clearFilters,
    otherSpecs,
    parsePriceInput,
    removeSpec,
    setBrand,
    toggleCategory,
} from '@/lib/filters';
import { countActiveFilters, type SearchFilterState } from '@/lib/searchState';

// FilterPanel — structured pre-filters, built from the ontology. The category tree comes from
// GET /api/taxonomy and each spec filter from GET /api/vocabularies, so a category, value or synonym added
// to domain-ontology.ttl appears here after re-running the AppHost, with no UI change (ADR-0013, ADR-0014).
// Filters narrow the catalogue before any ranking, in every stage, and they are Stage 1's only input
// (ADR-0007).

export interface FilterPanelProps {
    filters: SearchFilterState;
    taxonomy: ApiData<TaxonomyNode[]>;
    vocabularies: ApiData<ValueVocabulary[]>;
    onChange: (filters: SearchFilterState) => void;
}

export function FilterPanel({ filters, taxonomy, vocabularies, onChange }: FilterPanelProps) {
    const others = vocabularies.data === null ? [] : otherSpecs(filters, vocabularies.data);

    return (
        <div className="flex flex-col gap-5">
            <div className="flex flex-col gap-1">
                <div className="flex items-center gap-2">
                    <Funnel aria-hidden="true" className="size-5" />
                    <h2 className="text-lg font-bold">Filters</h2>
                    <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        disabled={countActiveFilters(filters) === 0}
                        onClick={() => onChange(clearFilters())}
                        className="ml-auto rounded-full text-muted-foreground"
                    >
                        Clear
                    </Button>
                </div>
                <p className="text-sm text-muted-foreground">
                    Built from the ontology, nothing hard-coded. Edit the TTL, re-run, refresh.
                </p>
            </div>

            <div className="flex flex-col gap-1.5">
                <label htmlFor="filter-brand" className="font-bold">
                    Brand
                </label>
                <CommittedInput
                    id="filter-brand"
                    value={filters.brand ?? ''}
                    onCommit={(text) => onChange(setBrand(filters, text))}
                    placeholder="Any brand"
                    className="rounded-full border-2 shadow-none"
                />
                <p className="text-xs text-muted-foreground">The exact name, in any case.</p>
            </div>

            <fieldset className="flex flex-col gap-1.5">
                <legend className="font-bold">Price (£)</legend>
                <div className="flex items-center gap-2">
                    <CommittedInput
                        aria-label="Minimum price"
                        type="number"
                        min={0}
                        step="0.01"
                        value={filters.minPrice?.toString() ?? ''}
                        onCommit={(text) => onChange({ ...filters, minPrice: parsePriceInput(text) })}
                        placeholder="Min"
                        className="rounded-full border-2 shadow-none"
                    />
                    <span className="text-muted-foreground">to</span>
                    <CommittedInput
                        aria-label="Maximum price"
                        type="number"
                        min={0}
                        step="0.01"
                        value={filters.maxPrice?.toString() ?? ''}
                        onCommit={(text) => onChange({ ...filters, maxPrice: parsePriceInput(text) })}
                        placeholder="Max"
                        className="rounded-full border-2 shadow-none"
                    />
                </div>
            </fieldset>

            <fieldset className="flex flex-col gap-1">
                <legend className="font-bold">Category</legend>
                <p className="text-xs text-muted-foreground">A parent includes its narrower concepts.</p>
                <Loaded resource={taxonomy} name="categories" path="/api/taxonomy">
                    {(concepts) => (
                        <CategoryTree
                            taxonomy={concepts}
                            selected={filters.categories}
                            onToggle={(notation) => onChange(toggleCategory(filters, concepts, notation))}
                        />
                    )}
                </Loaded>
            </fieldset>

            <Loaded resource={vocabularies} name="spec vocabularies" path="/api/vocabularies">
                {(schemes) =>
                    schemes.map((vocabulary) => (
                        <SpecVocabularyFilter
                            key={vocabulary.notation}
                            vocabulary={vocabulary}
                            filters={filters}
                            onChange={onChange}
                        />
                    ))
                }
            </Loaded>

            {others.length > 0 ? (
                <fieldset className="flex flex-col gap-1.5">
                    <legend className="font-bold">Other specs</legend>
                    <p className="text-xs text-muted-foreground">
                        Not from a vocabulary (a golden query can set these). Matched exactly: 18 is not "18".
                    </p>
                    <ul className="flex flex-wrap gap-1.5">
                        {others.map(([key, value]) => (
                            <li
                                key={key}
                                className="flex items-center gap-1 rounded-full border-2 py-0.5 pr-1 pl-2.5 font-mono text-sm"
                            >
                                {key} = {JSON.stringify(value)}
                                <button
                                    type="button"
                                    aria-label={`Remove the ${key} filter`}
                                    onClick={() => onChange(removeSpec(filters, key))}
                                    className="rounded-full p-0.5 text-muted-foreground hover:bg-muted hover:text-foreground"
                                >
                                    <X aria-hidden="true" className="size-3.5" />
                                </button>
                            </li>
                        ))}
                    </ul>
                </fieldset>
            ) : null}
        </div>
    );
}

interface LoadedProps<T> {
    resource: ApiData<T>;
    name: string;
    path: string;
    children: (data: T) => ReactNode;
}

// The filters can't be built without the ontology, so a failed load says which endpoint failed.
function Loaded<T>({ resource, name, path, children }: LoadedProps<T>) {
    if (resource.status === 'error') {
        return (
            <p role="alert" className="text-sm text-incompatible-ink">
                Couldn't load the {name} from <code className="font-mono">{path}</code>:{' '}
                {resource.error?.message}
            </p>
        );
    }

    if (resource.data === null) {
        return <p className="text-sm text-muted-foreground">Loading the {name}…</p>;
    }

    return children(resource.data);
}
