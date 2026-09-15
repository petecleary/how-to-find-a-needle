import { Switch } from '@/components/ui/switch';
import type { SearchState } from '@/lib/searchState';
import type { PipelineStage } from '@/lib/stageGroup';

// StageOptions — a stage's before/after switches, on the tab row next to what they change. Stage 5 turns
// synonym expansion and the domain rules on and off, so the audience sees what each one adds (ADR-0013).

export interface StageOptionsProps {
    stage: PipelineStage;
    expandSynonyms: boolean;
    applyConstraints: boolean;
    onChange: (change: Partial<Pick<SearchState, 'expandSynonyms' | 'applyConstraints'>>) => void;
}

export function StageOptions({ stage, expandSynonyms, applyConstraints, onChange }: StageOptionsProps) {
    // TODO(Phase 4): Stage 7's audience picker and "Apply pedagogy" switch (ADR-0017).
    if (stage !== 'ontology') {
        return null;
    }

    return (
        <>
            <label className="flex items-center gap-2 text-[15px] font-bold whitespace-nowrap">
                <Switch
                    checked={expandSynonyms}
                    onCheckedChange={(checked) => onChange({ expandSynonyms: checked })}
                    className="data-[state=checked]:bg-ontology"
                />
                Expand synonyms
            </label>
            <label className="flex items-center gap-2 text-[15px] font-bold whitespace-nowrap">
                <Switch
                    checked={applyConstraints}
                    onCheckedChange={(checked) => onChange({ applyConstraints: checked })}
                    className="data-[state=checked]:bg-ontology"
                />
                Apply constraints
            </label>
        </>
    );
}
