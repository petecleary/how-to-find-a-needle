// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, describe, expect, it } from 'vitest';
import { GoingFurtherTab } from './GoingFurtherTab';

// The content is real: these check that a stage with a file renders it, and that Stage 1's absence is handled.
afterEach(cleanup);

function renderTab(stage: 'structured' | 'hybrid') {
    render(
        <MemoryRouter>
            <GoingFurtherTab stage={stage} />
        </MemoryRouter>,
    );
}

describe('GoingFurtherTab', () => {
    it("renders a stage's going-further content", () => {
        renderTab('hybrid');

        expect(screen.queryByText('Where this goes next')).not.toBeNull();
        expect(screen.queryByRole('heading', { name: /Re-ranking with a cross-encoder/ })).not.toBeNull();
    });

    it('says so when the stage has no file, instead of rendering an empty panel', () => {
        renderTab('structured');

        expect(screen.queryByText(/content\/going-further\/structured\.md/)).not.toBeNull();
    });
});
