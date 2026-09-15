import { Check, CircleQuestionMark, X } from 'lucide-react';
import type { AnswerFinal } from '@/api/client';
import { cn } from '@/lib/utils';

// CitationSummary — the API's verdict on a finished section's citations (ADR-0016): all valid, or how many named a
// product the model wasn't given. It appears only after validation, never while the text is still streaming.

export interface CitationSummaryProps {
    final: AnswerFinal;
}

export function CitationSummary({ final }: CitationSummaryProps) {
    const invalidCount = final.invalidCitations.length;
    const citedCount = final.citations.length;

    const { Icon, text, className } =
        invalidCount > 0
            ? {
                  Icon: X,
                  text: `${invalidCount} invalid ${invalidCount === 1 ? 'citation' : 'citations'}`,
                  className: 'bg-incompatible-tint text-incompatible-ink',
              }
            : citedCount === 0
              ? {
                    Icon: CircleQuestionMark,
                    text: 'no citations',
                    className: 'bg-unknown-tint text-unknown-ink',
                }
              : {
                    Icon: Check,
                    text: `${citedCount} ${citedCount === 1 ? 'citation' : 'citations'} valid`,
                    className: 'bg-compatible-tint text-compatible-ink',
                };

    return (
        <span
            className={cn(
                'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-sm font-bold',
                className,
            )}
        >
            <Icon aria-hidden="true" className="size-4" />
            {text}
        </span>
    );
}
