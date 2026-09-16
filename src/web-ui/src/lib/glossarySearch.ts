import type { GlossaryEntry } from './content';

/** Entries whose term, acronym, definition or topic contains the search text, ignoring case. */
export function filterGlossary(entries: readonly GlossaryEntry[], query: string): GlossaryEntry[] {
    const text = query.trim().toLowerCase();
    if (text === '') {
        return [...entries];
    }

    return entries.filter((entry) =>
        [entry.term, entry.acronym ?? '', entry.definition, entry.topic].some((field) =>
            field.toLowerCase().includes(text),
        ),
    );
}

/** Entries grouped by topic, topics in the order they first appear in glossary.json. */
export function groupByTopic(entries: readonly GlossaryEntry[]): [topic: string, entries: GlossaryEntry[]][] {
    const groups = new Map<string, GlossaryEntry[]>();

    for (const entry of entries) {
        groups.set(entry.topic, [...(groups.get(entry.topic) ?? []), entry]);
    }

    return [...groups.entries()];
}
