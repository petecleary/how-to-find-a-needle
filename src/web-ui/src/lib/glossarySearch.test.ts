import { describe, expect, it } from 'vitest';
import type { GlossaryEntry } from './content';
import { filterGlossary, groupByTopic } from './glossarySearch';

const entries: GlossaryEntry[] = [
    {
        id: 'rrf',
        term: 'Reciprocal Rank Fusion',
        acronym: 'RRF',
        definition: 'Fuses ranks.',
        topic: 'Hybrid search',
    },
    {
        id: 'skos',
        term: 'Simple Knowledge Organization System',
        acronym: 'SKOS',
        definition: 'Labels.',
        topic: 'Ontology',
    },
    { id: 'taxonomy', term: 'Taxonomy', definition: 'A tree of categories.', topic: 'Ontology' },
];

describe('filterGlossary', () => {
    it('matches the acronym, term or definition, ignoring case', () => {
        expect(filterGlossary(entries, 'rrf').map((entry) => entry.id)).toEqual(['rrf']);
        expect(filterGlossary(entries, 'TREE').map((entry) => entry.id)).toEqual(['taxonomy']);
        expect(filterGlossary(entries, '  ')).toHaveLength(3);
    });
});

describe('groupByTopic', () => {
    it('keeps topics in the order they first appear', () => {
        expect(groupByTopic(entries).map(([topic, grouped]) => [topic, grouped.length])).toEqual([
            ['Hybrid search', 1],
            ['Ontology', 2],
        ]);
    });
});
