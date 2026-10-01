import { MonitorPlay } from 'lucide-react';
import { useEffect } from 'react';
import { Link, Navigate, useParams } from 'react-router';
import { getGoldenQueries } from '@/api/client';
import { Logo } from '@/components/Logo';
import { Markdown } from '@/components/Markdown';
import { SpeakerCard } from '@/components/SpeakerCard';
import { TalkControls } from '@/components/TalkControls';
import { ThemeToggle } from '@/components/ThemeToggle';
import { useApiData } from '@/hooks/useApiData';
import { useBroadcastSlide } from '@/hooks/useBroadcastSlide';
import { useTalkKeys } from '@/hooks/useTalkKeys';
import { isTypingTarget } from '@/lib/keyboard';
import {
    findSlide,
    isLastSlideOfStage,
    nextSlide,
    previousSlide,
    slideDemoPath,
    slideHeading,
    slideMarkdown,
    slidePath,
    slidePreset,
    slides,
    slideSectionLabel,
    slidesStartPath,
    type Slide,
} from '@/lib/slides';
import { speaker } from '@/lib/speakerProfile';
import { isPipelineStage, stageColourClasses, stageGroup, type TriadGroup } from '@/lib/stageGroup';
import { cn } from '@/lib/utils';

/** The demo opens in one named window, so pressing D again reuses it instead of opening another. */
const demoWindowName = 'needle-demo';

// Sets --step-colour, which Markdown's numbered circles read. Written out in full so Tailwind generates each.
const stepColourByGroup: Record<TriadGroup, string> = {
    search: '[--step-colour:var(--search-ink)]',
    ontology: '[--step-colour:var(--ontology-ink)]',
    pedagogy: '[--step-colour:var(--pedagogy-ink)]',
};

/**
 * `/slides/:slide`: the presenter's deck (ADR-0014 § Slides mode). One simple slide per position, readable from
 * the back of the room. ← / → (and a clicker's Page Up / Page Down) move one slide; D opens the demo window for
 * the current slide, and every slide change is broadcast so that window follows.
 */
export function SlidesPage() {
    const { slide: slideId } = useParams();
    const goldenQueries = useApiData(getGoldenQueries);

    const slide = findSlide(slideId);
    const next = slide === null ? null : nextSlide(slide.id);
    const previous = slide === null ? null : previousSlide(slide.id);

    useTalkKeys(next === null ? null : slidePath(next.id), previous === null ? null : slidePath(previous.id));
    useBroadcastSlide(slide?.id ?? null);

    // The golden query's preset fills the demo's query and device. Until the API has answered, the demo opens
    // on the right stage and tab with empty inputs, and the next slide change fills them.
    const demoPath = slide === null ? null : slideDemoPath(slide, slidePreset(slide, goldenQueries.data));
    useDemoKey(demoPath);

    if (slide === null) {
        return <Navigate to={slidesStartPath()} replace />;
    }

    const slideIndex = slides.indexOf(slide);
    const stage = isPipelineStage(slide.section) ? slide.section : null;
    const colours = stage === null ? null : stageColourClasses(stage);
    // On a stage's last slide the button fills with the stage's colour: it's time to show the technique running.
    const isDemoCue = colours !== null && isLastSlideOfStage(slide);

    return (
        <div className="flex min-h-screen flex-col">
            <div aria-hidden="true" className={cn('h-2 flex-none', colours?.fill ?? 'bg-border')} />
            <header className="flex h-14 flex-none items-center gap-4 px-6">
                <Link to="/" aria-label="Home">
                    <Logo size={28} />
                </Link>
                <span className={cn('text-lg font-bold', colours?.ink ?? 'text-muted-foreground')}>
                    {slideSectionLabel(slide)}
                </span>
                <div className="flex-1" />
                {slide.demo === undefined ? null : (
                    <button
                        type="button"
                        onClick={() => openDemo(demoPath)}
                        className={cn(
                            'flex items-center gap-2 rounded-full border-2 px-3 py-1 font-bold',
                            isDemoCue
                                ? cn(colours.fill, colours.onFill, colours.border, 'hover:opacity-90')
                                : 'hover:bg-muted',
                        )}
                    >
                        <MonitorPlay aria-hidden="true" className="size-4" />
                        Open demo
                        <kbd className={cn('font-mono', !isDemoCue && 'text-muted-foreground')}>D</kbd>
                    </button>
                )}
                <ThemeToggle />
            </header>
            <SlideContent key={slide.id} slide={slide} />
            <TalkControls
                previousPath={previous === null ? null : slidePath(previous.id)}
                nextPath={next === null ? null : slidePath(next.id)}
                label={`Slide ${slideIndex + 1} of ${slides.length}`}
            />
        </div>
    );
}

