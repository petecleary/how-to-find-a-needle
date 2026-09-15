import { LoaderCircle } from 'lucide-react';
import type { ReactNode } from 'react';
import type { AnswerMeta } from '@/api/answerEvents';
import { AnswerWarnings } from '@/components/AnswerWarnings';
import { CitationSummary } from '@/components/CitationSummary';
import { Markdown } from '@/components/Markdown';
import { StreamingBadge } from '@/components/StreamingBadge';
import type { AnswerSectionState } from '@/hooks/useAnswerStream';
import { linkCitations, withoutSentinel } from '@/lib/citations';
import { formatMilliseconds } from '@/lib/format';

// AnswerPanel — Stage 6's grounded answer, as it streams (ADR-0016). The text appears before it is checked; the
// citation verdict and any warnings arrive with the section's final event. The timings show why streaming matters:
// the first words arrive long before the answer is done.

export interface AnswerPanelProps {
    section: AnswerSectionState;
    meta: AnswerMeta | null;
    isStreaming: boolean;
    hasFailed: boolean;
    timeToFirstTokenMs: number | null;
    /** The whole answer request's time; Stage 7 shows its total in the explanation instead. */
    totalMs: number | null;
    renderProductLink: (productId: string) => ReactNode;
}

export function AnswerPanel({
    section,
    meta,
    isStreaming,
    hasFailed,
    timeToFirstTokenMs,
    totalMs,
    renderProductLink,
}: AnswerPanelProps) {
    const { final } = section;
    const text = withoutSentinel(section.markdown);

    const timings = [
        timeToFirstTokenMs === null ? null : `First token ${formatMilliseconds(timeToFirstTokenMs)}`,
        totalMs === null ? null : `done in ${(totalMs / 1000).toFixed(1)} s`,
    ].filter((part) => part !== null);

    return (
        <section aria-labelledby="answer-heading" className="flex flex-col gap-2">
            <div className="flex min-h-7 flex-wrap items-center gap-2">
                <h2
                    id="answer-heading"
                    className="text-sm font-bold tracking-[.08em] text-muted-foreground uppercase"
                >
                    Answer
                </h2>
                {final === null ? isStreaming ? <StreamingBadge /> : null : <CitationSummary final={final} />}
                {final?.insufficientEvidence === true ? (
                    <span className="rounded-full bg-unknown-tint px-2.5 py-0.5 text-sm font-bold text-unknown-ink">
                        Insufficient evidence
                    </span>
                ) : null}
            </div>

            <div className="rounded-card border-2 bg-card px-5 py-4 text-[17px]">
                {text === '' ? (
                    <p className="flex items-center gap-2 text-muted-foreground">
                        {hasFailed ? null : (
                            <LoaderCircle aria-hidden="true" className="size-5 animate-spin" />
                        )}
                        {hasFailed ? 'No answer text arrived.' : 'Waiting for the first token…'}
                    </p>
                ) : (
                    <Markdown renderProductLink={renderProductLink}>{linkCitations(text)}</Markdown>
                )}
            </div>

            {final === null ? null : <AnswerWarnings warnings={final.warnings} />}

            <p className="text-sm text-muted-foreground">
                {timings.join(' · ')}
                {meta === null
                    ? null
                    : `${timings.length > 0 ? ' · ' : ''}${meta.model} via ${meta.provider}`}
            </p>
        </section>
    );
}
