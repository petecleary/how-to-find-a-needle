// The Home page's speaker card reads content/speaker.md: `- **Key:** value` lines, then the bio as markdown.
// Values in [square brackets] are placeholders until the real details are filled in. Images live in
// content/images/ and are named by their path under content/, e.g. `images/linkedin-qr.png`.

export interface Speaker {
    name: string;
    title: string;
    email: string;
    linkedIn: string;
    /** A resolved image URL, or `null` for the placeholder. */
    photoUrl: string | null;
    linkedInQrCodeUrl: string | null;
    bio: string;
}

const fieldPattern = /^- \*\*(.+?):\*\*\s*(.*)$/;

/** Reads speaker.md. `imageUrls` maps paths under content/ to the URLs Vite serves them at. */
export function parseSpeaker(markdown: string, imageUrls: Readonly<Record<string, string>>): Speaker {
    const fields = new Map<string, string>();
    const bioLines: string[] = [];

    for (const line of markdown.split('\n')) {
        const match = fieldPattern.exec(line);
        if (match === null) {
            bioLines.push(line);
        } else {
            fields.set((match[1] ?? '').toLowerCase(), (match[2] ?? '').trim());
        }
    }

    const image = (key: string) => imageUrls[fields.get(key) ?? ''] ?? null;

    return {
        name: fields.get('name') ?? '[Name]',
        title: fields.get('title') ?? '[Title]',
        email: fields.get('email') ?? '',
        linkedIn: fields.get('linkedin') ?? '',
        photoUrl: image('photo'),
        linkedInQrCodeUrl: image('linkedin qr code'),
        bio: bioLines.join('\n').trim(),
    };
}

/** True for a value still waiting to be filled in, such as "[Name]". */
export function isPlaceholder(value: string): boolean {
    return value === '' || /^\[.*\]$/.test(value);
}
