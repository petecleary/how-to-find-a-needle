import speakerMarkdown from '../../content/speaker.md?raw';
import { parseSpeaker, type Speaker } from './speaker';

// The speaker's photo and QR code are images in content/images/ or assets/images/, which speaker.md names by
// path. Vite gives each a URL at build time, so parseSpeaker can turn a path into something an <img> can load.
const contentImages = Object.fromEntries([
    ...Object.entries(
        import.meta.glob<string>('../../content/images/*', { query: '?url', import: 'default', eager: true }),
    ).map(([path, url]) => [path.replace(/^.*\/content\//, ''), url]),
    ...Object.entries(
        import.meta.glob<string>('../../assets/images/*', { query: '?url', import: 'default', eager: true }),
    ).map(([path, url]) => [path.replace(/^.*\/assets\//, 'assets/'), url]),
]);

/** Who is giving the talk, from content/speaker.md: shown on Home and on the deck's speaker slides. */
export const speaker: Speaker = parseSpeaker(speakerMarkdown, contentImages);
