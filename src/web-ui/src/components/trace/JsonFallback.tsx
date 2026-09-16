import { TraceSection } from '@/components/trace/TraceSection';

// JsonFallback — a trace step no view recognises yet, pretty-printed as it arrived, so a new step is
// visible (never hidden) before it has its own renderer.

export interface JsonFallbackProps {
    details: Record<string, unknown> | null | undefined;
}

export function JsonFallback({ details }: JsonFallbackProps) {
    return (
        <TraceSection title="Details (no purpose-built view yet)">
            <pre className="overflow-x-auto rounded-xl bg-muted px-3 py-2 font-mono text-[13px]">
                {JSON.stringify(details ?? {}, null, 2)}
            </pre>
        </TraceSection>
    );
}