function SlideContent({ slide }: { slide: Slide }) {
    const markdown = slideMarkdown(slide);
    const stage = isPipelineStage(slide.section) ? slide.section : null;
    const stepColour = stage === null ? '' : stepColourByGroup[stageGroup(stage)];
    // A stage slide's title takes its triad colour, so the audience sees which part of the argument they're in.
    // The ink shade is the one made for coloured text, readable in both themes; other slides keep the text colour.
    const titleColour = stage === null ? '' : stageColourClasses(stage).ink;
    const isTitle = slide.layout === 'title';
    const heading = slideHeading(slide);

    const body =
        markdown === null ? (
            <p role="alert" className="text-incompatible-ink">
                content/{slide.file} doesn't exist.
            </p>
        ) : (
            <Markdown className={cn('gap-6 text-4xl leading-snug', isTitle && 'text-muted-foreground')}>
                {markdown}
            </Markdown>
        );

    // Dosis is for the talk title only (ADR-0014 § Visual design), so only the title slide uses it.
    const headingElement = (
        <h1
            className={cn(
                'font-bold',
                titleColour,
                isTitle ? 'font-brand text-8xl leading-[0.95] font-extrabold' : 'text-6xl leading-tight',
            )}
        >
            {heading.title}
            {heading.subtitle === null ? null : (
                <span className="mt-3 block text-5xl font-bold text-muted-foreground">
                    {heading.subtitle}
                </span>
            )}
        </h1>
    );

    // Every slide reads top down from the same place, so the eye finds the title without searching for it when
    // the slide changes. Only the opening title slide is centred, as a cover, with the speaker beside it: it is
    // on screen while the audience arrives.
    return (
        <main
            className={cn(
                'mx-auto flex w-full max-w-6xl flex-1 flex-col gap-10 px-10',
                isTitle ? 'justify-center py-8' : 'pt-12 pb-8',
                stepColour,
            )}
        >
            {isTitle ? (
                <div className="grid items-center gap-10 lg:grid-cols-[minmax(0,1fr)_26rem]">
                    <div className="flex flex-col gap-10">
                        {headingElement}
                        {body}
                    </div>
                    <SpeakerCard speaker={speaker} />
                </div>
            ) : (
                <>
                    {headingElement}
                    {slide.layout === 'speaker' ? (
                        <div className="grid items-center gap-10 lg:grid-cols-[minmax(0,1fr)_26rem]">
                            {body}
                            <SpeakerCard speaker={speaker} />
                        </div>
                    ) : (
                        body
                    )}
                </>
            )}
        </main>
    );
}

// A named window: opening the demo again navigates the same window instead of opening another.
function openDemo(demoPath: string | null): void {
    if (demoPath !== null) {
        window.open(demoPath, demoWindowName)?.focus();
    }
}

// D opens the demo, as ← and → move the deck: ignored while typing or with a modifier, like the talk's keys.
function useDemoKey(demoPath: string | null): void {
    useEffect(() => {
        function handleKeyDown(event: KeyboardEvent) {
            if (event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) {
                return;
            }
            if (isTypingTarget(event.target) || event.key.toLowerCase() !== 'd') {
                return;
            }

            event.preventDefault();
            openDemo(demoPath);
        }

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [demoPath]);
}
