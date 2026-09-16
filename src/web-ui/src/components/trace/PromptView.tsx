import { LlmSettings } from '@/components/trace/LlmSettings';
import { TraceSection, traceChipClass, traceTableClass } from '@/components/trace/TraceSection';
import type { PromptDetails } from '@/lib/traceDetails';

// PromptView — the messages sent to the model, exactly as sent (ADR-0016, ADR-0017): the system message with its rules,
// and the user message with the question and the evidence. On Stage 7 it names the prompt used, highlights the
// audience's section of the system message and lists the words offered per concept, so the baseline can be judged.

export interface PromptViewProps {
    details: PromptDetails;
}

export function PromptView({ details }: PromptViewProps) {
    const wordsOffered = Object.entries(details.wordsOffered);

    return (
        <div className="flex flex-col gap-4">
            <div className="flex flex-wrap gap-x-8 gap-y-3">
                <TraceSection title="Prompt files">
                    <div className="flex flex-wrap gap-1.5">
                        {details.promptFiles.map((file) => (
                            <span key={file} className={traceChipClass}>
                                {file}
                            </span>
                        ))}
                    </div>
                </TraceSection>
                {details.applyPedagogy === null ? null : (
                    <TraceSection title="Stage 7">
                        <p>
                            <b>{details.applyPedagogy ? 'Pedagogy prompt' : 'Baseline prompt'}</b>
                            {details.audience === null ? '' : ` · for a ${details.audience}`}
                        </p>
                    </TraceSection>
                )}
                <LlmSettings llm={details.llm} />
            </div>

            {wordsOffered.length > 0 ? (
                <TraceSection title="Words offered for each concept">
                    <table className={traceTableClass}>
                        <thead>
                            <tr>
                                <th>Concept</th>
                                <th>Words, from the ontology's labels</th>
                            </tr>
                        </thead>
                        <tbody>
                            {wordsOffered.map(([notation, words]) => (
                                <tr key={notation}>
                                    <td className="font-mono">{notation}</td>
                                    <td>{words.join(', ')}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </TraceSection>
            ) : null}

            <TraceSection title="System message">
                <PromptText text={details.systemPrompt} highlight={details.audienceGuidance} />
                {details.audienceGuidance === null ? null : (
                    <p className="text-sm text-muted-foreground">
                        Highlighted: the {details.audience} section of pedagogy-audiences.md.
                    </p>
                )}
            </TraceSection>

            <TraceSection title="User message">
                <PromptText text={details.userPrompt} highlight={null} />
            </TraceSection>
        </div>
    );
}

interface PromptTextProps {
    text: string;
    highlight: string | null;
}

function PromptText({ text, highlight }: PromptTextProps) {
    const index = highlight === null || highlight === '' ? -1 : text.indexOf(highlight);

    return (
        <pre className="max-h-[32rem] overflow-auto rounded-xl bg-muted px-4 py-3 font-mono text-[13px] leading-relaxed break-words whitespace-pre-wrap">
            {index === -1 || highlight === null ? (
                text
            ) : (
                <>
                    {text.slice(0, index)}
                    <mark className="rounded-sm bg-pedagogy-tint text-foreground">{highlight}</mark>
                    {text.slice(index + highlight.length)}
                </>
            )}
        </pre>
    );
}
