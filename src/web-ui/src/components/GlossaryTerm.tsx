import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { HoverCard, HoverCardContent, HoverCardTrigger } from '@/components/ui/hover-card';
import { findGlossaryEntry } from '@/lib/content';

// GlossaryTerm — a term explained where it appears: pedagogy applied to the tool itself (ADR-0014). Hovering
// or focusing the term shows its definition; following the link opens the glossary. Radix HoverCard opens on
// keyboard focus as well as the pointer, so the talk stays keyboard-operable.

export interface GlossaryTermProps {
    /** The glossary entry's id, from a `[text](term:id)` link. */
    id: string;
    children: ReactNode;
}

export function GlossaryTerm({ id, children }: GlossaryTermProps) {
    const entry = findGlossaryEntry(id);

    // content.test.ts fails on an unknown id, so this only shows while someone is editing content.
    if (entry === null) {
        return (
            <span
                title={`No glossary entry "${id}"`}
                className="text-incompatible-ink underline decoration-wavy"
            >
                {children}
            </span>
        );
    }

    return (
        <HoverCard openDelay={150} closeDelay={100}>
            <HoverCardTrigger asChild>
                <Link
                    to={`/glossary#${entry.id}`}
                    className="underline decoration-current decoration-dotted decoration-2 underline-offset-4"
                >
                    {children}
                </Link>
            </HoverCardTrigger>
            <HoverCardContent
                align="start"
                className="flex w-96 flex-col gap-1.5 rounded-card border-2 px-4 py-3"
            >
                <p className="flex flex-wrap items-baseline gap-x-2">
                    <b className="text-lg">{entry.acronym ?? entry.term}</b>
                    {entry.acronym === undefined ? null : (
                        <span className="text-muted-foreground">{entry.term}</span>
                    )}
                </p>
                <p>{entry.definition}</p>
                <p className="text-sm text-muted-foreground">{entry.topic}</p>
            </HoverCardContent>
        </HoverCard>
    );
}
