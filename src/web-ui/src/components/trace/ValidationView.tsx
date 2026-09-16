import { Check, X } from 'lucide-react';
import { AnswerWarnings } from '@/components/AnswerWarnings';
import { TraceSection, traceChipClass } from '@/components/trace/TraceSection';
import type { ValidationDetails } from '@/lib/traceDetails';
import { cn } from '@/lib/utils';

// ValidationView — every check run on the finished text, passed or failed (ADR-0016, ADR-0017). Nothing is stripped or
// regenerated, so this is where a citation of a product the model wasn't given, or a muddled structure, shows up.
// Heuristic checks are labelled: they guess from wording.

export interface ValidationViewProps {
    details: ValidationDetails;
}

export function ValidationView({ details }: ValidationViewProps) {
    const { structure } = details;

    return (
        <div className="flex flex-col gap-4">
            <TraceSection title="Checks">
                <ul className="flex flex-col gap-2">
                    {details.checks.map((check) => (
                        <li key={check.name} className="flex gap-2">
                            {check.passed ? (
                                <Check
                                    aria-hidden="true"
                                    className="mt-0.5 size-5 flex-none text-compatible-ink"
                                />
                            ) : (
                                <X
                                    aria-hidden="true"
                                    className="mt-0.5 size-5 flex-none text-incompatible-ink"
                                />
                            )}
                            <span className="flex flex-col">
                                <span>
                                    <span className="sr-only">{check.passed ? 'Passed: ' : 'Failed: '}</span>
                                    <b>{check.name}</b>
                                    {check.isHeuristic ? (
                                        <span className="ml-2 rounded-full bg-muted px-2 text-xs font-bold text-muted-foreground">
                                            heuristic
                                        </span>
                                    ) : null}
                                </span>
                                <span className="text-muted-foreground">{check.detail}</span>
                            </span>
                        </li>
                    ))}
                </ul>
            </TraceSection>

            <div className="flex flex-wrap gap-x-8 gap-y-3">
                <TraceSection title="Citations">
                    <div className="flex flex-wrap gap-1.5">
                        {details.citations.length === 0 ? (
                            <span className="text-muted-foreground">none</span>
                        ) : null}
                        {details.citations.map((id) => (
                            <span
                                key={id}
                                className={cn(
                                    traceChipClass,
                                    details.invalidCitations.includes(id) &&
                                        'border-dashed border-incompatible-ink text-incompatible-ink',
                                )}
                            >
                                {id}
                                {details.invalidCitations.includes(id) ? ' · not in evidence' : ''}
                            </span>
                        ))}
                    </div>
                </TraceSection>
                {details.insufficientEvidence === true ? (
                    <TraceSection title="Sentinel">
                        <p>INSUFFICIENT_EVIDENCE</p>
                    </TraceSection>
                ) : null}
            </div>

            {structure === null ? null : (
                <TraceSection title="Parsed structure">
                    <dl className="grid grid-cols-[max-content_1fr] gap-x-4 gap-y-1">
                        <dt className="font-bold">Decision</dt>
                        <dd className="font-mono">{structure.decision ?? '—'}</dd>
                        <dt className="font-bold">Concepts</dt>
                        <dd>{structure.concepts.length === 0 ? '—' : structure.concepts.join(', ')}</dd>
                        <dt className="font-bold">Near miss</dt>
                        <dd className="font-mono">{structure.nearMiss ?? '—'}</dd>
                        <dt className="font-bold">Rule of thumb</dt>
                        <dd>{structure.ruleOfThumb ?? '—'}</dd>
                        <dt className="font-bold">Next step</dt>
                        <dd>{structure.nextStep ?? '—'}</dd>
                    </dl>
                </TraceSection>
            )}

            {details.warnings.length > 0 ? (
                <TraceSection title="Warnings">
                    <AnswerWarnings warnings={details.warnings} />
                </TraceSection>
            ) : null}
        </div>
    );
}
