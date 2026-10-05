import { describe, expect, it } from 'vitest';
import goldenQueriesJson from '../../../PI.SearchApi/assets/data/golden-queries.json';
import type { GoldenQuery } from '@/api/client';
import { adrLinkIds, findGlossaryEntry, goldenQueryIds, termLinkIds } from './content';
import { findDecision } from './decisions';
import { parseSearchState } from './searchState';
import {
    findSlide,
    isLastSlideOfStage,
    isSlideSection,
    nextSlide,
    previousSlide,
    slideDemoPath,
    slideFileMarkdown,
    slideHeading,
    slides,
    slideSectionLabel,
    slidesStartPath,
    type Slide,
} from './slides';
import { isTabAvailable } from './stageTabs';
import { isPipelineStage } from './stageGroup';
import { isStageTab } from './stageSeed';

// JSON types each query's shape separately, so the API's contract type needs a cast through unknown.
const goldenQueries = goldenQueriesJson as unknown as GoldenQuery[];
const gq03 = goldenQueries.find((query) => query.id === 'GQ-03') ?? null;
const goldenQueryIdsInData = new Set(goldenQueries.map((query) => query.id));

const titleSlide: Slide = {
    id: 'title',
    section: 'intro',
    title: 'Title',
    file: 'slides/title.md',
    layout: 'title',
};
const vectorSlide: Slide = {
    id: 'vector-near-miss',
    section: 'vector',
    title: 'Near miss',
    file: 'slides/v.md',
    demo: { stage: 'vector', goldenQuery: 'GQ-03', tab: 'results' },
};
const summarySlide: Slide = {
    id: 'summary',
    section: 'summary',
    title: 'Summary',
    file: 'slides/summary.md',
};
const deck: Slide[] = [titleSlide, vectorSlide, summarySlide];

describe('slide navigation', () => {
    it('→ and ← move one slide and stop at the ends', () => {
        expect(nextSlide('title', deck)?.id).toBe('vector-near-miss');
        expect(nextSlide('summary', deck)).toBeNull();
        expect(previousSlide('vector-near-miss', deck)?.id).toBe('title');
        expect(previousSlide('title', deck)).toBeNull();
    });

    it('goes nowhere from a slide that is not in the deck', () => {
        expect(nextSlide('missing', deck)).toBeNull();
        expect(findSlide('missing', deck)).toBeNull();
    });

    it('starts the deck on its first slide', () => {
        expect(slidesStartPath(deck)).toBe('/slides/title');
    });

    it('labels a stage slide with its stage, and other slides with their section', () => {
        expect(slideSectionLabel(vectorSlide)).toBe('Stage 3 of 7 · Vector');
        expect(slideSectionLabel(summarySlide)).toBe('Summary');
    });
});

describe('isLastSlideOfStage', () => {
    const secondVectorSlide: Slide = { ...vectorSlide, id: 'vector-second' };
    const stageDeck: Slide[] = [titleSlide, vectorSlide, secondVectorSlide, summarySlide];

    it('is true only on the last slide of a stage', () => {
        expect(isLastSlideOfStage(vectorSlide, stageDeck)).toBe(false);
        expect(isLastSlideOfStage(secondVectorSlide, stageDeck)).toBe(true);
    });

    it('is false on a slide outside the stages', () => {
        expect(isLastSlideOfStage(titleSlide, stageDeck)).toBe(false);
        expect(isLastSlideOfStage(summarySlide, stageDeck)).toBe(false);
    });
});

describe('slideHeading', () => {
    it("calls out a stage slide's stage and triad group, with the slide title beneath", () => {
        expect(slideHeading(vectorSlide)).toEqual({ title: 'Stage 3 Search: Vector', subtitle: 'Near miss' });
        expect(slideHeading({ ...vectorSlide, section: 'rag' }).title).toBe('Stage 6 Pedagogy: RAG');
    });

    it('drops the stage name where it repeats the group', () => {
        expect(slideHeading({ ...vectorSlide, section: 'ontology' }).title).toBe('Stage 5 Ontology');
        expect(slideHeading({ ...vectorSlide, section: 'pedagogy' }).title).toBe('Stage 7 Pedagogy');
    });

    it("keeps a non-stage slide's title as it is", () => {
        expect(slideHeading(summarySlide)).toEqual({ title: 'Summary', subtitle: null });
    });
});

describe('slideDemoPath', () => {
    it("opens the demo on the slide's stage, tab and golden query, following the deck", () => {
        const path = slideDemoPath(vectorSlide, gq03);
        const params = new URLSearchParams(path.slice('/demo?'.length));

        expect(path.startsWith('/demo?')).toBe(true);
        expect(params.get('follow')).toBe('1');
        expect(params.get('slide')).toBe('vector-near-miss');
        expect(parseSearchState(params)).toMatchObject({
            stage: 'vector',
            tab: 'results',
            goldenQueryId: 'GQ-03',
            query: 'power adapter for my laptop',
            targetProductId: 'PROD-0001',
        });
    });

    it('opens the plain demo for a slide with no demo, still following', () => {
        expect(slideDemoPath(titleSlide, null)).toBe('/demo?follow=1&slide=title');
    });
});

