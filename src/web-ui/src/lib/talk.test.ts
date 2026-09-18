import { describe, expect, it } from 'vitest';
import goldenQueriesJson from '../../../PI.SearchApi/assets/data/golden-queries.json';
import { searchStages, type GoldenQuery } from '@/api/client';
import { adrLinkIds, findGlossaryEntry, goldenQueryIds, termLinkIds } from './content';
import { findDecision } from './decisions';
import {
    isStageTab,
    nextPosition,
    positionPath,
    previousPosition,
    stepTab,
    talkFileMarkdown,
    talkStartPath,
    talkStepState,
    talkSteps,
    type TalkPosition,
    type TalkStep,
} from './talk';
import { isTabAvailable } from './stageTabs';
import { isPipelineStage } from './stageGroup';

const steps: TalkStep[] = [
    { id: 'intro', kind: 'intro', title: 'Intro', file: 'talk/intro.md' },
    { id: 'stage-keyword', kind: 'stage', title: 'Keyword', file: 'talk/k.md', stage: 'keyword' },
    {
        id: 'stage-rag',
        kind: 'stage',
        title: 'RAG',
        file: 'talk/r.md',
        stage: 'rag',
        tab: 'answer',
    },
    { id: 'summary', kind: 'summary', title: 'Summary', file: 'talk/summary.md' },
];

function walk(from: TalkPosition, step: typeof nextPosition): string[] {
    const paths = [positionPath(from)];
    for (let position = step(steps, from); position !== null; position = step(steps, position)) {
        paths.push(positionPath(position));
    }
    return paths;
}

describe('talk navigation', () => {
    it('→ visits each step once, landing on its tab, and stops at the end', () => {
        expect(walk({ stepId: 'intro', tab: null }, nextPosition)).toEqual([
            '/talk/intro',
            '/talk/stage-keyword/how-it-works',
            '/talk/stage-rag/answer',
            '/talk/summary',
        ]);
    });

    it('← walks exactly the same positions backwards', () => {
        expect(walk({ stepId: 'summary', tab: null }, previousPosition)).toEqual(
            walk({ stepId: 'intro', tab: null }, nextPosition).reverse(),
        );
    });

    it('goes to the next step from whichever tab the presenter jumped to', () => {
        const jumpedTo: TalkPosition = { stepId: 'stage-rag', tab: 'going-further' };

        expect(nextPosition(steps, jumpedTo)).toEqual({ stepId: 'summary', tab: null });
        expect(previousPosition(steps, jumpedTo)).toEqual({ stepId: 'stage-keyword', tab: 'how-it-works' });
    });

    it('lands on How it works unless the step names another tab', () => {
        expect(stepTab({ id: 'k', kind: 'stage', title: 'K', file: 'k.md', stage: 'keyword' })).toBe(
            'how-it-works',
        );
        expect(stepTab({ id: 'p', kind: 'stage', title: 'P', file: 'p.md', stage: 'pedagogy' })).toBe(
            'how-it-works',
        );
    });

    it('falls back when a step names a tab its stage does not have', () => {
        const step: TalkStep = {
            id: 's',
            kind: 'stage',
            title: 'S',
            file: 's.md',
            stage: 'structured',
            tab: 'going-further',
        };

        expect(stepTab(step)).toBe('how-it-works');
    });

    it('gives a content step no tab', () => {
        expect(stepTab({ id: 'intro', kind: 'intro', title: 'Intro', file: 'talk/intro.md' })).toBeNull();
    });

    it('starts the talk on its first step', () => {
        expect(talkStartPath(steps)).toBe('/talk/intro');
    });
});

describe('talkStepState', () => {
    // JSON types each query's shape separately, so the API's contract type needs a cast through unknown.
    const gq03 = goldenQueriesJson.find((query) => query.id === 'GQ-03') as unknown as GoldenQuery;

    it("fills a stage step's golden query and options", () => {
        const step: TalkStep = {
            id: 'stage-ontology',
            kind: 'stage',
            title: 'Ontology',
            file: 'talk/o.md',
            stage: 'ontology',
            goldenQuery: 'GQ-03',
            options: { applyConstraints: false },
        };

        const state = talkStepState(step, gq03);

        expect(state).toMatchObject({
            stage: 'ontology',
            tab: 'how-it-works',
            goldenQueryId: 'GQ-03',
            query: 'power adapter for my laptop',
            targetProductId: 'PROD-0001',
            applyConstraints: false,
            expandSynonyms: true,
        });
    });
});

// Content integrity for talk.json (ADR-0014 § Quality bar).
describe('content/talk.json', () => {
    const goldenQueryIdsInData = new Set(goldenQueriesJson.map((query) => query.id));

    it('has unique step ids and known kinds', () => {
        const ids = talkSteps.map((step) => step.id);

        expect(new Set(ids).size).toBe(ids.length);
        expect(talkSteps.filter((step) => !['intro', 'stage', 'summary'].includes(step.kind))).toEqual([]);
    });

    it('names a file that exists for every step', () => {
        expect(
            talkSteps.filter((step) => talkFileMarkdown[step.file] === undefined).map((step) => step.file),
        ).toEqual([]);
    });

    it('walks Stages 1–7 in order, one step each, with a golden query the API serves', () => {
        const stageSteps = talkSteps.filter((step) => step.kind === 'stage');

        expect(stageSteps.map((step) => step.stage)).toEqual([...searchStages]);
        expect(stageSteps.filter((step) => !goldenQueryIdsInData.has(step.goldenQuery ?? ''))).toEqual([]);
    });

    it('names only tabs that exist and are available on the step’s stage', () => {
        const invalid = talkSteps.filter(
            (step) =>
                step.tab !== undefined &&
                (!isStageTab(step.tab) ||
                    step.stage === undefined ||
                    !isPipelineStage(step.stage) ||
                    !isTabAvailable(step.tab, step.stage)),
        );

        expect(invalid.map((step) => step.id)).toEqual([]);
    });

    it('opens the Summary after Going further, as ADR-0018 orders them', () => {
        expect(talkSteps.slice(-2).map((step) => step.id)).toEqual(['going-further', 'summary']);
    });
});

describe('talk files', () => {
    const files = Object.entries(talkFileMarkdown);

    it.each(files)(
        '%s links only to glossary terms, decisions and golden queries that exist',
        (_, markdown) => {
            expect(termLinkIds(markdown).filter((id) => findGlossaryEntry(id) === null)).toEqual([]);
            expect(adrLinkIds(markdown).filter((id) => findDecision(id) === null)).toEqual([]);
            expect(
                goldenQueryIds(markdown).filter((id) => !goldenQueriesJson.some((query) => query.id === id)),
            ).toEqual([]);
        },
    );
});
