import { describe, expect, it } from 'vitest';
import traceSteps from '@/test/fixtures/golden-query-trace-steps.json';
import { traceStepKind, traceStepShortLabel, type TraceStepKind } from './traceStepKind';

// golden-query-trace-steps.json lists every trace step (stage, title, whether it has SQL, its details keys)
// that GQ-01 to GQ-08 produce on Stages 1–5, plus GQ-01 on Stage 5 with each switch off. It was captured from
// a running AppHost. GQ-04 has no query, so only Stage 1 answers it (Stages 2–5 return 400).

const expectedKinds: [title: RegExp, kind: TraceStepKind][] = [
    [/^Parameterised SQL filters/, 'sql'],
    [/^Postgres full-text search/, 'tsquery'],
    [/^Query embedding/, 'embedding'],
    [/^pgvector cosine distance/, 'distances'],
    [/^Reciprocal Rank Fusion/, 'rrf'],
    [/^Understand:/, 'concept-matches'],
    [/^Expand:/, 'expansion'],
    [/^Classify:/, 'classification'],
    [/^Constrain:/, 'rule-checks'],
];

describe('traceStepKind', () => {
    it('gives every Stage 1–5 trace step for GQ-01 to GQ-08 a purpose-built view (no JSON fallback)', () => {
        const mismatches = traceSteps.flatMap(({ response, steps }) =>
            steps.flatMap((step) => {
                const details = Object.fromEntries(step.detailKeys.map((key) => [key, null]));
                const kind = traceStepKind({ details, sql: step.hasSql ? 'SELECT …' : null });
                const expected = expectedKinds.find(([title]) => title.test(step.title))?.[1] ?? 'json';

                return kind === expected && kind !== 'json' ? [] : [`${response}: "${step.title}" → ${kind}`];
            }),
        );

        expect(traceSteps.length).toBeGreaterThanOrEqual(38);
        expect(mismatches).toEqual([]);
    });

    // The keys Stages 6–7 write (RagSearch.cs, AnswerGenerator.cs, PedagogyEngine.cs).
    it.each([
        [{ evidence: [], limits: {}, concepts: [], rules: [] }, 'evidence', 'Evidence'],
        [
            { section: 'answer', promptFiles: [], systemPrompt: '', userPrompt: '', llm: null },
            'prompt',
            'Prompt',
        ],
        [{ section: 'answer', llm: null, rawOutput: '', timeToFirstTokenMs: 56 }, 'generation', 'Generate'],
        [
            { section: 'answer', citations: [], invalidCitations: [], checks: [], warnings: [] },
            'validation',
            'Validate',
        ],
        [
            {
                section: 'explanation',
                applyPedagogy: true,
                audience: 'novice',
                systemPrompt: '',
                userPrompt: '',
            },
            'prompt',
            'Explain: prompt',
        ],
        [
            { section: 'explanation', applyPedagogy: false, citations: [], checks: [], structure: null },
            'validation',
            'Explain: validate',
        ],
    ] as const)('recognises a Stage 6–7 step with keys %j as %s', (details, kind, label) => {
        expect(traceStepKind({ details, sql: null })).toBe(kind);
        expect(traceStepShortLabel({ details, sql: null, title: '' })).toBe(label);
    });

    it('falls back to JSON for a step it does not recognise', () => {
        expect(traceStepKind({ details: { somethingNew: 1 }, sql: null })).toBe('json');
    });

    it('labels chips by kind, and unknown steps by their title', () => {
        expect(
            traceStepShortLabel({
                details: { formulas: [] },
                sql: null,
                title: 'Reciprocal Rank Fusion (k=60)',
            }),
        ).toBe('RRF');
        expect(traceStepShortLabel({ details: {}, sql: null, title: 'Prompt' })).toBe('Prompt');
    });
});
