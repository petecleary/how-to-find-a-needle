// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, describe, expect, it } from 'vitest';
import { explanationHeadings, findGlossaryEntry } from '@/lib/content';
import { StageExplanation } from './StageExplanation';

afterEach(cleanup);

function renderStage(stage: 'ontology' | 'rag') {
    render(
        <MemoryRouter>
            <StageExplanation stage={stage} />
        </MemoryRouter>,
    );
}

describe('StageExplanation', () => {
    it('shows every fixed heading for Stage 5', () => {
        renderStage('ontology');

        expect(screen.getByRole('heading', { level: 2, name: 'Ontology' })).not.toBeNull();
        for (const heading of explanationHeadings) {
            expect(screen.getByRole('heading', { level: 3, name: heading })).not.toBeNull();
        }
    });

    it('links the decision record to its page', () => {
        renderStage('ontology');

        const link = screen.getByRole('link', { name: /ADR-0013/ });

        expect(link.getAttribute('href')).toBe('/decisions/0013-domain-ontology-and-compatibility');
    });

    it('opens a glossary definition when the term gets keyboard focus', async () => {
        renderStage('ontology');

        fireEvent.focus(screen.getByRole('link', { name: 'SKOS' }));

        const definition = findGlossaryEntry('skos')?.definition ?? '';
        expect(await screen.findByText(definition)).not.toBeNull();
    });

    it('shows Stage 6’s explanation, linked to its decision record', () => {
        renderStage('rag');

        expect(screen.getByRole('heading', { level: 2, name: 'RAG' })).not.toBeNull();
        expect(screen.getByRole('link', { name: /ADR-0016/ }).getAttribute('href')).toBe(
            '/decisions/0016-rag-grounding-and-citations',
        );
    });
});
