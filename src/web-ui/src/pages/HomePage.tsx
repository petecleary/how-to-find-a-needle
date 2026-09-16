import { ArrowRight } from 'lucide-react';
import { Fragment } from 'react';
import { Link } from 'react-router';
import homeMarkdown from '../../content/home.md?raw';
import speakerMarkdown from '../../content/speaker.md?raw';
import { AppHeader } from '@/components/AppHeader';
import { Markdown } from '@/components/Markdown';
import { SpeakerCard } from '@/components/SpeakerCard';
import { parseSpeaker } from '@/lib/speaker';
import { pipelineStages, triadGroupClasses, triadGroups } from '@/lib/stageGroup';
import { talkStartPath } from '@/lib/talk';
import { cn } from '@/lib/utils';

const contentImages = Object.fromEntries([
    ...Object.entries(
        import.meta.glob<string>('../../content/images/*', { query: '?url', import: 'default', eager: true }),
    ).map(([path, url]) => [path.replace(/^.*\/content\//, ''), url]),
    ...Object.entries(
        import.meta.glob<string>('../../assets/images/*', { query: '?url', import: 'default', eager: true }),
    ).map(([path, url]) => [path.replace(/^.*\/assets\//, 'assets/'), url]),
]);

const speaker = parseSpeaker(speakerMarkdown, contentImages);

/** `/`: the talk's title, its thesis and the triad, the way in to the talk or the demo, and the speaker. */
export function HomePage() {
    return (
        <div className="flex min-h-screen flex-col">
            <AppHeader wordmark="company" />
            <main className="mx-auto grid w-full max-w-7xl flex-1 items-center gap-10 px-6 py-10 lg:grid-cols-[minmax(0,1fr)_24rem]">
                <div className="flex flex-col gap-6">
                    <p className="text-sm font-bold tracking-wide text-ontology-ink uppercase">
                        A talk in {pipelineStages.length} stages
                    </p>
                    <h1 className="font-brand text-6xl leading-[0.95] font-extrabold sm:text-8xl">
                        How to Find a Needle
                    </h1>
                    <Markdown className="max-w-2xl text-2xl">{homeMarkdown}</Markdown>

                    <ol aria-label="The triad" className="flex flex-wrap items-center gap-x-4 gap-y-3">
                        {triadGroups.map(({ group, label, question }, index) => {
                            const colours = triadGroupClasses(group);

                            return (
                                <Fragment key={group}>
                                    {index > 0 ? (
                                        <ArrowRight
                                            aria-hidden="true"
                                            className="size-5 text-muted-foreground"
                                        />
                                    ) : null}
                                    <li className="flex items-center gap-3">
                                        <span
                                            className={cn(
                                                'flex size-14 items-center justify-center rounded-full border-4 text-2xl font-bold',
                                                colours.border,
                                                colours.ink,
                                            )}
                                        >
                                            {index + 1}
                                        </span>
                                        <span className="flex flex-col leading-tight">
                                            <b className="text-xl">{label}</b>
                                            <span className="text-muted-foreground">
                                                {sentenceCase(question)}
                                            </span>
                                        </span>
                                    </li>
                                </Fragment>
                            );
                        })}
                    </ol>

                    <div className="flex flex-wrap gap-3">
                        <Link
                            to={talkStartPath()}
                            className="flex items-center gap-2 rounded-full bg-ontology px-6 py-3 text-xl font-bold text-on-ontology hover:opacity-90"
                        >
                            Start the talk <ArrowRight aria-hidden="true" className="size-5" />
                        </Link>
                        <Link
                            to="/demo"
                            className="rounded-full border-2 border-ontology px-6 py-3 text-xl font-bold text-ontology-ink hover:bg-ontology-tint"
                        >
                            Explore the demo
                        </Link>
                    </div>
                </div>

                <SpeakerCard speaker={speaker} />
            </main>
        </div>
    );
}

function sentenceCase(text: string): string {
    return text.charAt(0).toUpperCase() + text.slice(1);
}
