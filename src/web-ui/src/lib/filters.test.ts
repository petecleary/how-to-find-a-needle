import { describe, expect, it } from 'vitest';
import { taxonomyFixture, vocabulariesFixture } from '@/test/ontologyFixtures';
import {
    changeSpecKey,
    clearFilters,
    hasSelectedNarrower,
    otherSpecs,
    parsePriceInput,
    removeSpec,
    setBrand,
    setVocabularyValue,
    specSelection,
    toggleCategory,
} from './filters';
import { defaultSearchState, toSearchRequest, type SearchFilterState } from './searchState';
import { findConcept } from './taxonomy';

const [connectors, memoryTypes] = vocabulariesFixture as [
    (typeof vocabulariesFixture)[0],
    (typeof vocabulariesFixture)[0],
];

function withFilters(change: Partial<SearchFilterState>): SearchFilterState {
    return { ...clearFilters(), ...change };
}

describe('categories', () => {
    it('finds a concept at any depth', () => {
        expect(findConcept(taxonomyFixture, 'usb-c-pd-chargers')?.label).toBe('USB-C PD chargers');
        expect(findConcept(taxonomyFixture, 'not-a-concept')).toBeNull();
    });

    it('selects and deselects a category', () => {
        const selected = toggleCategory(clearFilters(), taxonomyFixture, 'laptops');
        expect(selected.categories).toEqual(['laptops']);

        expect(toggleCategory(selected, taxonomyFixture, 'laptops').categories).toEqual([]);
    });

    it('drops selected narrower concepts when their broader concept is selected', () => {
        const filters = withFilters({ categories: ['laptop-chargers', 'laptops'] });

        expect(toggleCategory(filters, taxonomyFixture, 'power').categories).toEqual(['laptops', 'power']);
    });

    it('knows when a branch holds a selection', () => {
        const power = findConcept(taxonomyFixture, 'power');

        expect(power && hasSelectedNarrower(power, ['usb-c-pd-chargers'])).toBe(true);
        expect(power && hasSelectedNarrower(power, ['laptops'])).toBe(false);
    });
});

describe('spec vocabularies', () => {
    it('matches the first spec key until another is chosen', () => {
        expect(specSelection(clearFilters(), connectors)).toEqual({ key: 'chargingPort', value: null });
        expect(specSelection(withFilters({ specs: { connector: 'usb-c' } }), connectors)).toEqual({
            key: 'connector',
            value: 'usb-c',
        });
    });

    it('has no selection for a vocabulary no rule uses', () => {
        expect(specSelection(clearFilters(), { ...memoryTypes, specs: [] })).toBeNull();
    });

    it("sets the value's notation, keeping one key per vocabulary", () => {
        const onPort = setVocabularyValue(clearFilters(), connectors, 'chargingPort', 'usb-c');
        const onConnector = setVocabularyValue(onPort, connectors, 'connector', 'barrel-5.5mm');

        expect(onPort.specs).toEqual({ chargingPort: 'usb-c' });
        expect(onConnector.specs).toEqual({ connector: 'barrel-5.5mm' });
        expect(setVocabularyValue(onConnector, connectors, 'connector', null).specs).toEqual({});
    });

    it('moves the value when the spec key changes', () => {
        const filters = withFilters({ specs: { chargingPort: 'usb-c', memoryType: 'ddr5-sodimm' } });

        expect(changeSpecKey(filters, connectors, 'connector').specs).toEqual({
            connector: 'usb-c',
            memoryType: 'ddr5-sodimm',
        });
    });

    it('lists spec filters that no vocabulary covers, and removes them', () => {
        const filters = withFilters({ specs: { voltageV: 18, connector: 'usb-c' } });

        expect(otherSpecs(filters, vocabulariesFixture)).toEqual([['voltageV', 18]]);
        expect(removeSpec(filters, 'voltageV').specs).toEqual({ connector: 'usb-c' });
    });
});

describe('brand and price', () => {
    it('trims the brand, and treats blank as any brand', () => {
        expect(setBrand(clearFilters(), '  Brakk ').brand).toBe('Brakk');
        expect(setBrand(clearFilters(), '   ').brand).toBeNull();
    });

    it('reads a price box', () => {
        expect(parsePriceInput('49.99')).toBe(49.99);
        expect(parsePriceInput('')).toBeNull();
        expect(parsePriceInput('-5')).toBeNull();
        expect(parsePriceInput('cheap')).toBeNull();
    });
});

describe('selecting values builds the request filters', () => {
    it('sends notations, the chosen spec key and numbers as numbers', () => {
        let filters = clearFilters();
        filters = toggleCategory(filters, taxonomyFixture, 'chargers');
        filters = setVocabularyValue(filters, connectors, 'connector', 'usb-c');
        filters = setBrand(filters, 'Voltline');
        filters = { ...filters, maxPrice: parsePriceInput('50') };

        expect(toSearchRequest({ ...defaultSearchState, filters }).filters).toEqual({
            brand: 'Voltline',
            categories: ['chargers'],
            minPrice: null,
            maxPrice: 50,
            specs: { connector: 'usb-c' },
        });
    });

    it('sends nulls once every filter is cleared', () => {
        expect(toSearchRequest({ ...defaultSearchState, filters: clearFilters() }).filters).toEqual({
            brand: null,
            categories: null,
            minPrice: null,
            maxPrice: null,
            specs: null,
        });
    });
});
