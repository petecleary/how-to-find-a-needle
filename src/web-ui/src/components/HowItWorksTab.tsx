import { StageExplanation } from '@/components/StageExplanation';
import type { PipelineStage } from '@/lib/stageGroup';

export interface HowItWorksTabProps {
    stage: PipelineStage;
}

/** The stage explanation: what the technique is, how it works, its strength and its failure mode. */
export function HowItWorksTab({ stage }: HowItWorksTabProps) {
    return <StageExplanation stage={stage} />;
}
