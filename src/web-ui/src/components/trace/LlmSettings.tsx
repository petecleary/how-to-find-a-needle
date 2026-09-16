import { TraceSection, traceChipClass } from '@/components/trace/TraceSection';
import type { LlmInfo } from '@/lib/traceDetails';

// LlmSettings — which model produced the text and how it was asked (ADR-0015): provider, model, endpoint host and the
// sampling and reasoning settings sent. Never the API key: the API builds this from an allow-list of fields.

export interface LlmSettingsProps {
    llm: LlmInfo | null;
}

export function LlmSettings({ llm }: LlmSettingsProps) {
    if (llm === null) {
        return null;
    }

    return (
        <TraceSection title="Model">
            <p>
                <b>{llm.model}</b> via {llm.provider}
                {llm.endpointHost === null ? '' : ` at ${llm.endpointHost}`}
            </p>
            <div className="flex flex-wrap gap-1.5">
                {Object.entries(llm.settings).map(([name, value]) => (
                    <span key={name} className={traceChipClass}>
                        {name} = {value === null ? 'not sent' : String(value)}
                    </span>
                ))}
            </div>
        </TraceSection>
    );
}
