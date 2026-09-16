import { TriangleAlert } from 'lucide-react';

// AnswerWarnings — what validation found wrong with a finished section. LLM output is untrusted: the problems stay on
// screen next to the text instead of being stripped or silently regenerated (ADR-0016). Heuristic checks say so.

export interface AnswerWarningsProps {
    warnings: readonly string[];
}

export function AnswerWarnings({ warnings }: AnswerWarningsProps) {
    if (warnings.length === 0) {
        return null;
    }

    return (
        <ul aria-label="Validation warnings" className="flex flex-col gap-1">
            {warnings.map((warning) => (
                <li key={warning} className="flex gap-2 text-sm text-unknown-ink">
                    <TriangleAlert aria-hidden="true" className="mt-0.5 size-4 flex-none" />
                    <span>{warning}</span>
                </li>
            ))}
        </ul>
    );
}
