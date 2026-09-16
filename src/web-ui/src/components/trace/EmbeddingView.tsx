import { TraceSection, traceChipClass } from '@/components/trace/TraceSection';
import type { EmbeddingDetails } from '@/lib/traceDetails';

// EmbeddingView — the exact text the model embedded, task prefix included, and the first numbers of the
// vector it became. No single dimension means anything on its own; only the direction of all of them does.

export interface EmbeddingViewProps {
    details: EmbeddingDetails;
}

export function EmbeddingView({ details }: EmbeddingViewProps) {
    const { prefix, embeddedText } = details;
    const hasPrefix = prefix !== null && embeddedText?.startsWith(prefix) === true;
    const text = hasPrefix ? embeddedText.slice(prefix.length) : embeddedText;

    return (
        <div className="flex flex-col gap-4">
            <TraceSection title="Model">
                <p>
                    <code className="font-mono">{details.model ?? '—'}</code>
                    {details.provider === null ? null : (
                        <span className="text-muted-foreground">
                            {' '}
                            · {details.provider}, run locally with ONNX Runtime
                        </span>
                    )}
                </p>
            </TraceSection>

            <TraceSection title="Text embedded">
                <p className="rounded-xl bg-muted px-3 py-2 font-mono text-[15px]">
                    {hasPrefix ? <span className="text-search-ink">{prefix}</span> : null}
                    {text ?? '—'}
                </p>
                <p className="text-sm text-muted-foreground">
                    {details.tokenCount ?? '—'} tokens
                    {details.isTruncated ? (
                        <b className="text-unknown-ink"> · truncated: the end of the text was not embedded</b>
                    ) : null}
                </p>
            </TraceSection>

            <TraceSection title={`Vector · ${details.dimensions ?? '—'} dimensions`}>
                <p className="flex flex-wrap items-center gap-1.5">
                    {details.firstDimensions.map((value, index) => (
                        <span key={index} className={traceChipClass}>
                            {value.toFixed(4)}
                        </span>
                    ))}
                    <span className="font-mono text-muted-foreground">
                        …{' '}
                        {details.dimensions === null
                            ? ''
                            : details.dimensions - details.firstDimensions.length}{' '}
                        more
                    </span>
                </p>
            </TraceSection>
        </div>
    );
}
