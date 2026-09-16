import { describe, expect, it } from 'vitest';
import goldenQueriesJson from '../../../PI.SearchApi/assets/data/golden-queries.json';
import { searchStages } from '@/api/client';
import {
    adrLinkIds,
    explanationHeadings,
    findGlossaryEntry,
    glossary,
    goldenQueryIds,
    parseStageExplanation,
    stageExplanationFiles,
    termLinkIds,
    unknownStageFiles,
} from './content';

// Content integrity (ADR-0014 § Quality bar): the talk's text is data, so these tests are its compiler.
// talk.json's checks (step files, tabs values) arrive with talk mode in roadmap Phase 3 step 10.

// The decision records on disk. Only the file names are read, not the files.
const adrFileIds = Object.keys(import.meta.glob('../../../../docs/decisions/*.md')).map((path) =>
    path.replace(/^.*\/|\.md$/g, ''),
);

// The API's own golden-query file, so content can only name queries the API serves.
const goldenQueryIdsInData = new Set(goldenQueriesJson.map((query) => query.id));

const explanations = Object.entries(stageExplanationFiles);

describe('stage explanations', () => {
    it('exist for every stage the API serves, and only for pipeline stages', () => {
        expect(Object.keys(stageExplanationFiles).sort()).toEqual(
            expect.arrayContaining([...searchStages].sort()),
        );
        expect(unknownStageFiles()).toEqual([]);
    });

    it.each(explanations)('%s.md has the fixed headings, in order, each with text', (_, markdown) => {
        const parsed = parseStageExplanation(markdown);

        expect(parsed).not.toHaveProperty('error');
        if ('sections' in parsed) {
            for (const heading of explanationHeadings) {
                expect(parsed.sections[heading], heading).not.toBe('');
            }
        }
    });

    it.each(explanations)('%s.md links only to glossary terms that exist', (_, markdown) => {
        expect(termLinkIds(markdown).filter((id) => findGlossaryEntry(id) === null)).toEqual([]);
    });

    it.each(explanations)('%s.md links only to decision records that exist', (_, markdown) => {
        expect(adrLinkIds(markdown).length).toBeGreaterThan(0);
        expect(adrLinkIds(markdown).filter((id) => !adrFileIds.includes(id))).toEqual([]);
    });

    it.each(explanations)('%s.md names only real golden queries', (_, markdown) => {
        expect(goldenQueryIds(markdown).filter((id) => !goldenQueryIdsInData.has(id))).toEqual([]);
    });
});

describe('parseStageExplanation', () => {
    it('reports headings that are missing or out of order', () => {
        const parsed = parseStageExplanation('## What it is\n\nText.\n\n## Strength\n\nText.');

        expect(parsed).toHaveProperty('error');
    });
});

describe('glossary', () => {
    it('has unique, kebab-case ids', () => {
        const ids = glossary.map((entry) => entry.id);

        expect(new Set(ids).size).toBe(ids.length);
        expect(ids.filter((id) => !/^[a-z0-9]+(-[a-z0-9]+)*$/.test(id))).toEqual([]);
    });

    it('cross-references only terms and decision records that exist', () => {
        const brokenSeeAlso = glossary.flatMap((entry) =>
            (entry.seeAlso ?? [])
                .filter((id) => findGlossaryEntry(id) === null)
                .map((id) => `${entry.id} → ${id}`),
        );
        const brokenAdr = glossary.filter(
            (entry) => entry.adr !== undefined && !adrFileIds.includes(entry.adr),
        );

        expect(brokenSeeAlso).toEqual([]);
        expect(brokenAdr.map((entry) => entry.id)).toEqual([]);
    });

    it('defines the terms ADR-0014 lists, including the going-further ones', () => {
        const required = [
            'bm25',
            'fts',
            'tsvector',
            'embedding',
            'cosine-distance',
            'hnsw',
            'rrf',
            'dense-sparse',
            'skos',
            'rag',
            'onnx',
            'chunking',
            're-ranking',
            'cross-encoder',
            'learned-sparse',
            'owl',
            'shacl',
            'knowledge-graph',
        ];

        expect(required.filter((id) => findGlossaryEntry(id) === null)).toEqual([]);
    });
});
