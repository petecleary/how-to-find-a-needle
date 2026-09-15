import type { TaxonomyNode, ValueVocabulary } from '@/api/client';
import { defaultSearchState, type SearchFilterState, type SpecValue } from './searchState';
import { findConcept, narrowerNotations } from './taxonomy';

// The rules behind FilterPanel, kept free of React so they can be tested on their own. Every function
// takes the current filters and returns new ones; the panel only decides which function a click calls.
// The taxonomy and vocabularies come from the API, so nothing here knows a category or spec value by name.

/** True when a concept somewhere below this one is selected, so its branch should start expanded. */
export function hasSelectedNarrower(concept: TaxonomyNode, selected: readonly string[]): boolean {
    return narrowerNotations(concept).some((notation) => selected.includes(notation));
}

/** Selects or deselects a category. */
export function toggleCategory(
    filters: SearchFilterState,
    taxonomy: readonly TaxonomyNode[],
    notation: string,
): SearchFilterState {
    const { categories } = filters;

    if (categories.includes(notation)) {
        return { ...filters, categories: categories.filter((selected) => selected !== notation) };
    }

    // A broader concept already includes its narrower ones: the API expands "chargers" to chargers,
    // laptop-chargers, phone-chargers and usb-c-pd-chargers (ADR-0013). So selecting it drops any narrower
    // concept that was selected before, and the request and the URL say only what was meant.
    const concept = findConcept(taxonomy, notation);
    const redundant = concept === null ? [] : narrowerNotations(concept);

    return {
        ...filters,
        categories: [...categories.filter((selected) => !redundant.includes(selected)), notation],
    };
}

/** The spec key a vocabulary's filter matches on, and the value chosen for it (`null` for any value). */
export interface SpecSelection {
    key: string;
    value: SpecValue | null;
}

/**
 * One vocabulary can hold the values of several spec keys: connectors are `connector` on a charger and
 * `chargingPort` on a laptop. The filter matches one key at a time, and uses the first key until another
 * is chosen. Returns `null` when no rule uses the vocabulary, because then there is no key to match.
 */
export function specSelection(filters: SearchFilterState, vocabulary: ValueVocabulary): SpecSelection | null {
    const keyInUse = vocabulary.specs.find((key) => Object.hasOwn(filters.specs, key));
    const key = keyInUse ?? vocabulary.specs[0];

    if (key === undefined) {
        return null;
    }

    return { key, value: filters.specs[key] ?? null };
}

/**
 * Sets a vocabulary's value on one of its spec keys, or clears it with `null`. The value is the concept's
 * notation ("usb-c"), never a label ("Type-C"): products store notations, and specs match exactly.
 */
export function setVocabularyValue(
    filters: SearchFilterState,
    vocabulary: ValueVocabulary,
    key: string,
    value: SpecValue | null,
): SearchFilterState {
    // One key per vocabulary: `connector: usb-c` and `chargingPort: usb-c` together would match nothing,
    // because no product is both a charger and a laptop.
    const specs = withoutKeys(filters.specs, vocabulary.specs);
    if (value !== null) {
        specs[key] = value;
    }

    return { ...filters, specs };
}

/** Moves a vocabulary's chosen value to another of its spec keys. */
export function changeSpecKey(
    filters: SearchFilterState,
    vocabulary: ValueVocabulary,
    key: string,
): SearchFilterState {
    const value = specSelection(filters, vocabulary)?.value ?? null;
    return setVocabularyValue(filters, vocabulary, key, value);
}

/**
 * Spec filters no vocabulary covers, such as GQ-04's `voltageV: 18`. They still apply, so the panel lists
 * them rather than hiding a filter that changes the results.
 */
export function otherSpecs(
    filters: SearchFilterState,
    vocabularies: readonly ValueVocabulary[],
): [key: string, value: SpecValue][] {
    const vocabularyKeys = new Set(vocabularies.flatMap((vocabulary) => vocabulary.specs));

    return Object.entries(filters.specs)
        .filter(([key]) => !vocabularyKeys.has(key))
        .sort(([a], [b]) => a.localeCompare(b));
}

export function removeSpec(filters: SearchFilterState, key: string): SearchFilterState {
    return { ...filters, specs: withoutKeys(filters.specs, [key]) };
}

/** The API matches brands exactly, ignoring case, so only surrounding spaces are trimmed. */
export function setBrand(filters: SearchFilterState, text: string): SearchFilterState {
    const brand = text.trim();
    return { ...filters, brand: brand === '' ? null : brand };
}

/** Reads a price box: blank, negative or not a number means no limit. */
export function parsePriceInput(text: string): number | null {
    const trimmed = text.trim();
    if (trimmed === '') {
        return null;
    }

    const price = Number(trimmed);
    return Number.isFinite(price) && price >= 0 ? price : null;
}

export function clearFilters(): SearchFilterState {
    const { filters } = defaultSearchState;
    return { ...filters, categories: [], specs: {} };
}

function withoutKeys(specs: Record<string, SpecValue>, keys: readonly string[]): Record<string, SpecValue> {
    return Object.fromEntries(Object.entries(specs).filter(([key]) => !keys.includes(key)));
}
