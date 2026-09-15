import { Search } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useLocation } from 'react-router';
import { AppHeader } from '@/components/AppHeader';
import { findGlossaryEntry, glossary } from '@/lib/content';
import { findDecision } from '@/lib/decisions';
import { filterGlossary, groupByTopic } from '@/lib/glossarySearch';
import { cn } from '@/lib/utils';

/** `/glossary`: every term and acronym the talk uses, searchable and grouped by topic (ADR-0014 § Pages). */
export function GlossaryPage() {
    const [query, setQuery] = useState('');
    const { hash } = useLocation();
    const groups = groupByTopic(filterGlossary(glossary, query));

    // A term link (#rrf) scrolls to its entry, which is highlighted.
    useEffect(() => {
        if (hash === '') {
            window.scrollTo(0, 0);
        } else {
            document.getElementById(decodeURIComponent(hash.slice(1)))?.scrollIntoView({ block: 'center' });
        }
    }, [hash]);

    return (
        <div className="flex min-h-screen flex-col">
            <AppHeader />
            <main className="mx-auto flex w-full max-w-4xl flex-1 flex-col gap-6 px-6 py-8">
                <div className="flex flex-col gap-2">
                    <h1 className="text-4xl font-bold">Glossary</h1>
                    <p className="text-lg text-muted-foreground">
                        {glossary.length} terms and acronyms, in the words the talk, the code and the traces
                        share.
                    </p>
                </div>

                <label className="flex items-center gap-2.5 rounded-full border-2 bg-card px-4 py-2 focus-within:border-ring focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-ring">
                    <Search aria-hidden="true" className="size-5 text-muted-foreground" />
                    <span className="sr-only">Search the glossary</span>
                    <input
                        type="search"
                        value={query}
                        onChange={(event) => setQuery(event.target.value)}
                        placeholder="Search terms, acronyms and definitions"
                        className="min-w-0 flex-1 bg-transparent text-lg outline-none placeholder:text-muted-foreground"
                    />
                </label>

                {groups.length === 0 ? (
                    <p className="text-muted-foreground">No term matches “{query}”.</p>
                ) : null}

                {groups.map(([topic, entries]) => (
                    <section key={topic} aria-labelledby={`topic-${topic}`} className="flex flex-col gap-3">
                        <h2 id={`topic-${topic}`} className="text-2xl font-bold">
                            {topic}
                        </h2>
                        <dl className="flex flex-col gap-3">
                            {entries.map((entry) => {
                                const decision = entry.adr === undefined ? null : findDecision(entry.adr);

                                return (
                                    <div
                                        key={entry.id}
                                        id={entry.id}
                                        className={cn(
                                            'flex scroll-mt-4 flex-col gap-1 rounded-card border-2 bg-card px-5 py-3',
                                            hash === `#${entry.id}` && 'border-ontology',
                                        )}
                                    >
                                        <dt className="flex flex-wrap items-baseline gap-x-2">
                                            <b className="text-xl">{entry.acronym ?? entry.term}</b>
                                            {entry.acronym === undefined ? null : (
                                                <span className="text-muted-foreground">{entry.term}</span>
                                            )}
                                        </dt>
                                        <dd className="text-[17px]">{entry.definition}</dd>
                                        {entry.seeAlso !== undefined || decision !== null ? (
                                            <dd className="flex flex-wrap gap-x-4 gap-y-1 text-sm">
                                                {(entry.seeAlso ?? []).map((id) => (
                                                    <Link
                                                        key={id}
                                                        to={`/glossary#${id}`}
                                                        className="underline underline-offset-4"
                                                    >
                                                        See{' '}
                                                        {findGlossaryEntry(id)?.acronym ??
                                                            findGlossaryEntry(id)?.term ??
                                                            id}
                                                    </Link>
                                                ))}
                                                {decision === null ? null : (
                                                    <Link
                                                        to={`/decisions/${decision.id}`}
                                                        className="font-bold underline underline-offset-4"
                                                    >
                                                        ADR-{decision.number}
                                                    </Link>
                                                )}
                                            </dd>
                                        ) : null}
                                    </div>
                                );
                            })}
                        </dl>
                    </section>
                ))}
            </main>
        </div>
    );
}
