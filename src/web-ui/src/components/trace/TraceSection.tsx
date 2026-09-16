import type { ReactNode } from 'react';
import { cn } from '@/lib/utils';

/** Classes shared by the trace renderers' small monospaced chips (lexemes, notations, terms). */
export const traceChipClass =
    'inline-flex items-center rounded-full border-2 px-2 py-0.5 font-mono text-[13px] whitespace-nowrap';

/** Classes shared by the trace renderers' tables. */
export const traceTableClass =
    'w-full border-collapse text-sm [&_td]:border-t-2 [&_td]:px-2 [&_td]:py-1 [&_td]:align-top [&_th]:px-2 [&_th]:pb-1 [&_th]:text-left [&_th]:text-xs [&_th]:font-bold [&_th]:text-muted-foreground';

export interface TraceSectionProps {
    title: string;
    children: ReactNode;
    className?: string;
}

/** A labelled part of a trace step's view, e.g. "tsquery" or "Parameters". */
export function TraceSection({ title, children, className }: TraceSectionProps) {
    return (
        <section className={cn('flex min-w-0 flex-col gap-1.5', className)}>
            <h4 className="text-xs font-bold tracking-wide text-muted-foreground uppercase">{title}</h4>
            {children}
        </section>
    );
}
