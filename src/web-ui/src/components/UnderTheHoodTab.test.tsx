// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { gq03OntologyResponse, gq01StructuredResponse } from '@/test/responses';
import { UnderTheHoodTab } from './UnderTheHoodTab';

afterEach(cleanup);

// Radix tabs select on mouse down, as a pointer press does, rather than on click.
function selectStep(name: RegExp) {
    fireEvent.mouseDown(screen.getByRole('tab', { name }), { button: 0 });
}

describe('UnderTheHoodTab, GQ-03 on Stage 5', () => {
    it('shows one chip per step and opens on the last one, the rule checks', () => {
        render(<UnderTheHoodTab response={gq03OntologyResponse} />);

        expect(screen.getAllByRole('tab').map((tab) => tab.textContent)).toEqual([
            '1 Understand',
            '2 Expand',
            '3 Keyword',
            '4 Embed',
            '5 Vector',
            '6 RRF',
            '7 Classify',
            '8 Constrain',
        ]);
        expect(screen.getByText('chargers → laptops')).not.toBeNull();
    });

    it('draws every step with a purpose-built view, never the JSON fallback', () => {
        render(<UnderTheHoodTab response={gq03OntologyResponse} />);

        for (const tab of screen.getAllByRole('tab')) {
            fireEvent.mouseDown(tab, { button: 0 });
            expect(tab.getAttribute('aria-selected')).toBe('true');
            expect(screen.queryByText(/no purpose-built view/)).toBeNull();
        }
    });

    it("shows the RRF maths as the API wrote it, next to the rules' verdict", () => {
        render(<UnderTheHoodTab response={gq03OntologyResponse} />);

        selectStep(/RRF/);

        expect(screen.getByText('1/(60+1) + 1/(60+4)')).not.toBeNull();
        expect(screen.getByText('0.03202')).not.toBeNull();
        expect(screen.getByRole('columnheader', { name: 'The rules say' })).not.toBeNull();
    });
});

describe('UnderTheHoodTab, GQ-01 on Stage 1', () => {
    it('shows the parameterised SQL with its parameters beside it', () => {
        render(<UnderTheHoodTab response={gq01StructuredResponse} />);

        expect(screen.getAllByText(/specs @> @specs::jsonb/).length).toBeGreaterThan(0);
        expect(screen.getByText('@brand')).not.toBeNull();
        expect(screen.getByText('"Brakk"')).not.toBeNull();
    });
});
