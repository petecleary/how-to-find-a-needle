import { TraceSection, traceChipClass } from '@/components/trace/TraceSection';
import type { ExpansionDetails } from '@/lib/traceDetails';

// ExpansionView — the synonyms and narrower-concept labels added for each wanted phrase, and the exact input
// each retriever then received: an OR group for keyword search, and the query with concept names appended
// for vector search. This is where Stage 5 improves recall before any rule runs.

export interface ExpansionViewProps {
    details: ExpansionDetails;
}

export function ExpansionView({ details }: ExpansionViewProps) {
    if (!details.expandSynonyms) {
        return (
            <div className="flex flex-col gap-4">
                <p className="text-unknown-ink">
                    expandSynonyms is off: nothing was added, and retrieval received the query as typed.
                </p>
                <TraceSection title="Vector search embeds">
                    <p className="font-mono">{details.embeddingText ?? '—'}</p>
                </TraceSection>
            </div>
        );
    }

    const keywordQuery = [
        ...details.keywordOrGroups.map((group) => `(${group.join(' | ')})`),
        ...(details.keywordRemainingText ? [details.keywordRemainingText] : []),
    ].join(' & ');

    return (
        <div className="flex flex-col gap-4">
            {details.phrases.length === 0 ? (
                <p className="text-muted-foreground">No wanted concept matched, so nothing was expanded.</p>
            ) : (
                details.phrases.map((phrase) => (
                    <TraceSection
                        key={phrase.phrase}
                        title={`“${phrase.phrase}” → ${phrase.concepts.join(', ')}`}
                    >
                        <p className="flex flex-wrap gap-1.5">
                            {phrase.terms.map((term) => (
                                <span key={term} className={traceChipClass}>
                                    {term}
                                </span>
                            ))}
                        </p>
                        <p className="text-xs text-muted-foreground">
                            {phrase.terms.length} terms: the phrase, then the concept's and its narrower
                            concepts' labels (at most 10)
                        </p>
                    </TraceSection>
                ))
            )}

            <TraceSection title="Keyword search receives">
                <p className="rounded-xl bg-muted px-3 py-2 font-mono text-[13px]">{keywordQuery || '—'}</p>
            </TraceSection>

            <TraceSection title="Vector search embeds">
                <p className="rounded-xl bg-muted px-3 py-2 font-mono text-[13px]">
                    {details.embeddingText ?? '—'}
                </p>
            </TraceSection>
        </div>
    );
}
