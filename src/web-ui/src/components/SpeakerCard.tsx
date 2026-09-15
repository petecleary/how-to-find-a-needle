import { Link2, Mail, UserRound } from 'lucide-react';
import { Markdown } from '@/components/Markdown';
import { isPlaceholder, type Speaker } from '@/lib/speaker';

export interface SpeakerCardProps {
    speaker: Speaker;
}

/** Who is giving the talk, from content/speaker.md, with a LinkedIn QR code for the audience to scan. */
export function SpeakerCard({ speaker }: SpeakerCardProps) {
    const linkedInUrl = speaker.linkedIn.startsWith('http')
        ? speaker.linkedIn
        : `https://${speaker.linkedIn}`;

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
                <li className="flex items-center gap-2">
                    <Link2 aria-hidden="true" className="size-5 text-muted-foreground" />
                    {isPlaceholder(speaker.linkedIn) ? (
                        <span>{speaker.linkedIn}</span>
                    ) : (
                        <a href={linkedInUrl} className="underline underline-offset-4">
                            {speaker.linkedIn}
                        </a>
                    )}
                </li>
            </ul>

            <div className="flex items-center gap-4 border-t-2 pt-4">
                {speaker.linkedInQrCodeUrl === null ? (
                    <span className="flex size-28 flex-none items-center justify-center rounded-lg border-2 border-dashed text-sm text-muted-foreground">
                        QR code
                    </span>
                ) : (
                    <img
                        src={speaker.linkedInQrCodeUrl}
                        alt={`QR code linking to ${speaker.name} on LinkedIn`}
                        className="size-28 flex-none rounded-lg"
                    />
                )}
                <p className="text-muted-foreground">Scan to connect on LinkedIn</p>
            </div>
        </aside>
    );
}
