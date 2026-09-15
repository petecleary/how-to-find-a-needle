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

    it('moves to the next stage with the right arrow key', () => {
        const onChooseStage = vi.fn();
        render(<PipelineStepper stage="hybrid" onChooseStage={onChooseStage} />);

        fireEvent.keyDown(screen.getByRole('tab', { selected: true }), { key: 'ArrowRight' });

        expect(onChooseStage).toHaveBeenCalledWith('ontology');
    });

    it('skips stages without an endpoint, wrapping round to Stage 1', () => {
        const onChooseStage = vi.fn();
        render(<PipelineStepper stage="ontology" onChooseStage={onChooseStage} />);

        fireEvent.keyDown(screen.getByRole('tab', { selected: true }), { key: 'ArrowRight' });

        expect(onChooseStage).toHaveBeenCalledWith('structured');
    });

    it('does not choose a stage without an endpoint when clicked', () => {
        const onChooseStage = vi.fn();
        render(<PipelineStepper stage="ontology" onChooseStage={onChooseStage} />);
        const rag = screen.getByRole('tab', { name: /RAG/ });

        fireEvent.click(rag);

        expect(rag.getAttribute('aria-disabled')).toBe('true');
        expect(onChooseStage).not.toHaveBeenCalled();
    });
});
