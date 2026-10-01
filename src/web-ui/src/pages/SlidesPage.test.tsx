// @vitest-environment jsdom
import { act, cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router';
import { slides } from '@/lib/slides';
import { ThemeContext } from '@/lib/theme';
import { SlidesPage } from './SlidesPage';

// The deck asks the API for the golden queries, to fill the demo's query; an empty list keeps the test offline.
vi.mock('@/api/client', () => ({ getGoldenQueries: () => Promise.resolve([]) }));

function CurrentPath() {
    return <output data-testid="path">{useLocation().pathname}</output>;
}

async function renderDeckAt(path: string) {
    // A fixed light theme: the logo and the theme toggle read it, and jsdom has no system theme to follow.
    const theme = { preference: 'light', resolvedTheme: 'light', setPreference: () => undefined } as const;

    render(
        <ThemeContext value={theme}>
            <MemoryRouter initialEntries={[path]}>
                <Routes>
                    <Route path="/slides/:slide" element={<SlidesPage />} />
                </Routes>
                <CurrentPath />
            </MemoryRouter>
        </ThemeContext>,
    );
    // Let the golden-query request settle, so its state update lands inside act().
    await act(() => Promise.resolve());
}

const first = slides[0];
const second = slides[1];
if (first === undefined || second === undefined) throw new Error('The deck needs at least two slides');

beforeEach(() => {
    vi.spyOn(window, 'open').mockReturnValue(null);
});

afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
});

describe('SlidesPage', () => {
    it('shows the slide title as the heading', async () => {
        await renderDeckAt(`/slides/${second.id}`);

        expect(screen.getByRole('heading', { level: 1 }).textContent).toBe(second.title);
    });

    it('calls out the stage on a stage slide, with the slide title beneath', async () => {
        const stageSlide = slides.find((slide) => slide.section === 'structured');
        if (stageSlide === undefined) throw new Error('The deck has no Stage 1 slide');
        await renderDeckAt(`/slides/${stageSlide.id}`);

        expect(screen.getByRole('heading', { level: 1 }).textContent).toBe(
            `Stage 1 Search: Structured${stageSlide.title}`,
        );
    });

    it('→ moves to the next slide and ← back again', async () => {
        await renderDeckAt(`/slides/${first.id}`);

        fireEvent.keyDown(window, { key: 'ArrowRight' });
        expect(screen.getByTestId('path').textContent).toBe(`/slides/${second.id}`);

        fireEvent.keyDown(window, { key: 'ArrowLeft' });
        expect(screen.getByTestId('path').textContent).toBe(`/slides/${first.id}`);
    });

    it('D opens the demo in one named window, following the deck', async () => {
        const withDemo = slides.find((slide) => slide.demo !== undefined);
        if (withDemo === undefined) throw new Error('No slide in the deck names a demo');
        await renderDeckAt(`/slides/${withDemo.id}`);

        fireEvent.keyDown(window, { key: 'd' });

        expect(window.open).toHaveBeenCalledWith(
            expect.stringContaining(`follow=1&slide=${withDemo.id}`),
            'needle-demo',
        );
    });

    it('leaves D alone when a modifier is held', async () => {
        await renderDeckAt(`/slides/${first.id}`);

        fireEvent.keyDown(window, { key: 'd', metaKey: true });

        expect(window.open).not.toHaveBeenCalled();
    });
});
