import type { TraceStep } from '@/api/client';

// Which purpose-built view renders a trace step (ADR-0014 § Under the hood). Steps are recognised by their
// `details` keys, not their titles: a title is prose for people, while the keys are what each stage writes
// into the contract (ADR-0003). A step no rule recognises falls back to pretty-printed JSON, so a new kind
// of step is still visible before it has its own view.

export type TraceStepKind =
    | 'sql'
    | 'tsquery'
    | 'embedding'
    | 'distances'
    | 'rrf'
    | 'concept-matches'
    | 'expansion'
    | 'classification'
    | 'rule-checks'
    | 'evidence'
    | 'prompt'
    | 'generation'
    | 'validation'
    | 'json';

// Checked in order; the first rule whose keys are all present wins. Keyword and Understand both write
// `matches`, so each is recognised by a key only it writes (`tsquery`, `tokens`). Stage 7's validation step
// writes `checks` like none of Stage 5's steps do, alongside `citations`.
const rules: readonly { kind: TraceStepKind; keys: readonly string[] }[] = [
    { kind: 'evidence', keys: ['evidence', 'limits'] },
    { kind: 'prompt', keys: ['systemPrompt', 'userPrompt'] },
    { kind: 'generation', keys: ['rawOutput'] },
    { kind: 'validation', keys: ['citations', 'checks'] },
    { kind: 'rule-checks', keys: ['applyConstraints'] },
    { kind: 'classification', keys: ['classifications'] },
    { kind: 'expansion', keys: ['expandSynonyms', 'embeddingText'] },
    { kind: 'concept-matches', keys: ['tokens', 'matches'] },
    { kind: 'rrf', keys: ['formulas'] },
    { kind: 'distances', keys: ['distances'] },
    { kind: 'embedding', keys: ['embeddedText', 'firstDimensions'] },
    { kind: 'tsquery', keys: ['tsquery'] },
];

export function traceStepKind(step: Pick<TraceStep, 'details' | 'sql'>): TraceStepKind {
    const details = step.details ?? {};
    const rule = rules.find(({ keys }) => keys.every((key) => Object.hasOwn(details, key)));

    if (rule !== undefined) {
        return rule.kind;
    }

    // Stage 1's step is its SQL: the only details are row counts and the count query.
    return step.sql ? 'sql' : 'json';
}

const shortLabels: Record<Exclude<TraceStepKind, 'json'>, string> = {
    sql: 'SQL filters',
    tsquery: 'Keyword',
    embedding: 'Embed',
    distances: 'Vector',
    rrf: 'RRF',
    'concept-matches': 'Understand',
    expansion: 'Expand',
    classification: 'Classify',
    'rule-checks': 'Constrain',
    evidence: 'Evidence',
    prompt: 'Prompt',
    generation: 'Generate',
    validation: 'Validate',
};

/**
 * A step's name for its chip in the trace flow, e.g. "RRF"; the full title is shown when it's selected. Stage 7 runs
 * prompt, generate and validate twice, so the explanation's chips say so.
 */
export function traceStepShortLabel(step: Pick<TraceStep, 'details' | 'sql' | 'title'>): string {
    const kind = traceStepKind(step);
    if (kind === 'json') {
        return step.title;
    }

    const isExplanation = step.details?.section === 'explanation';
    return isExplanation ? `Explain: ${shortLabels[kind].toLowerCase()}` : shortLabels[kind];
}
