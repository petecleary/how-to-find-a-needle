import { ArrowLeft, ArrowRight } from 'lucide-react';
import { Link } from 'react-router';
import { cn } from '@/lib/utils';

export interface TalkControlsProps {
    previousPath: string | null;
    nextPath: string | null;
    /** Where the presenter is, e.g. "Step 5 of 9 · Results (2 of 3)". Announced to screen readers when it changes. */
    label: string;
}

/** The bar along the bottom of talk mode: Previous and Next for a mouse or a clicker, and the position. */
export function TalkControls({ previousPath, nextPath, label }: TalkControlsProps) {
    return (
        <nav
            aria-label="Talk steps"
            className="sticky bottom-0 z-10 flex items-center gap-3 border-t-2 bg-card px-6 py-2"
        >
            <StepLink path={previousPath} direction="previous" />
            <p aria-live="polite" className="flex-1 text-center text-sm text-muted-foreground">
                {label}
                <span className="presentation:hidden">
                    {' · '}
                    <kbd className="font-mono">←</kbd> <kbd className="font-mono">→</kbd>
                </span>
            </p>
            <StepLink path={nextPath} direction="next" />
        </nav>
    );
}

function StepLink({ path, direction }: { path: string | null; direction: 'previous' | 'next' }) {
    const isNext = direction === 'next';
    const label = isNext ? 'Next' : 'Previous';
    const className = 'flex items-center gap-1.5 rounded-full border-2 px-3 py-1 font-bold';

    if (path === null) {
        return (
            <span aria-disabled="true" className={cn(className, 'opacity-40')}>
                {isNext ? null : <ArrowLeft aria-hidden="true" className="size-4" />}
                {label}
                {isNext ? <ArrowRight aria-hidden="true" className="size-4" /> : null}
            </span>
        );
    }

    return (
        <Link to={path} className={cn(className, 'hover:bg-muted')}>
            {isNext ? null : <ArrowLeft aria-hidden="true" className="size-4" />}
            {label}
            {isNext ? <ArrowRight aria-hidden="true" className="size-4" /> : null}
        </Link>
    );
}
