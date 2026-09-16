import glossaryJson from '../../content/glossary.json';
import { isPipelineStage, type PipelineStage } from './stageGroup';

// The talk's words live in content/, not in components (web-ui CLAUDE.md § Content vs code), so a stage
// explanation or a definition can be edited without touching React. Vite reads the files into the bundle at
// build time. Content can link to two things with link schemes of its own:
//   [RRF](term:rrf)                                   a glossary term, shown as a hover card
//   [ADR-0011 · Hybrid](adr:0011-hybrid-search-rrf)   a decision record, by its file name
// content.test.ts checks that every such link resolves, so a typo fails the build instead of the talk.

/** Every stage explanation has these headings, in this order (ADR-0014 § Content). */
export const explanationHeadings = [
    'What it is',
    'How it works',
    'What to look for',
    'Strength',
    'Failure mode',
    'Try this',
    'Read the decision',
] as const;

export type ExplanationHeading = (typeof explanationHeadings)[number];

export type StageExplanation = Record<ExplanationHeading, string>;

export interface GlossaryEntry {
    /** Used in `term:` links and as the glossary page anchor, e.g. "rrf". */
    id: string;
    term: string;
    acronym?: string;
    definition: string;
    topic: string;
    seeAlso?: string[];
    /** The decision record's file name without `.md`, e.g. "0011-hybrid-search-rrf". */
    adr?: string;
}

export const glossary: readonly GlossaryEntry[] = glossaryJson;

const glossaryById = new Map(glossary.map((entry) => [entry.id, entry]));

export function findGlossaryEntry(id: string): GlossaryEntry | null {
    return glossaryById.get(id) ?? null;
}

const stageFiles = import.meta.glob<string>('../../content/stages/*.md', {
    query: '?raw',
    import: 'default',
    eager: true,
});

/** Each stage explanation's markdown, by the file's name: `{ ontology: "## What it is…" }`. */
export const stageExplanationFiles: Readonly<Record<string, string>> = Object.fromEntries(
    Object.entries(stageFiles).map(([path, markdown]) => [path.replace(/^.*\/|\.md$/g, ''), markdown]),
);

/** The markdown for a stage, or `null` when it has no explanation file (the content tests require one per stage). */
export function stageExplanationMarkdown(stage: PipelineStage): string | null {
    return stageExplanationFiles[stage] ?? null;
}

export type ParsedExplanation = { sections: StageExplanation } | { error: string };

/**
 * Splits a stage file at its `## ` headings. The headings must be exactly the fixed ones, in order, so every
 * stage reads the same way and the layout knows where each section goes.
 */
export function parseStageExplanation(markdown: string): ParsedExplanation {
    const headings: string[] = [];
    const bodies: string[][] = [];

    for (const line of markdown.split('\n')) {
        const heading = /^## (.+)$/.exec(line)?.[1];

        if (heading !== undefined) {
            headings.push(heading.trim());
            bodies.push([]);
        } else if (bodies.length > 0) {
            bodies.at(-1)?.push(line);
        } else if (line.trim() !== '') {
            return {
                error: 'Text before the first heading. Every line belongs under one of the fixed headings.',
            };
        }
    }

    const expected = explanationHeadings.join(' · ');
    if (headings.join(' · ') !== expected) {
        return { error: `The headings are "${headings.join(' · ')}"; they must be "${expected}".` };
    }

    const sections = Object.fromEntries(
        explanationHeadings.map((heading, index) => [heading, (bodies[index] ?? []).join('\n').trim()]),
    ) as StageExplanation;

    return { sections };
}

/** The glossary ids a piece of markdown links to with `term:`. */
export function termLinkIds(markdown: string): string[] {
    return [...markdown.matchAll(/\]\(term:([^)\s]+)\)/g)].flatMap((match) => match[1] ?? []);
}

/** The decision records a piece of markdown links to with `adr:`. */
export function adrLinkIds(markdown: string): string[] {
    return [...markdown.matchAll(/\]\(adr:([^)\s]+)\)/g)].flatMap((match) => match[1] ?? []);
}

/** The golden-query ids a piece of markdown mentions, e.g. "GQ-01". */
export function goldenQueryIds(markdown: string): string[] {
    return [...markdown.matchAll(/\bGQ-\d{2}\b/g)].map((match) => match[0]);
}

/** True when every explanation file is named after a pipeline stage. */
export function unknownStageFiles(): string[] {
    return Object.keys(stageExplanationFiles).filter((name) => !isPipelineStage(name));
}
