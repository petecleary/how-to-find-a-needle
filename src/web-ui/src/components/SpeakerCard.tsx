import { Mail, UserRound } from 'lucide-react';
import { Markdown } from '@/components/Markdown';
import { isPlaceholder, type Speaker } from '@/lib/speaker';

export interface SpeakerCardProps {
    speaker: Speaker;
}

// lucide-react has no brand icons, so the GitHub mark is this one small inline SVG.
function GithubIcon({ className }: { className?: string }) {
    return (
        <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true" className={className}>
            <path d="M12 .5C5.65.5.5 5.65.5 12c0 5.08 3.29 9.39 7.86 10.91.57.1.78-.25.78-.55 0-.27-.01-1-.02-1.96-3.2.7-3.88-1.54-3.88-1.54-.52-1.33-1.28-1.69-1.28-1.69-1.04-.71.08-.7.08-.7 1.15.08 1.76 1.18 1.76 1.18 1.02 1.75 2.68 1.25 3.34.95.1-.74.4-1.25.72-1.54-2.55-.29-5.24-1.28-5.24-5.7 0-1.26.45-2.29 1.18-3.09-.12-.29-.51-1.46.11-3.05 0 0 .97-.31 3.18 1.18a11 11 0 0 1 5.79 0c2.2-1.49 3.17-1.18 3.17-1.18.63 1.59.24 2.76.12 3.05.74.8 1.18 1.83 1.18 3.09 0 4.43-2.7 5.4-5.27 5.69.42.36.78 1.08.78 2.17 0 1.57-.01 2.83-.01 3.21 0 .3.21.66.79.55A10.52 10.52 0 0 0 23.5 12C23.5 5.65 18.35.5 12 .5Z" />
        </svg>
    );
}

/** Who is giving the talk, from content/speaker.md, with a LinkedIn QR code for the audience to scan. */
export function SpeakerCard({ speaker }: SpeakerCardProps) {
    const githubUrl = speaker.github.startsWith('http') ? speaker.github : `https://${speaker.github}`;

    return (
        <aside aria-label="Speaker" className="flex flex-col gap-4 rounded-card border-2 bg-card p-6">
            <div className="flex items-center gap-4">
                {speaker.photoUrl === null ? (
                    <span className="flex size-20 flex-none items-center justify-center rounded-full border-4 border-ontology bg-muted">
                        <UserRound aria-hidden="true" className="size-9 text-muted-foreground" />
                    </span>
                ) : (
                    <img
                        src={speaker.photoUrl}
                        alt={speaker.name}
                        className="size-20 flex-none rounded-full border-4 border-ontology object-cover"
                    />
                )}
                <div>
                    <p className="text-2xl font-bold">{speaker.name}</p>
                    <p className="text-muted-foreground">{speaker.title}</p>
                </div>
            </div>

            {speaker.bio === '' ? null : <Markdown className="text-muted-foreground">{speaker.bio}</Markdown>}

            <ul className="flex flex-col gap-1.5">
                <li className="flex items-center gap-2">
                    <Mail aria-hidden="true" className="size-5 text-muted-foreground" />
                    {isPlaceholder(speaker.email) ? (
                        <span>{speaker.email}</span>
                    ) : (
                        <a href={`mailto:${speaker.email}`} className="underline underline-offset-4">
                            {speaker.email}
                        </a>
                    )}
                </li>
                {isPlaceholder(speaker.github) ? null : (
                    <li className="flex items-center gap-2">
                        <GithubIcon className="size-5 text-muted-foreground" />
                        <a href={githubUrl} className="underline underline-offset-4">
                            {speaker.github}
                        </a>
                    </li>
                )}
            </ul>   

            <div className="flex items-center justify-center gap-4 pt-4">
                {speaker.linkedInQrCodeUrl === null ? (
                    <span className="flex size-28 flex-none items-center justify-center rounded-lg border-2 border-dashed text-sm text-muted-foreground">
                        QR code
                    </span>
                ) : (
                    <img
                        src={speaker.linkedInQrCodeUrl}
                        alt={`QR code linking to ${speaker.name} on LinkedIn`}
                        className="size-64 flex-none rounded-lg"
                    />
                )}
            </div>
        </aside>
    );
}
