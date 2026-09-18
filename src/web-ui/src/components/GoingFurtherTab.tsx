import { Markdown } from '@/components/Markdown';
import { goingFurtherMarkdown } from '@/lib/content';
import {
    stageColourClasses,
    stageGroup,
    stageLabel,
    type PipelineStage,
    type TriadGroup,
} from '@/lib/stageGroup';
import { cn } from '@/lib/utils';

// GoingFurtherTab — shows where a stage's technique goes next: the topics the talk names but doesn't build
// (ADR-0018). Prose and glossary links only, from content/going-further/{stage}.md, so the tab costs no
// request and can't drift from the demo. It answers a question from the floor without leaving the stage.

// Sets --step-colour, which Markdown's numbered circles read. Written out in full so Tailwind generates each.
const stepColourByGroup: Record<TriadGroup, string> = {
    search: '[--step-colour:var(--search-ink)]',
    ontology: '[--step-colour:var(--ontology-ink)]',
    pedagogy: '[--step-colour:var(--pedagogy-ink)]',
};

export interface GoingFurtherTabProps {
    stage: PipelineStage;
}

export function GoingFurtherTab({ stage }: GoingFurtherTabProps) {
    const markdown = goingFurtherMarkdown(stage);

    if (markdown === null) {
        return (
            <p className="rounded-card border-2 bg-card px-5 py-4 text-muted-foreground">
                {stageLabel(stage)} has nowhere further to go in this talk:{' '}
                <code className="font-mono">content/going-further/{stage}.md</code>
            </p>
        );
    }

    const colours = stageColourClasses(stage);

    return (
        <article
            aria-labelledby="going-further-title"
            className={cn('flex flex-col gap-4 text-[17px]', stepColourByGroup[stageGroup(stage)])}
        >
            <h2
                id="going-further-title"
                className={cn('text-sm font-bold tracking-wide uppercase', colours.ink)}
            >
                Where this goes next
            </h2>
            {/*
             * Two newspaper columns from `lg`, so a projector shows a whole topic instead of a narrow
             * ribbon of text. Blocks don't split across the gap, and a heading never ends a column with
             * its paragraph in the next one. Below `lg` the columns collapse to one, and the flow is
             * the reading order either way.
             */}
            <Markdown className="block max-w-3xl gap-4 lg:max-w-none lg:columns-2 lg:gap-10 [&>*]:break-inside-avoid [&>*+*]:mt-4 [&>h3]:break-after-avoid">
                {markdown}
            </Markdown>
        </article>
    );
}
