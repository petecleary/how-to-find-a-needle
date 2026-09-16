import { LlmSettings } from '@/components/trace/LlmSettings';
import { TraceSection, traceTableClass } from '@/components/trace/TraceSection';
import { formatMilliseconds } from '@/lib/format';
import type { GenerationDetails, SectionTiming } from '@/lib/traceDetails';

// GenerationView — what the model wrote, before any check, and how long it took (ADR-0015, ADR-0016). Time to first
// token is what the audience feels; the total is when validation can start. Stage 7 adds each section's timings.

export interface GenerationViewProps {
    details: GenerationDetails;
}

export function GenerationView({ details }: GenerationViewProps) {
    const stats: [label: string, value: string][] = [
        ['First token', formatOptionalMs(details.timeToFirstTokenMs)],
        ['Total', formatOptionalMs(details.totalMs)],
        ['Finish reason', details.finishReason ?? '—'],
        ['Tokens in / out', `${details.inputTokens ?? '—'} / ${details.outputTokens ?? '—'}`],
    ];

    return (
        <div className="flex flex-col gap-4">
            <dl className="grid grid-cols-2 gap-x-6 gap-y-2 sm:grid-cols-4">
                {stats.map(([label, value]) => (
                    <div key={label}>
                        <dt className="text-xs font-bold tracking-wide text-muted-foreground uppercase">
                            {label}
                        </dt>
                        <dd className="font-mono">{value}</dd>
                    </div>
                ))}
            </dl>

            {details.answerTiming === null && details.explanationTiming === null ? null : (
                <TraceSection title="Per section">
                    <table className={traceTableClass}>
                        <thead>
                            <tr>
                                <th>Section</th>
                                <th>Started at</th>
                                <th>First token</th>
                                <th>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            <TimingRow label="Answer" timing={details.answerTiming} />
                            <TimingRow label="Explanation" timing={details.explanationTiming} />
                        </tbody>
                    </table>
                </TraceSection>
            )}

            <LlmSettings llm={details.llm} />

            <TraceSection title="Raw output">
                <pre className="max-h-[32rem] overflow-auto rounded-xl bg-muted px-4 py-3 font-mono text-[13px] leading-relaxed break-words whitespace-pre-wrap">
                    {details.rawOutput === '' ? '(no text)' : details.rawOutput}
                </pre>
            </TraceSection>
        </div>
    );
}

interface TimingRowProps {
    label: string;
    timing: SectionTiming | null;
}

function TimingRow({ label, timing }: TimingRowProps) {
    return (
        <tr>
            <td>{label}</td>
            <td className="font-mono">{formatOptionalMs(timing?.startedAtMs ?? null)}</td>
            <td className="font-mono">{formatOptionalMs(timing?.timeToFirstTokenMs ?? null)}</td>
            <td className="font-mono">{formatOptionalMs(timing?.totalMs ?? null)}</td>
        </tr>
    );
}

function formatOptionalMs(milliseconds: number | null): string {
    return milliseconds === null ? '—' : formatMilliseconds(milliseconds);
}
