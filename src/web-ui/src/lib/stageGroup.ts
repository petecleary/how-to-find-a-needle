// stageGroup — which part of the talk's triad a stage belongs to, and the colour classes for it.
//
// The talk asks three questions in order: Search (what is relevant?), Ontology (how is it related
// and constrained?) and Pedagogy (how should I explain it?). Every stage is coloured by its group
// in the stepper, tabs and trace chips, so the audience always sees where in the argument they are.
// Decision: docs/decisions/0014-web-ui-architecture.md#visual-design

/** The seven stage slugs in talk order. They match the API routes: `POST /api/search/{stage}`. */
export const pipelineStages = [
    'structured',
    'keyword',
    'vector',
    'hybrid',
    'ontology',
    'rag',
    'pedagogy',
] as const;

export type PipelineStage = (typeof pipelineStages)[number];

export type TriadGroup = 'search' | 'ontology' | 'pedagogy';

const groupByStage: Record<PipelineStage, TriadGroup> = {
    structured: 'search',
    keyword: 'search',
    vector: 'search',
    hybrid: 'search',
    ontology: 'ontology',
    // RAG retrieves nothing new: it decides *what to say* from the Stage 5 results, and Stage 7
    // decides *how to say it*. Both are about explanation, so both sit with Pedagogy.
    rag: 'pedagogy',
    pedagogy: 'pedagogy',
};

/** The triad group a stage belongs to. Use this instead of hard-coding a stage's colour. */
export function stageGroup(stage: PipelineStage): TriadGroup {
    return groupByStage[stage];
}

/** The stage's number in the talk, 1–7. */
export function stageNumber(stage: PipelineStage): number {
    return pipelineStages.indexOf(stage) + 1;
}

/** Tailwind classes for one triad group's colour tokens (defined in `src/index.css`). */
export interface TriadGroupClasses {
    /** Solid background, for stage number circles and selected tabs. */
    fill: string;
    /** Text on the solid fill: white on purple, dark on green and orange (for contrast). */
    onFill: string;
    /** Coloured text on the page background. */
    ink: string;
    /** Quiet background, for chips and highlighted rows. */
    tint: string;
    border: string;
}

// Tailwind only generates a class it finds written out in full in the source, so each name is a
// complete literal here rather than built with a template string like `bg-${group}`.
const classesByGroup: Record<TriadGroup, TriadGroupClasses> = {
    search: {
        fill: 'bg-search',
        onFill: 'text-on-search',
        ink: 'text-search-ink',
        tint: 'bg-search-tint',
        border: 'border-search',
    },
    ontology: {
        fill: 'bg-ontology',
        onFill: 'text-on-ontology',
        ink: 'text-ontology-ink',
        tint: 'bg-ontology-tint',
        border: 'border-ontology',
    },
    pedagogy: {
        fill: 'bg-pedagogy',
        onFill: 'text-on-pedagogy',
        ink: 'text-pedagogy-ink',
        tint: 'bg-pedagogy-tint',
        border: 'border-pedagogy',
    },
};

/** The colour classes for a triad group. */
export function triadGroupClasses(group: TriadGroup): TriadGroupClasses {
    return classesByGroup[group];
}

/** The colour classes for a stage, via its triad group. */
export function stageColourClasses(stage: PipelineStage): TriadGroupClasses {
    return classesByGroup[stageGroup(stage)];
}

/** True for one of the seven stage slugs, e.g. a trace step's `stage`. */
export function isPipelineStage(value: string): value is PipelineStage {
    return pipelineStages.some((stage) => stage === value);
}

const stageLabels: Record<PipelineStage, string> = {
    structured: 'Structured',
    keyword: 'Keyword',
    vector: 'Vector',
    hybrid: 'Hybrid',
    ontology: 'Ontology',
    rag: 'RAG',
    pedagogy: 'Pedagogy',
};

/** The stage's name as the talk says it, e.g. "Hybrid" or "RAG". */
export function stageLabel(stage: PipelineStage): string {
    return stageLabels[stage];
}

export interface TriadGroupInfo {
    group: TriadGroup;
    label: string;
    /** The question this part of the pipeline answers, shown above its stages. */
    question: string;
}

/** The three groups in talk order. */
export const triadGroups: readonly TriadGroupInfo[] = [
    { group: 'search', label: 'Search', question: 'what is relevant?' },
    { group: 'ontology', label: 'Ontology', question: 'how is it related?' },
    { group: 'pedagogy', label: 'Pedagogy', question: 'how should I explain it?' },
];
