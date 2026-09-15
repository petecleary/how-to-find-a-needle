import { Markdown } from '@/components/Markdown';
import {
    parseStageExplanation,
    stageExplanationMarkdown,
    type StageExplanation as Sections,
} from '@/lib/content';
import {
    stageColourClasses,
    stageGroup,
    stageLabel,
    stageNumber,
    type PipelineStage,
    type TriadGroup,
} from '@/lib/stageGroup';
import { cn } from '@/lib/utils';

// StageExplanation — content/stages/{stage}.md in three columns: what the technique is and how it works,
// what to look for (the talk moment) and how it fails, then its strength, something to try and the decision
// record. Every stage uses the same headings, so the audience learns where to look.

// Sets --step-colour, which Markdown's numbered circles read. Written out in full so Tailwind generates each.
const stepColourByGroup: Record<TriadGroup, string> = {
    search: '[--step-colour:var(--search-ink)]',
    ontology: '[--step-colour:var(--ontology-ink)]',
    pedagogy: '[--step-colour:var(--pedagogy-ink)]',
};

export interface StageExplanationProps {
    stage: PipelineStage;
}

export function StageExplanation({ stage }: StageExplanationProps) {
    const markdown = stageExplanationMarkdown(stage);

    if (markdown === null) {
        return (
            <p className="rounded-card border-2 bg-card px-5 py-4 text-muted-foreground">
                {stageLabel(stage)} has no explanation yet:{' '}
                <code className="font-mono">content/stages/{stage}.md</code>
            </p>
        );
    }

    const parsed = parseStageExplanation(markdown);

    if ('error' in parsed) {
        return (
            <p role="alert" className="rounded-card border-2 border-incompatible-ink bg-card px-5 py-4">
                <code className="font-mono">content/stages/{stage}.md</code>: {parsed.error}
            </p>
        );
    }

    const { sections } = parsed;
    const colours = stageColourClasses(stage);

    return (
        <article
            aria-labelledby="stage-explanation-title"
            className={cn(
                'grid gap-x-10 gap-y-5 text-[17px] lg:grid-cols-3',
                stepColourByGroup[stageGroup(stage)],
            )}
        >
            <div className="flex flex-col gap-4">
                <header>
                    <p className={cn('text-sm font-bold tracking-wide uppercase', colours.ink)}>
                        Stage {stageNumber(stage)}
                    </p>
                    <h2 id="stage-explanation-title" className="text-4xl font-bold">
                        {stageLabel(stage)}
                    </h2>
                </header>
                <Section sections={sections} heading="What it is" />
                <Section sections={sections} heading="How it works" />
            </div>
            <div className="flex flex-col gap-4">
                <Section
                    sections={sections}
                    heading="What to look for"
                    headingClassName={colours.ink}
                    className={cn('rounded-card border-2 bg-card px-5 py-4 text-xl', colours.border)}
                />
                <Section sections={sections} heading="Failure mode" />
            </div>
            <div className="flex flex-col gap-4">
                <Section sections={sections} heading="Strength" />
                <Section sections={sections} heading="Try this" />
                <Section sections={sections} heading="Read the decision" />
            </div>
        </article>
    );
}

interface SectionProps {
    sections: Sections;
    heading: keyof Sections;
    className?: string;
    headingClassName?: string;
}

function Section({ sections, heading, className, headingClassName }: SectionProps) {
    return (
        <section className={cn('flex flex-col gap-1.5', className)}>
            <h3
                className={cn(
                    'text-xs font-bold tracking-wide uppercase',
                    headingClassName ?? 'text-muted-foreground',
                )}
            >
                {heading}
            </h3>
            <Markdown>{sections[heading]}</Markdown>
        </section>
    );
}
