// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { TaxonomyNode, ValueVocabulary } from '@/api/client';
import type { ApiData } from '@/hooks/useApiData';
import { clearFilters } from '@/lib/filters';
import type { SearchFilterState } from '@/lib/searchState';
import { taxonomyFixture, vocabulariesFixture } from '@/test/ontologyFixtures';
import { FilterPanel } from './FilterPanel';

const taxonomy: ApiData<TaxonomyNode[]> = { status: 'success', data: taxonomyFixture, error: null };
const vocabularies: ApiData<ValueVocabulary[]> = {
    status: 'success',
    data: vocabulariesFixture,
    error: null,
};

afterEach(cleanup);

function renderPanel(filters: SearchFilterState = clearFilters()) {
    const onChange = vi.fn<(filters: SearchFilterState) => void>();
    render(
        <FilterPanel filters={filters} taxonomy={taxonomy} vocabularies={vocabularies} onChange={onChange} />,
    );
    return onChange;
}

describe('FilterPanel', () => {
    it('builds the category tree and spec values from the ontology it is given', () => {
        renderPanel();

        expect(screen.getByLabelText('Power')).not.toBeNull();
        expect(screen.getByText('also: Type-C, USB Type-C')).not.toBeNull();
        expect(screen.getByText('Memory types')).not.toBeNull();
    });

    it('sends the notation of a ticked category', () => {
        const onChange = renderPanel();

        fireEvent.click(screen.getByRole('button', { name: 'Show narrower concepts of Power' }));
        fireEvent.click(screen.getByRole('checkbox', { name: 'Chargers' }));

        expect(onChange).toHaveBeenLastCalledWith({ ...clearFilters(), categories: ['chargers'] });
    });

    it('shows narrower concepts as included, not separately selectable, under a ticked parent', () => {
        renderPanel({ ...clearFilters(), categories: ['chargers'] });

        // Power starts expanded, because a concept inside it is selected.
        expect(screen.getByRole('button', { name: 'Hide narrower concepts of Power' })).not.toBeNull();
        fireEvent.click(screen.getByRole('button', { name: 'Show narrower concepts of Chargers' }));
        const child = screen.getByRole('checkbox', { name: 'Laptop chargers, included by Chargers' });

        expect(child.getAttribute('aria-checked')).toBe('true');
        expect(child.hasAttribute('disabled')).toBe(true);
    });

    it("sends a value's notation on the chosen spec key", () => {
        const onChange = renderPanel();

        fireEvent.click(screen.getByRole('radio', { name: /^USB-C/ }));

        expect(onChange).toHaveBeenLastCalledWith({ ...clearFilters(), specs: { chargingPort: 'usb-c' } });
    });

    it('moves the value to another spec key', () => {
        const onChange = renderPanel({ ...clearFilters(), specs: { chargingPort: 'usb-c' } });

        fireEvent.click(screen.getByRole('radio', { name: 'connector' }));

        expect(onChange).toHaveBeenLastCalledWith({ ...clearFilters(), specs: { connector: 'usb-c' } });
    });

    it('applies the brand on Enter', () => {
        const onChange = renderPanel();
        const brand = screen.getByLabelText('Brand');

        fireEvent.change(brand, { target: { value: 'Voltline' } });
        expect(onChange).not.toHaveBeenCalled();

        fireEvent.keyDown(brand, { key: 'Enter' });
        expect(onChange).toHaveBeenLastCalledWith({ ...clearFilters(), brand: 'Voltline' });
    });

    it('lists a spec filter no vocabulary covers, so it is never hidden', () => {
        const onChange = renderPanel({ ...clearFilters(), brand: 'Brakk', specs: { voltageV: 18 } });

        expect(screen.getByText('voltageV = 18')).not.toBeNull();
        fireEvent.click(screen.getByRole('button', { name: 'Remove the voltageV filter' }));

        expect(onChange).toHaveBeenLastCalledWith({ ...clearFilters(), brand: 'Brakk' });
    });

    it('says which endpoint failed when the ontology cannot be loaded', () => {
        render(
            <FilterPanel
                filters={clearFilters()}
                taxonomy={{ status: 'error', data: null, error: new Error('502 Bad Gateway') }}
                vocabularies={vocabularies}
                onChange={vi.fn()}
            />,
        );

        expect(screen.getByRole('alert').textContent).toContain('/api/taxonomy');
    });
});
