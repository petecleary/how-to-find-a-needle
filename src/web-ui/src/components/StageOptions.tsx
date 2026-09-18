import { RefreshCw } from 'lucide-react';
import { Switch } from '@/components/ui/switch';
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group';
import { Button } from '@/components/ui/button';
import { audiences, type SearchState } from '@/lib/searchState';
import type { PipelineStage } from '@/lib/stageGroup';

// StageOptions — a stage's before/after switches, on the tab row next to what they change. Stage 5 turns synonym
// expansion and the domain rules on and off, so the audience sees what each one adds (ADR-0013). Stage 7 chooses the
// audience and switches the pedagogy prompt for the baseline, which changes only the system prompt (ADR-0017).

export type StageOptionValues = Pick<
    SearchState,
    'expandSynonyms' | 'applyConstraints' | 'audience' | 'applyPedagogy'
>;

export interface StageOptionsProps extends StageOptionValues {
    stage: PipelineStage;
    onChange: (change: Partial<StageOptionValues>) => void;
    onRefreshAnswer?: () => void;
}

export function StageOptions({
    stage,
    expandSynonyms,
    applyConstraints,
    audience,
    applyPedagogy,
    onChange,
    onRefreshAnswer,
}: StageOptionsProps) {
    if (stage === 'ontology') {
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

    // Only Stage 7 reads the audience, so the picker appears only there (its trace lists the audience it used).
    if (stage === 'rag') {
        return (
            <Button
                type="button"
                variant="outline"
                size="icon"
                aria-label="Refresh answer"
                title="Refresh answer"
                onClick={onRefreshAnswer}
                className="size-8 rounded-full border-2"
            >
                <RefreshCw aria-hidden="true" className="size-4" />
            </Button>
        );
    }

    if (stage !== 'pedagogy') {
        return null;
    }

    return (
        <>
            <div className="flex items-center gap-2">
                <span id="audience-label" className="text-[15px] text-muted-foreground">
                    Audience
                </span>
                <ToggleGroup
                    type="single"
                    spacing={1}
                    aria-labelledby="audience-label"
                    value={audience}
                    onValueChange={(value) => {
                        const next = audiences.find((candidate) => candidate === value);
                        // Clicking the chosen audience again would clear the value; an audience is always required.
                        if (next !== undefined) {
                            onChange({ audience: next });
                        }
                    }}
                    className="rounded-full border-2 p-0.5"
                >
                    {audiences.map((candidate) => (
                        <ToggleGroupItem
                            key={candidate}
                            value={candidate}
                            className="h-8 rounded-full px-3 text-[15px] font-bold capitalize data-[state=on]:bg-pedagogy data-[state=on]:text-on-pedagogy"
                        >
                            {candidate}
                        </ToggleGroupItem>
                    ))}
                </ToggleGroup>
            </div>
            <label className="flex items-center gap-2 text-[15px] font-bold whitespace-nowrap">
                <Switch
                    checked={applyPedagogy}
                    onCheckedChange={(checked) => onChange({ applyPedagogy: checked })}
                    className="data-[state=checked]:bg-pedagogy"
                />
                Apply pedagogy
            </label>
        </>
    );
}
