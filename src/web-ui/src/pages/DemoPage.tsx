import { Link2, Link2Off } from 'lucide-react';
import { useCallback, useState } from 'react';
import { useSearchParams } from 'react-router';
import { getGoldenQueries } from '@/api/client';
import { AppHeader } from '@/components/AppHeader';
import { StageScreen } from '@/components/StageScreen';
import { useApiData } from '@/hooks/useApiData';
import { useFollowSlides } from '@/hooks/useFollowSlides';
import { useSearchState } from '@/hooks/useSearchState';
import { findSlide, followedSlideParam, followParam, slideDemoState, slidePreset } from '@/lib/slides';
import { cn } from '@/lib/utils';

/**
 * `/demo`: the free-exploration stage screen (ADR-0014 § Pages). Every input lives in the URL, so a moment can
 * be bookmarked and Back undoes a change.
 *
 * Opened from the slide deck (`?follow=1`), it also follows the slides: each slide that names a demo loads its
 * stage, golden query, options and tab here, as an ordinary URL change, so Back steps through what was shown.
 */
export function DemoPage() {
    const [state, setState] = useSearchState();
    const goldenQueries = useApiData(getGoldenQueries);

    // Read once, when the window opens: the first change to the search state rewrites the query string.
    const [searchParams] = useSearchParams();
    const [isOpenedByDeck] = useState(() => searchParams.get(followParam) === '1');
    const [isFollowing, setIsFollowing] = useState(isOpenedByDeck);
    const [followedSlideId, setFollowedSlideId] = useState(() => searchParams.get(followedSlideParam));

    const goldenQueryList = goldenQueries.data;
    const handleSlide = useCallback(
        (slideId: string) => {
            const slide = findSlide(slideId);
            if (slide === null) {
                return;
            }

            setFollowedSlideId(slide.id);
            // A slide with no demo (the intro, the summary) leaves the demo as it is; only the badge changes.
            const slideState = slideDemoState(slide, slidePreset(slide, goldenQueryList));
            if (slideState !== null) {
                setState(slideState);
            }
        },
        [goldenQueryList, setState],
    );
    useFollowSlides(isFollowing, handleSlide);

    const followedSlide = findSlide(followedSlideId ?? undefined);

    return (
        <div className="flex min-h-screen flex-col">
            <AppHeader />
            {isOpenedByDeck ? (
                <div className="flex items-center gap-3 border-b-2 bg-muted px-6 py-1.5" aria-live="polite">
                    <span className={cn('font-bold', !isFollowing && 'text-muted-foreground')}>
                        {isFollowing ? 'Following the slides' : 'Not following the slides'}
                        {isFollowing && followedSlide !== null ? `: ${followedSlide.title}` : ''}
                    </span>
                    <div className="flex-1" />
                    <button
                        type="button"
                        aria-pressed={isFollowing}
                        onClick={() => setIsFollowing(!isFollowing)}
                        className="flex items-center gap-1.5 rounded-full border-2 bg-card px-3 py-0.5 text-sm font-bold hover:bg-muted"
                    >
                        {isFollowing ? (
                            <Link2Off aria-hidden="true" className="size-4" />
                        ) : (
                            <Link2 aria-hidden="true" className="size-4" />
                        )}
                        {isFollowing ? 'Stop following' : 'Follow the slides'}
                    </button>
                </div>
            ) : null}
            <StageScreen state={state} onStateChange={setState} goldenQueries={goldenQueries} />
        </div>
    );
}
