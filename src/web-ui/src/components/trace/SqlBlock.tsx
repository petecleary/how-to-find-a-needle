import { TraceSection, traceTableClass } from '@/components/trace/TraceSection';

// SqlBlock — the exact parameterised SQL that ran, with each parameter's value beside it and never pasted
// into the text (ADR-0003). Values are shown as JSON, so 18 and "18" look different, as they are to Postgres.

export interface SqlBlockProps {
    sql: string;
    parameters: Record<string, unknown> | null;
    title?: string;
}

export function SqlBlock({ sql, parameters, title = 'SQL' }: SqlBlockProps) {
    const entries = Object.entries(parameters ?? {});

    return (
        <TraceSection title={title}>
            <pre className="overflow-x-auto rounded-xl bg-muted px-3 py-2 font-mono text-[13px] leading-relaxed">
                {sql}
            </pre>
            {entries.length > 0 ? (
                <div className="overflow-x-auto">
                    <table className={traceTableClass}>
                        <thead>
                            <tr>
                                <th scope="col">Parameter</th>
                                <th scope="col">Value</th>
                            </tr>
                        </thead>
                        <tbody>
                            {entries.map(([name, value]) => (
                                <tr key={name}>
                                    <td className="font-mono whitespace-nowrap">@{name}</td>
                                    <td className="font-mono break-all">{JSON.stringify(value)}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            ) : null}
        </TraceSection>
    );
}
