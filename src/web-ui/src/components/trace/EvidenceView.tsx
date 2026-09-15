import { CompatibilityBadge } from '@/components/CompatibilityBadge';
import { TraceSection, traceChipClass, traceTableClass } from '@/components/trace/TraceSection';
import type { EvidenceDetails } from '@/lib/traceDetails';

// EvidenceView — the evidence set as the model received it (ADR-0016): each product with its verdict and why it was
// included, the matched concepts with their labels and definitions, and the rules. What isn't here, the model can't use.

export interface EvidenceViewProps {
    details: EvidenceDetails;
}

const limitLabels: Record<string, string> = {
    compatible: 'compatible',
    incompatible: 'incompatible',
    unknown: 'unknown',
    notChecked: 'not checked',
    descriptionCharacters: 'description characters',
};

export function EvidenceView({ details }: EvidenceViewProps) {
    const limits = Object.entries(details.limits)
        .map(([key, value]) => `${value} ${limitLabels[key] ?? key}`)
        .join(' · ');

    return (
        <div className="flex flex-col gap-4">
            <TraceSection title={`Products given to the model · ${details.items.length}`}>
                <div className="overflow-x-auto">
                    <table className={traceTableClass}>
                        <thead>
                            <tr>
                                <th>Stage 5 rank</th>
                                <th>Product</th>
                                <th>Verdict</th>
                                <th>Why it was included</th>
                            </tr>
                        </thead>
                        <tbody>
                            {details.items.map((item) => (
                                <tr key={item.id}>
                                    <td className="font-mono">
                                        {item.rank === null ? '—' : `#${item.rank}`}
                                    </td>
                                    <td>
                                        <b>{item.name}</b>
                                        <br />
                                        <span className="font-mono text-muted-foreground">{item.id}</span>
                                    </td>
                                    <td>
                                        {item.role === 'TargetDevice' ? (
                                            'Target device'
                                        ) : (
                                            <CompatibilityBadge compatibility={item.compatibility} />
                                        )}
                                    </td>
                                    <td>{item.whyIncluded}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
                {limits === '' ? null : <p className="text-sm text-muted-foreground">Limits: {limits}.</p>}
            </TraceSection>

            <div className="grid gap-4 lg:grid-cols-2">
                <TraceSection title="Concepts, in the ontology's words">
                    {details.concepts.length === 0 ? (
                        <p className="text-muted-foreground">None matched.</p>
                    ) : null}
                    <ul className="flex flex-col gap-2">
                        {details.concepts.map((concept) => (
                            <li key={concept.notation} className="flex flex-col gap-1">
                                <span>
                                    <b>{concept.prefLabel}</b>{' '}
                                    <span className="font-mono text-sm text-muted-foreground">
                                        {concept.notation}
                                    </span>
                                </span>
                                {concept.altLabels.length > 0 ? (
                                    <span className="flex flex-wrap gap-1.5">
                                        {concept.altLabels.map((label) => (
                                            <span key={label} className={traceChipClass}>
                                                {label}
                                            </span>
                                        ))}
                                    </span>
                                ) : null}
                                {concept.definition === null ? null : (
                                    <span className="text-sm text-muted-foreground">
                                        {concept.definition}
                                    </span>
                                )}
                            </li>
                        ))}
                    </ul>
                </TraceSection>

                <TraceSection title="Rules checked">
                    {details.rules.length === 0 ? (
                        <p className="text-muted-foreground">No rule was checked.</p>
                    ) : null}
                    <ul className="flex flex-col gap-2">
                        {details.rules.map((rule) => (
                            <li key={rule.name}>
                                <b className="font-mono">{rule.name}</b>
                                <ul className="list-disc pl-5 text-sm">
                                    {rule.definitions.map((definition) => (
                                        <li key={definition}>{definition}</li>
                                    ))}
                                </ul>
                            </li>
                        ))}
                    </ul>
                </TraceSection>
            </div>
        </div>
    );
}
