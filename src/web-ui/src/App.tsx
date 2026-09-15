import { CircleCheck, CircleHelp, CircleMinus, CircleX } from 'lucide-react';
import { Logo } from '@/components/Logo';
import { ThemeToggle } from '@/components/ThemeToggle';
import { cn } from '@/lib/utils';
import {
    pipelineStages,
    stageColourClasses,
    stageGroup,
    stageNumber,
    triadGroupClasses,
} from '@/lib/stageGroup';

// TODO(Phase 3): a theme check for step 1 (scaffold and theme). Step 10 replaces it with the router and pages.

const triadGroups = [
    { group: 'search', label: 'Search', question: 'What is relevant?' },
    { group: 'ontology', label: 'Ontology', question: 'How is it related and constrained?' },
    { group: 'pedagogy', label: 'Pedagogy', question: 'How should I explain it?' },
] as const;

const statusSamples = [
    { label: 'Compatible', Icon: CircleCheck, className: 'bg-compatible-tint text-compatible-ink' },
    { label: 'Incompatible', Icon: CircleX, className: 'bg-incompatible-tint text-incompatible-ink' },
    { label: 'Unknown', Icon: CircleHelp, className: 'bg-unknown-tint text-unknown-ink' },
    { label: 'Not evaluated', Icon: CircleMinus, className: 'bg-muted text-muted-foreground' },
];

export function App() {
    return (
        <>
            <header className="flex items-center gap-3 border-b-2 px-6 py-3">
                <Logo size={32} />
                <span className="font-brand text-2xl font-extrabold">How to Find a Needle</span>
                <div className="flex-1" />
                <ThemeToggle />
            </header>

            <main className="mx-auto flex max-w-5xl flex-col gap-8 px-6 py-8">
                <section aria-labelledby="triad-heading" className="flex flex-col gap-4">
                    <h1 id="triad-heading" className="text-xl font-bold">
                        The triad
                    </h1>
                    <div className="grid gap-4 sm:grid-cols-3">
                        {triadGroups.map(({ group, label, question }) => {
                            const colours = triadGroupClasses(group);
                            return (
                                <div
                                    key={group}
                                    className={cn('rounded-card border-2 p-4', colours.border, colours.tint)}
                                >
                                    <p className={cn('font-bold', colours.ink)}>{label}</p>
                                    <p className="text-muted-foreground">{question}</p>
                                    <ol className="mt-3 flex gap-2">
                                        {pipelineStages
                                            .filter((stage) => stageGroup(stage) === group)
                                            .map((stage) => {
                                                const stageColours = stageColourClasses(stage);
                                                return (
                                                    <li
                                                        key={stage}
                                                        title={stage}
                                                        className={cn(
                                                            'flex size-9 items-center justify-center rounded-full font-bold',
                                                            stageColours.fill,
                                                            stageColours.onFill,
                                                        )}
                                                    >
                                                        {stageNumber(stage)}
                                                    </li>
                                                );
                                            })}
                                    </ol>
                                </div>
                            );
                        })}
                    </div>
                </section>

                <section aria-labelledby="status-heading" className="flex flex-col gap-3">
                    <h2 id="status-heading" className="text-xl font-bold">
                        Compatibility status
                    </h2>
                    <ul className="flex flex-wrap gap-2">
                        {statusSamples.map(({ label, Icon, className }) => (
                            <li
                                key={label}
                                className={cn(
                                    'flex items-center gap-1.5 rounded-full px-3 py-1 font-bold',
                                    className,
                                )}
                            >
                                <Icon aria-hidden="true" className="size-4" />
                                {label}
                            </li>
                        ))}
                    </ul>
                </section>

                <section aria-labelledby="type-heading" className="flex flex-col gap-3">
                    <h2 id="type-heading" className="text-xl font-bold">
                        Type
                    </h2>
                    <p>
                        Atkinson Hyperlegible for body text: PROD-0001, l I 1, 0 O. <em>Italic</em> and{' '}
                        <strong>bold</strong>.
                    </p>
                    <pre className="overflow-x-auto rounded-card bg-code p-4 font-mono text-sm">
                        RRF(d) = Σ wᵢ / (k + rᵢ(d)), with k = 60{'\n'}1/(60+2) + 1/(60+1) = 0.03252
                    </pre>
                </section>
            </main>
        </>
    );
}
