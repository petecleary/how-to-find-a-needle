// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { ModelCatalogue } from '@/api/client';
import { ModelPicker } from './ModelPicker';

afterEach(cleanup);

const catalogue: ModelCatalogue = {
    default: 'ollama/qwen3.6:35b',
    providers: [
        {
            id: 'ollama',
            name: 'Ollama',
            isLocal: true,
            models: [{ ref: 'ollama/qwen3.6:35b', model: 'qwen3.6:35b', isDefault: true }],
            problem: null,
        },
    ],
};

describe('ModelPicker', () => {
    it('shows the default model when the request names none', () => {
        render(
            <ModelPicker
                catalogue={{ status: 'success', data: catalogue, error: null }}
                value={null}
                onChange={vi.fn()}
                onManage={vi.fn()}
            />,
        );

        expect(screen.getByRole('combobox', { name: 'Model' }).textContent).toContain(
            'qwen3.6:35b (default)',
        );
    });

    it('says the models are being listed, then why they could not be', () => {
        const { rerender } = render(
            <ModelPicker
                catalogue={{ status: 'loading', data: null, error: null }}
                value={null}
                onChange={vi.fn()}
                onManage={vi.fn()}
            />,
        );

        expect(screen.getByRole('status').textContent).toBe('Listing models…');

        rerender(
            <ModelPicker
                catalogue={{ status: 'error', data: null, error: new Error('502 Bad Gateway') }}
                value={null}
                onChange={vi.fn()}
                onManage={vi.fn()}
            />,
        );

        expect(screen.getByRole('status').textContent).toContain('502 Bad Gateway');
    });

    it('opens Models and API keys', () => {
        const onManage = vi.fn();
        render(<ModelPicker catalogue={null} value={null} onChange={vi.fn()} onManage={onManage} />);

        fireEvent.click(screen.getByRole('button', { name: 'Models and API keys' }));

        expect(onManage).toHaveBeenCalledOnce();
    });
});
