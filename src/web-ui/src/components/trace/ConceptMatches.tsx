import { TraceSection, traceChipClass, traceTableClass } from '@/components/trace/TraceSection';
import type { UnderstandDetails } from '@/lib/traceDetails';
import { cn } from '@/lib/utils';

// ConceptMatches — how the query's words were matched to SKOS labels (preferred, alternative, and labels in
// other languages), and which matched concepts the shopper wants versus the ones that only describe the
// device they own. A wanted concept is expanded and used to classify; a context concept is not.

export interface ConceptMatchesProps {
    details: UnderstandDetails;
}

export function ConceptMatches({ details }: ConceptMatchesProps) {
    return (
        <div className="flex flex-col gap-4">
            <TraceSection title="Target device">
                {details.targetDevice === null ? (
                    <p className="text-muted-foreground">None · {details.targetDeviceMethod ?? '—'}</p>
                ) : (
                    <p>
                        <b>{details.targetDevice.name}</b>{' '}
                        <span className="font-mono text-xs text-muted-foreground">
                            {details.targetDevice.id}
                        </span>
                        <span className="text-muted-foreground"> · {details.targetDeviceMethod ?? '—'}</span>
                    </p>
                )}
                {details.deviceMention === null ? null : (
                    <p className="text-sm text-muted-foreground">
                        Took “{details.deviceMention}” out of the query, leaving “{details.queryWithoutDevice}
                        ”.
                    </p>
                )}
            </TraceSection>

            <TraceSection title="Tokens">
                <p className="flex flex-wrap gap-1.5">
                    {details.tokens.map((token, index) => (
                        <span key={index} className={traceChipClass}>
                            {token.original}
                            {token.folded === token.original ? null : ` → ${token.folded}`}
                        </span>
                    ))}
                </p>
            </TraceSection>

            <TraceSection title={`Phrases matched to labels · ${details.matches.length}`}>
                {details.matches.length === 0 ? (
                    <p className="text-muted-foreground">No phrase matched a label in the ontology.</p>
                ) : (
                    <div className="overflow-x-auto">
                        <table className={traceTableClass}>
                            <thead>
                                <tr>
                                    <th scope="col">Phrase</th>
                                    <th scope="col">Label</th>
                                    <th scope="col">Concept</th>
                                    <th scope="col">Scheme</th>
                                </tr>
                            </thead>
                            <tbody>
                                {details.matches.flatMap((match) =>
                                    match.labels.map((label) => (
                                        <tr key={`${match.phrase}-${label.concept}-${label.label}`}>
                                            <td className="font-bold">“{match.phrase}”</td>
                                            <td>
                                                {label.label}{' '}
                                                <span className="text-xs text-muted-foreground">
                                                    {label.kind} · {label.language}
                                                </span>
                                            </td>
                                            <td className="font-mono">{label.concept}</td>
                                            <td className="text-muted-foreground">{label.scheme}</td>
                                        </tr>
                                    )),
                                )}
                            </tbody>
                        </table>
                    </div>
                )}
            </TraceSection>

            <div className="flex flex-wrap gap-x-8 gap-y-4">
                <ConceptList
                    title="Wanted: expanded and classified"
                    notations={details.wantedConcepts}
                    isWanted
                />
                <ConceptList title="Context: describes your device" notations={details.contextConcepts} />
                <TraceSection title="Rest of the query">
                    <p className="font-mono">{details.remainingText || '—'}</p>
                </TraceSection>
            </div>
        </div>
    );
}

function ConceptList({
    title,
    notations,
    isWanted = false,
}: {
    title: string;
    notations: string[];
    isWanted?: boolean;
}) {
    return (
        <TraceSection title={title}>
            <p className="flex flex-wrap gap-1.5">
                {notations.length === 0 ? <span className="text-muted-foreground">none</span> : null}
                {notations.map((notation) => (
                    <span
                        key={notation}
                        className={cn(
                            traceChipClass,
                            isWanted && 'border-transparent bg-ontology-tint text-ontology-ink',
                        )}
                    >
                        {notation}
                    </span>
                ))}
            </p>
        </TraceSection>
    );
}
