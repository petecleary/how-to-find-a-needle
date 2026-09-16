import { LoaderCircle } from 'lucide-react';
import type { ReactNode } from 'react';
import { AnswerWarnings } from '@/components/AnswerWarnings';
import { CitationSummary } from '@/components/CitationSummary';
import { Markdown } from '@/components/Markdown';
import { StreamingBadge } from '@/components/StreamingBadge';
import type { AnswerSectionState } from '@/hooks/useAnswerStream';
import { linkCitations } from '@/lib/citations';
import type { Audience } from '@/lib/searchState';

// ExplanationPanel — Stage 7's explanation of the checked answer, for the chosen audience (ADR-0017). With pedagogy on
// it streams under five fixed headings (decision → concepts → near miss → rule of thumb → next step); with it off, the
// baseline's free-form text, from the same facts and words. It starts only after the answer's final event.

export interface ExplanationPanelProps {
    section: AnswerSectionState;
    audience: Audience;
    applyPedagogy: boolean;
    isStreaming: boolean;
    /** True while the answer is still being written: the explanation hasn't started. */
    isWaitingForAnswer: boolean;
    hasFailed: boolean;
    totalMs: number | null;
    renderProductLink: (productId: string) => ReactNode;
}

export function ExplanationPanel({
    section,
    audience,
    applyPedagogy,
    isStreaming,
    isWaitingForAnswer,
    hasFailed,
    totalMs,
    renderProductLink,
}: ExplanationPanelProps) {
    const { final } = section;

    let placeholder: string;
    if (hasFailed) {
        placeholder = 'No explanation text arrived.';
    } else if (isWaitingForAnswer) {
        placeholder = 'Starts when the answer is complete.';
    } else {
        placeholder = 'Waiting for the first token…';
    }

    return (
        <section aria-labelledby="explanation-heading" className="flex flex-col gap-2">
            <div className="flex min-h-7 flex-wrap items-center gap-2">
                <h2
                    id="explanation-heading"
                    className="text-sm font-bold tracking-[.08em] text-pedagogy-ink uppercase"
                >
                    Explanation · for {audience === 'expert' ? 'an' : 'a'} {audience}
                </h2>
                <span className="rounded-full bg-muted px-2.5 py-0.5 text-sm font-bold text-muted-foreground">
                    {applyPedagogy ? 'pedagogy prompt' : 'baseline prompt'}
                </span>
                {final === null ? isStreaming ? <StreamingBadge /> : null : <CitationSummary final={final} />}
            </div>

            <div className="rounded-card border-2 border-pedagogy bg-card px-5 py-4 text-[17px]">
                {section.markdown === '' ? (
                    <p className="flex items-center gap-2 text-muted-foreground">
                        {hasFailed || isWaitingForAnswer ? null : (
                            <LoaderCircle aria-hidden="true" className="size-5 animate-spin" />
                        )}
                        {placeholder}
                    </p>
                ) : (
                    // The model's ## headings are section labels here, not page headings: small and in Pedagogy orange.
                    <Markdown
                        renderProductLink={renderProductLink}
                        className="[&_h2]:mt-2 [&_h2]:text-lg [&_h2]:text-pedagogy-ink [&_h2:first-child]:mt-0 [&_h3]:mt-2 [&_h3]:text-lg [&_h3]:text-pedagogy-ink"
                    >
                        {linkCitations(section.markdown)}
                    </Markdown>
                )}
            </div>

            {final === null ? null : <AnswerWarnings warnings={final.warnings} />}

            {totalMs === null ? null : (
                <p className="text-sm text-muted-foreground">
                    Answer and explanation done in {(totalMs / 1000).toFixed(1)} s
                </p>
            )}
        </section>
    );
}
