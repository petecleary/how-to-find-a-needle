// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { AnswerFinal, SearchResponse } from '@/api/client';
import type { AnswerStream } from '@/hooks/useAnswerStream';
import type { PipelineSearch } from '@/hooks/usePipelineSearch';
import { AnswerTab } from './AnswerTab';

afterEach(cleanup);

// GQ-03 in miniature: the evidence step Stage 7's results response carries, and a finished answer and explanation.
const response: SearchResponse = {
    stage: 'pedagogy',
    query: 'power adapter for my laptop',
    page: 1,
    pageSize: 50,
    totalResults: 2,
    executionTimeMs: 20,
    results: [],
    debugTrace: {
        steps: [
            {
                stage: 'pedagogy',
                title: 'Evidence: what the model is given',
                durationMs: 1,
                details: {
                    evidence: [
                        {
                            id: 'PROD-0012',
                            name: 'Voltline 65W USB-C GaN Charger',
                            role: 'Compatible',
                            rank: 1,
                            compatibility: 'Compatible',
                            reasons: [],
                            conceptMatch: 'InConcept',
                            whyIncluded: '#1 in Stage 5’s order.',
                            description: null,
                        },
                        {
                            id: 'PROD-0014',
                            name: 'Voltline 45W Barrel Charger',
                            role: 'Incompatible',
                            rank: 44,
                            compatibility: 'Incompatible',
                            reasons: ['✗ Wrong plug.'],
                            conceptMatch: 'InConcept',
                            whyIncluded: '#44, included to warn.',
                            description: null,
                        },
                    ],
                    limits: { compatible: 5 },
                    concepts: [],
                    rules: [],
                },
                notes: [],
            },
        ],
    },
};

const search: PipelineSearch = {
    stage: 'pedagogy',
    request: {},
    status: 'success',
    response,
    error: null,
    rerun: vi.fn(),
};

function final(
    section: string,
    markdown: string,
    citations: string[],
    invalidCitations: string[] = [],
): AnswerFinal {
    return {
        section,
        markdown,
        citations,
        invalidCitations,
        insufficientEvidence: false,
        warnings: [],
        structure: null,
    };
}

const answerText = 'Get the Voltline [PROD-0012]; avoid [PROD-0014].';
const explanationText = '## Decision\nGet [PROD-0012].\n\n## Near miss\nNot [PROD-0099].';

const stream: AnswerStream = {
    status: 'done',
    meta: {
        stage: 'pedagogy',
        provider: 'ollama',
        model: 'qwen3.6:35b',
        evidence: ['PROD-0012', 'PROD-0014'],
    },
    sections: {
        answer: { markdown: answerText, final: final('answer', answerText, ['PROD-0012', 'PROD-0014']) },
        explanation: {
            markdown: explanationText,
            final: final('explanation', explanationText, ['PROD-0012', 'PROD-0099'], ['PROD-0099']),
        },
    },
    activeSection: null,
    done: { timeToFirstTokenMs: 59, totalMs: 6009, trace: [] },
    error: null,
    rerun: vi.fn(),
};

describe('AnswerTab, Stage 7', () => {
    it('colours citation chips by the product’s verdict, and marks one the model wasn’t given as invalid', () => {
        render(
            <AnswerTab stage="pedagogy" stream={stream} search={search} audience="novice" applyPedagogy />,
        );

        expect(
            screen.getAllByRole('button', { name: /^PROD-0012, Voltline 65W USB-C GaN Charger: Compatible/ }),
        ).toHaveLength(2);
        expect(screen.getByRole('button', { name: /^PROD-0014, .*: Incompatible/ })).not.toBeNull();
        expect(screen.getByRole('button', { name: /^PROD-0099: not in the evidence/ })).not.toBeNull();
        expect(screen.getByText('Cited but not in the evidence: PROD-0099')).not.toBeNull();
    });

    it('shows the explanation under its audience, with the prompt it used', () => {
        render(
            <AnswerTab
                stage="pedagogy"
                stream={stream}
                search={search}
                audience="novice"
                applyPedagogy={false}
            />,
        );

        expect(screen.getByRole('heading', { name: 'Explanation · for a novice' })).not.toBeNull();
        expect(screen.getByText('baseline prompt')).not.toBeNull();
        expect(screen.getByText('Answer and explanation done in 6.0 s')).not.toBeNull();
    });

    it('highlights a product in the evidence set when its citation chip is clicked', () => {
        render(
            <AnswerTab stage="pedagogy" stream={stream} search={search} audience="novice" applyPedagogy />,
        );

        fireEvent.click(screen.getByRole('button', { name: /^PROD-0014,/ }));

        // The chip's own label names the product too, so look for the row inside the evidence set.
        const evidenceSet = within(screen.getByRole('region', { name: /Evidence set/ }));
        const row = evidenceSet.getByRole('button', { name: /Voltline 45W Barrel Charger/ }).closest('li');
        expect(row?.getAttribute('aria-current')).toBe('true');
        expect(screen.getByText(/#44, included to warn/)).not.toBeNull();
    });

    it('shows the LLM’s 503 guidance, while the evidence from the results stays visible', () => {
        const failed: AnswerStream = {
            ...stream,
            status: 'error',
            sections: { answer: { markdown: '', final: null }, explanation: { markdown: '', final: null } },
            done: null,
            error: new Error('Is Ollama running?'),
        };

        render(<AnswerTab stage="rag" stream={failed} search={search} audience="novice" applyPedagogy />);

        expect(screen.getByRole('alert').textContent).toContain('Is Ollama running?');
        expect(screen.getByText('Voltline 65W USB-C GaN Charger')).not.toBeNull();
    });
});
