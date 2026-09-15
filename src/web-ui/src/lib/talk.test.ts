import { describe, expect, it } from 'vitest';
import goldenQueriesJson from '../../../PI.SearchApi/assets/data/golden-queries.json';
import { searchStages, type GoldenQuery } from '@/api/client';
import { adrLinkIds, findGlossaryEntry, goldenQueryIds, termLinkIds } from './content';
import { findDecision } from './decisions';
import {
    nextPosition,
    positionPath,
    previousPosition,
    stepTabs,
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
        tabs: ['results', 'answer'],
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
    it("→ walks each stage step's tabs, then moves to the next step, and stops at the end", () => {
        expect(walk({ stepId: 'intro', tab: null }, nextPosition)).toEqual([
            '/talk/intro',
            '/talk/stage-keyword/how-it-works',
            '/talk/stage-keyword/results',
            '/talk/stage-keyword/under-the-hood',
            '/talk/stage-rag/results',
            '/talk/stage-rag/answer',
            '/talk/summary',
        ]);
    });

    it('← walks the same positions backwards, entering a stage step on its last tab', () => {
        expect(walk({ stepId: 'summary', tab: null }, previousPosition)).toEqual(
            walk({ stepId: 'intro', tab: null }, nextPosition).reverse(),
        );
    });

    it('moves on from a tab the step does not list, and ← returns to its first tab', () => {
        const jumpedTo: TalkPosition = { stepId: 'stage-rag', tab: 'under-the-hood' };

        expect(nextPosition(steps, jumpedTo)).toEqual({ stepId: 'summary', tab: null });
        expect(previousPosition(steps, jumpedTo)).toEqual({ stepId: 'stage-rag', tab: 'results' });
    });

    it('adds Answer after Results on Stages 6–7 by default', () => {
        expect(stepTabs({ id: 'p', kind: 'stage', title: 'P', file: 'p.md', stage: 'pedagogy' })).toEqual([
            'how-it-works',
            'results',
            'answer',
            'under-the-hood',
        ]);
    });

    it('starts the talk on its first step', () => {
        expect(talkStartPath(steps)).toBe('/talk/intro');
    });
});

describe('talkStepState', () => {
    // JSON types each query's shape separately, so the API's contract type needs a cast through unknown.
    const gq01 = goldenQueriesJson.find((query) => query.id === 'GQ-01') as unknown as GoldenQuery;

    it("fills a stage step's golden query and options", () => {
        const step: TalkStep = {
            id: 'stage-ontology',
            kind: 'stage',
            title: 'Ontology',
            file: 'talk/o.md',
            stage: 'ontology',
            goldenQuery: 'GQ-01',
            options: { applyConstraints: false },
        };

        const state = talkStepState(step, gq01);

        expect(state).toMatchObject({
            stage: 'ontology',
            tab: 'how-it-works',
            goldenQueryId: 'GQ-01',
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

    it('walks Stages 1–5 in order, each with a golden query the API serves', () => {
        const stageSteps = talkSteps.filter((step) => step.kind === 'stage');

        expect(stageSteps.map((step) => step.stage)).toEqual([...searchStages]);
        expect(stageSteps.filter((step) => !goldenQueryIdsInData.has(step.goldenQuery ?? ''))).toEqual([]);
    });

    it('lists only tabs that exist and are available on the step’s stage', () => {
        const invalid = talkSteps.flatMap((step) =>
            (step.tabs ?? []).filter(
                (tab) =>
                    step.stage === undefined ||
                    !isPipelineStage(step.stage) ||
                    !stepTabs(step).some((known) => known === tab) ||
                    !isTabAvailable(stepTabs(step).find((known) => known === tab) ?? 'answer', step.stage),
            ),
        );

        expect(invalid).toEqual([]);
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
