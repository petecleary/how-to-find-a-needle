// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { StageTabs } from './StageTabs';

const panels = {
    'how-it-works': <p>Explanation</p>,
    results: <p>Result list</p>,
    answer: <p>Generated answer</p>,
    'under-the-hood': <p>Trace steps</p>,
};

afterEach(cleanup);

describe('StageTabs', () => {
    it("shows only the chosen tab's panel", () => {
        render(<StageTabs stage="hybrid" tab="results" onChooseTab={vi.fn()} counts={{}} panels={panels} />);

        expect(screen.queryByText('Result list')).not.toBeNull();
        expect(screen.queryByText('Trace steps')).toBeNull();
    });

    it('jumps to a tab with its letter key', () => {
        const onChooseTab = vi.fn();
        render(
            <StageTabs
                stage="ontology"
                tab="results"
                onChooseTab={onChooseTab}
                counts={{}}
                panels={panels}
            />,
        );

        fireEvent.keyDown(window, { key: 'u' });

        expect(onChooseTab).toHaveBeenCalledWith('under-the-hood');
    });

    it('ignores the letter keys while typing a query', () => {
        const onChooseTab = vi.fn();
        render(
            <>
                <input aria-label="Search query" />
                <StageTabs
                    stage="ontology"
                    tab="results"
                    onChooseTab={onChooseTab}
                    counts={{}}
                    panels={panels}
                />
            </>,
        );

        fireEvent.keyDown(screen.getByLabelText('Search query'), { key: 'h' });

        expect(onChooseTab).not.toHaveBeenCalled();
    });

    it('disables the Answer tab, and its A key, before Stage 6', () => {
        const onChooseTab = vi.fn();
        render(
            <StageTabs
                stage="ontology"
                tab="results"
                onChooseTab={onChooseTab}
                counts={{}}
                panels={panels}
            />,
        );

        fireEvent.keyDown(window, { key: 'a' });

        expect(screen.getByRole('tab', { name: /Answer/ }).hasAttribute('disabled')).toBe(true);
        expect(onChooseTab).not.toHaveBeenCalled();
    });
});
