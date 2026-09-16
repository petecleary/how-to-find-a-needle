import { Check, CircleQuestionMark, X, type LucideIcon } from 'lucide-react';
import { ShowMoreButton } from '@/components/trace/ShowMoreButton';
import { TraceSection, traceChipClass } from '@/components/trace/TraceSection';
import { useShowMore } from '@/hooks/useShowMore';
import { operatorSymbol, type CheckResult, type ConstrainDetails, type RuleCheck } from '@/lib/traceDetails';
import { cn } from '@/lib/utils';

// RuleChecks — every domain rule check against the target device (or, with no device, against the values the
// query stated): the two specs compared, their values, the operator and the result. The rules are data in the TTL, found with SPARQL (shown below); the evaluator runs
// whatever checks it finds and never names a rule. This is the step beyond SKOS (ADR-0013).

const results: Record<CheckResult, { Icon: LucideIcon; className: string }> = {
    Pass: { Icon: Check, className: 'text-compatible-ink' },
    Fail: { Icon: X, className: 'text-incompatible-ink' },
    Unknown: { Icon: CircleQuestionMark, className: 'text-unknown-ink' },
};

export interface RuleChecksProps {
    details: ConstrainDetails;
    productNames: Map<string, string>;
}

export function RuleChecks({ details, productNames }: RuleChecksProps) {
    const byCandidate = groupByCandidate(details.checks);
    const showMore = useShowMore(byCandidate, 6);

    if (!details.applyConstraints) {
        return (
            <p className="text-unknown-ink">
                applyConstraints is off: no rule was checked, and every result is Not evaluated.
            </p>
        );
    }

    const failCount = details.checks.filter((check) => check.result === 'Fail').length;

    return (
        <div className="flex flex-col gap-4">
            <TraceSection title="Target device">
                {details.targetDevice === null && details.checkedAgainst === 'query' ? (
                    <p>
                        None, so each check ran against <b>what the query asked for</b>. A check the query
                        says nothing about stays Unknown.
                    </p>
                ) : details.targetDevice === null ? (
                    <p className="text-unknown-ink">
                        None ({details.targetDeviceMethod ?? '—'}), and the query states no value a rule
                        compares, so there is nothing to check against.
                    </p>
                ) : (
                    <p>
                        <b>{details.targetDevice.name}</b>{' '}
                        <span className="font-mono text-xs text-muted-foreground">
                            {details.targetDevice.id}
                        </span>
                        <span className="text-muted-foreground"> · {details.targetDeviceMethod ?? '—'}</span>
                    </p>
                )}
            </TraceSection>

            <TraceSection title="Rules applied">
                <p className="flex flex-wrap gap-1.5">
                    {details.rulesApplied.length === 0 ? (
                        <span className="text-muted-foreground">none</span>
                    ) : null}
                    {details.rulesApplied.map((rule) => (
                        <span key={rule} className={traceChipClass}>
                            {rule}
                        </span>
                    ))}
                </p>
            </TraceSection>

            {details.checks.length > 0 ? (
                <TraceSection
                    title={`Checks · ${details.checks.length} on ${byCandidate.length} products · ${failCount} failed`}
                >
                    <div className="grid gap-x-8 gap-y-3 md:grid-cols-2">
                        {showMore.visible.map(([candidateId, checks]) => (
                            <div key={candidateId} className="flex min-w-0 flex-col gap-0.5">
                                <p className="font-bold">
                                    {productNames.get(candidateId) ?? candidateId}{' '}
                                    <span className="font-mono text-xs font-normal text-muted-foreground">
                                        {candidateId}
                                    </span>
                                </p>
                                {checks.map((check) => (
                                    <CheckRow key={`${check.rule}-${check.accessorySpec}`} check={check} />
                                ))}
                            </div>
                        ))}
                    </div>
                    <ShowMoreButton showMore={showMore} />
                </TraceSection>
            ) : null}

            {details.rulesSparql === null ? null : (
                <details className="rounded-xl border-2 px-3 py-2">
                    <summary className="cursor-pointer text-sm font-bold">
                        SPARQL that found the rules (rules.rq)
                    </summary>
                    <pre className="mt-2 overflow-x-auto font-mono text-[13px]">{details.rulesSparql}</pre>
                </details>
            )}
        </div>
    );
}

function CheckRow({ check }: { check: RuleCheck }) {
    const { Icon, className } = results[check.result];

    return (
        <p
            title={check.definition}
            className="grid grid-cols-[minmax(0,7rem)_minmax(0,1fr)_auto] items-baseline gap-2 text-sm"
        >
            <code className="truncate font-mono text-[13px] text-muted-foreground">
                {check.accessorySpec}
            </code>
            <span>
                <b className={cn(check.result === 'Fail' && 'text-incompatible-ink')}>
                    {check.accessoryValue ?? 'no value'}
                </b>{' '}
                <span className="font-mono">{operatorSymbol(check.operator)}</span>{' '}
                <b>{check.deviceValue ?? 'no value'}</b>{' '}
                <code className="font-mono text-xs text-muted-foreground">
                    {check.source === 'Query' ? 'you asked' : check.deviceSpec}
                </code>
            </span>
            <span className={cn('flex items-center gap-1 font-bold', className)}>
                <Icon aria-hidden="true" className="size-4" />
                {check.result}
            </span>
        </p>
    );
}

function groupByCandidate(checks: RuleCheck[]): [candidateId: string, checks: RuleCheck[]][] {
    const groups = new Map<string, RuleCheck[]>();

    for (const check of checks) {
        groups.set(check.candidateId, [...(groups.get(check.candidateId) ?? []), check]);
    }

    return [...groups.entries()];
}
