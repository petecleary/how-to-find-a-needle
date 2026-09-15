// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { gq01OntologyResponse } from '@/test/responses';
import { ResultsTab } from './ResultsTab';

afterEach(cleanup);

describe('ResultsTab on Stage 5, GQ-01', () => {
    it('puts the near miss in the Flagged column with the checks it failed', () => {
        render(
            <ResultsTab
                response={gq01OntologyResponse}
                taxonomy={null}
                targetProductId="PROD-0001"
                applyConstraints
            />,
        );

        expect(screen.getByText('Flagged · 7 incompatible')).not.toBeNull();

        const card = within(screen.getByRole('article', { name: 'Voltline 45W Barrel Charger' }));
        expect(card.getByText('#2 before rules')).not.toBeNull();
        expect(card.getByText('5.5mm barrel')).not.toBeNull();
        expect(card.getAllByText('Fails:')).toHaveLength(2);
    });

    it('keeps out-of-concept items one click away, counted', () => {
        render(
            <ResultsTab
                response={gq01OntologyResponse}
                taxonomy={null}
                targetProductId="PROD-0001"
                applyConstraints
            />,
        );

        expect(screen.getByText('Out of concept · kept, moved down · 40')).not.toBeNull();
        fireEvent.click(screen.getByRole('button', { name: 'Show 36 more' }));

        expect(screen.getByText('Blackbird Aerobook 14')).not.toBeNull();
        expect(screen.getByText(/This is your target device/)).not.toBeNull();
    });

    it('shows one list with concept badges when the rules are off', () => {
        render(
            <ResultsTab
                response={gq01OntologyResponse}
                taxonomy={null}
                targetProductId="PROD-0001"
                applyConstraints={false}
            />,
        );

        expect(screen.queryByText(/Flagged/)).toBeNull();
        expect(
            within(screen.getByRole('region', { name: 'Results' })).getAllByRole('listitem').length,
        ).toBeGreaterThanOrEqual(50);
    });
});
