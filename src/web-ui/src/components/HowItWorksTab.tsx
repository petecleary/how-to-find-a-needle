import { stageLabel, type PipelineStage } from '@/lib/stageGroup';

// TODO(Phase 3): step 9 renders content/stages/{stage}.md here, with glossary hover cards.

export interface HowItWorksTabProps {
    stage: PipelineStage;
}

/** The stage explanation: what the technique is, its strength and its failure mode. */
export function HowItWorksTab({ stage }: HowItWorksTabProps) {
    return (
        <div className="flex flex-col gap-1 rounded-card border-2 bg-card px-5 py-4">
            <h2 className="text-xl font-bold">{stageLabel(stage)}</h2>
            <p className="text-muted-foreground">
                The explanation of this stage is added with the content in Phase 3 step 9.
            </p>
        </div>
    );
}
