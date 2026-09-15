import { BookOpen } from 'lucide-react';
import { Markdown } from '@/components/Markdown';
import { stageColourClasses, stageLabel, type PipelineStage } from '@/lib/stageGroup';
import { cn } from '@/lib/utils';

export interface TalkCaptionProps {
    stage: PipelineStage;
    /** The talk step's one-line caption, from its markdown file. */
    markdown: string;
    onOpenHowItWorks: () => void;
}

/** The talk step's point in one sentence, above the tabs, in the stage's triad colour. */
export function TalkCaption({ stage, markdown, onOpenHowItWorks }: TalkCaptionProps) {
    const colours = stageColourClasses(stage);

    return (
        <div
            className={cn(
                'flex flex-wrap items-center gap-x-3 gap-y-1 rounded-card px-4 py-2 text-lg',
                colours.tint,
            )}
        >
            <BookOpen aria-hidden="true" className={cn('size-5 flex-none', colours.ink)} />
            <b className={colours.ink}>{stageLabel(stage)}</b>
            <Markdown className="min-w-0 flex-1">{markdown}</Markdown>
            <button
                type="button"
                onClick={onOpenHowItWorks}
                className={cn('font-bold hover:underline', colours.ink)}
            >
                More in How it works
            </button>
        </div>
    );
}
