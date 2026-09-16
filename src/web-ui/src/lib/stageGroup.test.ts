import { describe, expect, it } from 'vitest';
import {
    pipelineStages,
    stageColourClasses,
    stageGroup,
    stageNumber,
    triadGroupClasses,
    type TriadGroup,
} from './stageGroup';

describe('stageGroup', () => {
    it('puts Stages 1–4 in Search, Stage 5 in Ontology and Stages 6–7 in Pedagogy', () => {
        const groups = pipelineStages.map((stage) => stageGroup(stage));

        expect(groups).toEqual(['search', 'search', 'search', 'search', 'ontology', 'pedagogy', 'pedagogy']);
    });

    it('puts RAG with Pedagogy, not Search', () => {
        expect(stageGroup('rag')).toBe('pedagogy');
    });

    it('numbers stages 1–7 in talk order', () => {
        expect(stageNumber('structured')).toBe(1);
        expect(stageNumber('ontology')).toBe(5);
        expect(stageNumber('pedagogy')).toBe(7);
    });
});

describe('stage colour classes', () => {
    const groups: TriadGroup[] = ['search', 'ontology', 'pedagogy'];

    it('gives every group its own fill, ink and tint', () => {
        const fills = groups.map((group) => triadGroupClasses(group).fill);

        expect(new Set(fills).size).toBe(groups.length);
    });

    it('uses white text only on the purple Search fill', () => {
        expect(triadGroupClasses('search').onFill).toBe('text-on-search');
        expect(triadGroupClasses('ontology').onFill).toBe('text-on-ontology');
        expect(triadGroupClasses('pedagogy').onFill).toBe('text-on-pedagogy');
    });

    it('colours a stage by its group', () => {
        expect(stageColourClasses('hybrid')).toEqual(triadGroupClasses('search'));
        expect(stageColourClasses('rag')).toEqual(triadGroupClasses('pedagogy'));
    });
});
