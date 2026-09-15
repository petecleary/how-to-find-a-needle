// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PipelineStepper } from './PipelineStepper';

afterEach(cleanup);

describe('PipelineStepper', () => {
    it('shows all seven stages as tabs, with the current stage selected', () => {
        render(<PipelineStepper stage="hybrid" onChooseStage={vi.fn()} />);

        expect(screen.getAllByRole('tab')).toHaveLength(7);
        expect(screen.getByRole('tab', { selected: true }).textContent).toContain('Hybrid');
    });

    it('moves to the next stage with the right arrow key, from Stage 5 into the Pedagogy group', () => {
        const onChooseStage = vi.fn();
        render(<PipelineStepper stage="ontology" onChooseStage={onChooseStage} />);

        fireEvent.keyDown(screen.getByRole('tab', { selected: true }), { key: 'ArrowRight' });

        expect(onChooseStage).toHaveBeenCalledWith('rag');
    });

    it('wraps round from Stage 7 to Stage 1', () => {
        const onChooseStage = vi.fn();
        render(<PipelineStepper stage="pedagogy" onChooseStage={onChooseStage} />);

        fireEvent.keyDown(screen.getByRole('tab', { selected: true }), { key: 'ArrowRight' });

        expect(onChooseStage).toHaveBeenCalledWith('structured');
    });

    it('chooses a stage when clicked', () => {
        const onChooseStage = vi.fn();
        render(<PipelineStepper stage="ontology" onChooseStage={onChooseStage} />);

        fireEvent.click(screen.getByRole('tab', { name: /Pedagogy/ }));

        expect(onChooseStage).toHaveBeenCalledWith('pedagogy');
    });
});