// Content integrity for slides.json (ADR-0014 § Slides mode).
describe('content/slides.json', () => {
    it('has unique slide ids, known sections and files that exist', () => {
        const ids = slides.map((slide) => slide.id);

        expect(new Set(ids).size).toBe(ids.length);
        expect(slides.filter((slide) => !isSlideSection(slide.section)).map((slide) => slide.id)).toEqual([]);
        expect(
            slides.filter((slide) => slideFileMarkdown[slide.file] === undefined).map((slide) => slide.file),
        ).toEqual([]);
    });

    // RAG gets one slide: by Stage 6 the audience knows retrieval, and RAG adds one step, the prompt.
    it('covers every stage, in order, with several slides each for Stages 3, 4, 5 and 7', () => {
        const stageSections = slides.map((slide) => slide.section).filter(isPipelineStage);
        const inOrder = stageSections.filter((section, index) => section !== stageSections[index - 1]);

        expect(inOrder).toEqual(['structured', 'keyword', 'vector', 'hybrid', 'ontology', 'rag', 'pedagogy']);
        for (const stage of ['vector', 'hybrid', 'ontology', 'pedagogy']) {
            expect(stageSections.filter((section) => section === stage).length).toBeGreaterThanOrEqual(2);
        }
    });

    it('names only demos that exist: a stage, a golden query the API serves and an available tab', () => {
        const invalid = slides.filter((slide) => {
            const demo = slide.demo;
            if (demo === undefined) return false;

            const hasStage = demo.stage !== undefined && isPipelineStage(demo.stage);
            const hasQuery = demo.goldenQuery === undefined || goldenQueryIdsInData.has(demo.goldenQuery);
            const hasTab =
                demo.tab === undefined ||
                (isStageTab(demo.tab) &&
                    demo.stage !== undefined &&
                    isPipelineStage(demo.stage) &&
                    isTabAvailable(demo.tab, demo.stage));

            return !(hasStage && hasQuery && hasTab);
        });

        expect(invalid.map((slide) => slide.id)).toEqual([]);
    });

    it("demos a stage slide's own stage", () => {
        const mismatched = slides.filter(
            (slide) => isPipelineStage(slide.section) && slide.demo?.stage !== slide.section,
        );

        expect(mismatched.map((slide) => slide.id)).toEqual([]);
    });
});

// A slide is read from the back of the room while the speaker talks, so it carries a few short lines, not
// paragraphs. The detail lives in the stage explanations and the ADRs.
const maxBullets = 5;
// Sub-bullets add detail under a bullet, but the whole slide still stops at ten lines.
const maxLines = maxBullets * 2;
const maxWordsPerLine = 14;
const maxTitleWords = 8;

/** The visible words in a line of markdown: link targets and emphasis markers don't count. */
function wordsIn(line: string): number {
    const text = line
        .replace(/\[([^\]]*)\]\([^)]*\)/g, '$1')
        .replace(/^\s*(?:[-*]|\d+\.)\s+/, '')
        .replace(/[*_`]/g, '');
    return text.split(/\s+/).filter((word) => /\w/.test(word)).length;
}

/** The lines outside code blocks: a code block holds one formula, which is read as a picture, not as text. */
function proseLines(markdown: string): string[] {
    const withoutCode = markdown.replace(/```[\s\S]*?```/g, '');
    return withoutCode.split('\n').filter((line) => line.trim() !== '');
}

describe('slide readability', () => {
    it.each(slides.map((slide) => [slide.id, slide] as const))(
        '%s is short enough to read at a glance',
        (_, slide) => {
            const markdown = slideFileMarkdown[slide.file] ?? '';
            const lines = proseLines(markdown);
            // Top-level bullets only: a sub-bullet adds detail to its bullet rather than another point.
            const bullets = lines.filter((line) => /^(?:[-*]|\d+\.)\s/.test(line));

            expect(wordsIn(slide.title)).toBeLessThanOrEqual(maxTitleWords);
            expect(bullets.length).toBeLessThanOrEqual(maxBullets);
            expect(lines.length).toBeLessThanOrEqual(maxLines);
            expect(lines.filter((line) => wordsIn(line) > maxWordsPerLine)).toEqual([]);
        },
    );
});

describe('slide files', () => {
    it.each(Object.entries(slideFileMarkdown))(
        '%s links only to glossary terms, decisions and golden queries that exist',
        (_, markdown) => {
            expect(termLinkIds(markdown).filter((id) => findGlossaryEntry(id) === null)).toEqual([]);
            expect(adrLinkIds(markdown).filter((id) => findDecision(id) === null)).toEqual([]);
            expect(goldenQueryIds(markdown).filter((id) => !goldenQueryIdsInData.has(id))).toEqual([]);
        },
    );
});
