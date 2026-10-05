import type { GoldenQuery } from '@/api/client';
import slidesJson from '../../content/slides.json';
import { serializeSearchState, type SearchState } from './searchState';
import {
    isPipelineStage,
    pipelineStages,
    stageGroup,
    stageLabel,
    stageNumber,
    triadGroups,
    type PipelineStage,
} from './stageGroup';
import { seededState, type StageSeed } from './stageSeed';

// The presenter's slide deck (ADR-0014 § Slides mode). content/slides.json lists the slides in order; each has
// a markdown file, and a slide can name a demo: the stage, golden query, options and tab the demo window should
// show while the slide is up. The deck explains a concept; the demo window, which follows it, shows it running.
//
// The position lives in the route, /slides/:slide, so a bookmark reopens the same slide and Back walks back.

/** A stage slug, or one of the talk's non-stage sections. It sets the slide's colour and its position label. */
export type SlideSection = PipelineStage | 'intro' | 'needle' | 'going-further' | 'summary';

const otherSections = ['intro', 'needle', 'going-further', 'summary'] as const;

/** `title` is the deck's opening slide: the talk title in Dosis, beside the speaker card. `speaker` adds the speaker card. */
export type SlideLayout = 'title' | 'speaker';

export interface Slide {
    id: string;
    section: string;
    title: string;
    /** Path under content/, e.g. "slides/hybrid-rrf.md". */
    file: string;
    layout?: SlideLayout;
    /** What the demo window shows for this slide. A slide without one leaves the demo where it is. */
    demo?: StageSeed;
}

// JSON types strings loosely, so the sections and stages are checked at runtime by slides.test.ts instead.
export const slides: readonly Slide[] = slidesJson as Slide[];

const slideFiles = import.meta.glob<string>('../../content/slides/*.md', {
    query: '?raw',
    import: 'default',
    eager: true,
});

/** Each slide file's markdown, by its path under content/: `{ "slides/title.md": "A practical guide…" }`. */
export const slideFileMarkdown: Readonly<Record<string, string>> = Object.fromEntries(
    Object.entries(slideFiles).map(([path, markdown]) => [path.replace(/^.*\/content\//, ''), markdown]),
);

export function slideMarkdown(slide: Slide): string | null {
    return slideFileMarkdown[slide.file] ?? null;
}

export function isSlideSection(value: string): value is SlideSection {
    return isPipelineStage(value) || otherSections.some((section) => section === value);
}

export function findSlide(id: string | undefined, deck: readonly Slide[] = slides): Slide | null {
    return deck.find((slide) => slide.id === id) ?? null;
}

/** The slide after this one, or `null` at the end of the deck (or for an id that isn't in it). */
export function nextSlide(slideId: string, deck: readonly Slide[] = slides): Slide | null {
    return slideAt(deck, slideId, 1);
}

/** The slide before this one, or `null` at the start of the deck. */
export function previousSlide(slideId: string, deck: readonly Slide[] = slides): Slide | null {
    return slideAt(deck, slideId, -1);
}

function slideAt(deck: readonly Slide[], slideId: string, offset: number): Slide | null {
    const index = deck.findIndex((slide) => slide.id === slideId);
    return index === -1 ? null : (deck[index + offset] ?? null);
}

/**
 * True on the last slide of a stage: the concept has been explained and the next slide moves on, so this is
 * the moment to switch to the demo. The deck highlights its *Open demo* button here.
 */
export function isLastSlideOfStage(slide: Slide, deck: readonly Slide[] = slides): boolean {
    if (!isPipelineStage(slide.section)) {
        return false;
    }

    return nextSlide(slide.id, deck)?.section !== slide.section;
}

/** The route for a slide. */
export function slidePath(slideId: string): string {
    return `/slides/${encodeURIComponent(slideId)}`;
}

/** Where *Present the slides* goes. */
export function slidesStartPath(deck: readonly Slide[] = slides): string {
    const first = deck[0];
    return first === undefined ? '/' : slidePath(first.id);
}

/** Where the slide sits in the talk, e.g. "Stage 5 of 7 · Ontology", or "The needle" for a non-stage slide. */
export function slideSectionLabel(slide: Slide): string {
    if (isPipelineStage(slide.section)) {
        return `Stage ${stageNumber(slide.section)} of ${pipelineStages.length} · ${stageLabel(slide.section)}`;
    }

    const labels: Record<string, string> = {
        intro: 'Introduction',
        needle: 'The needle',
        'going-further': 'Going further',
        summary: 'Summary',
    };
    return labels[slide.section] ?? slide.title;
}

export interface SlideHeading {
    title: string;
    /** The slide's own title, beneath the stage it belongs to; `null` on a slide outside the stages. */
    subtitle: string | null;
}

/**
 * What the slide's heading says. A stage slide calls out its stage and the triad question it answers, e.g.
 * "Stage 1 Search: Structured", with the slide's own title ("Filters are exact") beneath. The stage name is
 * dropped where it repeats the group: "Stage 5 Ontology", not "Stage 5 Ontology: Ontology".
 */
export function slideHeading(slide: Slide): SlideHeading {
    if (!isPipelineStage(slide.section)) {
        return { title: slide.title, subtitle: null };
    }

    const stage = slide.section;
    const group = triadGroups.find((info) => info.group === stageGroup(stage))?.label ?? '';
    const name = stageLabel(stage);
    const stagePart = `Stage ${stageNumber(stage)} ${group}`;

    return { title: name === group ? stagePart : `${stagePart}: ${name}`, subtitle: slide.title };
}

/** The demo window's state for a slide, or `null` when the slide names no demo. */
export function slideDemoState(slide: Slide, preset: GoldenQuery | null): SearchState | null {
    return slide.demo === undefined ? null : seededState(slide.demo, preset);
}

/**
 * The demo URL for a slide: its demo state, plus `follow=1` so the window keeps following the deck, and the
 * slide's id so the window can say which slide it is following. A slide with no demo opens the plain demo.
 */
export function slideDemoPath(slide: Slide, preset: GoldenQuery | null): string {
    const state = slideDemoState(slide, preset);
    const params = state === null ? new URLSearchParams() : serializeSearchState(state);

    params.set(followParam, '1');
    params.set(followedSlideParam, slide.id);
    return `/demo?${params.toString()}`;
}

/** The query-string names the demo reads once, when the deck opens it. They're not part of the search state. */
export const followParam = 'follow';
export const followedSlideParam = 'slide';

/** The golden query a slide's demo names, from those the API served. */
export function slidePreset(slide: Slide, goldenQueries: readonly GoldenQuery[] | null): GoldenQuery | null {
    const id = slide.demo?.goldenQuery;
    return id === undefined ? null : (goldenQueries?.find((query) => query.id === id) ?? null);
}
