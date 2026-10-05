// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { StageOptions, type StageOptionValues } from './StageOptions';

afterEach(cleanup);

const values: StageOptionValues = {
    expandSynonyms: true,
    applyConstraints: true,
    audience: 'novice',
    applyPedagogy: true,
    model: null,
};

describe('StageOptions', () => {
    it('offers the audience and the pedagogy switch on Stage 7 only', () => {
        const { rerender } = render(<StageOptions stage="pedagogy" {...values} onChange={vi.fn()} />);

        expect(screen.getByRole('radio', { name: 'novice' }).getAttribute('aria-checked')).toBe('true');
        expect(screen.getByRole('switch', { name: 'Apply pedagogy' })).not.toBeNull();

        rerender(<StageOptions stage="rag" {...values} onChange={vi.fn()} />);

        expect(screen.queryByRole('radio')).toBeNull();
        expect(screen.queryByRole('switch')).toBeNull();
    });

    it('chooses an audience, and ignores clicking the chosen one again', () => {
        const onChange = vi.fn();
        render(<StageOptions stage="pedagogy" {...values} onChange={onChange} />);

        fireEvent.click(screen.getByRole('radio', { name: 'expert' }));
        fireEvent.click(screen.getByRole('radio', { name: 'novice' }));

        expect(onChange).toHaveBeenCalledTimes(1);
        expect(onChange).toHaveBeenCalledWith({ audience: 'expert' });
    });

    it('turns pedagogy off for the baseline prompt', () => {
        const onChange = vi.fn();
        render(<StageOptions stage="pedagogy" {...values} onChange={onChange} />);

        fireEvent.click(screen.getByRole('switch', { name: 'Apply pedagogy' }));

        expect(onChange).toHaveBeenCalledWith({ applyPedagogy: false });
    });

    it('refreshes the RAG answer without showing stage options', () => {
        const onRefreshAnswer = vi.fn();
        render(<StageOptions stage="rag" {...values} onChange={vi.fn()} onRefreshAnswer={onRefreshAnswer} />);

        fireEvent.click(screen.getByRole('button', { name: 'Refresh answer' }));

        expect(onRefreshAnswer).toHaveBeenCalledOnce();
        expect(screen.queryByRole('switch')).toBeNull();
        expect(screen.queryByRole('radio')).toBeNull();
    });
    it('offers the model on Stages 6 and 7 only', () => {
        const { rerender } = render(<StageOptions stage="rag" {...values} onChange={vi.fn()} />);
        expect(screen.getByRole('button', { name: 'Models and API keys' })).not.toBeNull();

        rerender(<StageOptions stage="pedagogy" {...values} onChange={vi.fn()} />);
        expect(screen.getByRole('button', { name: 'Models and API keys' })).not.toBeNull();

        rerender(<StageOptions stage="hybrid" {...values} onChange={vi.fn()} />);
        expect(screen.queryByRole('button', { name: 'Models and API keys' })).toBeNull();
    });
});
