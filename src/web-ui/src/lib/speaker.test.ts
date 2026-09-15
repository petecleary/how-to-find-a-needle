import { describe, expect, it } from 'vitest';
import { isPlaceholder, parseSpeaker } from './speaker';

describe('parseSpeaker', () => {
    it('reads the fields, resolves images and keeps the rest as the bio', () => {
        const speaker = parseSpeaker(
            [
                '- **Name:** Ada Lovelace',
                '- **Title:** Analyst',
                '- **Email:** ada@example.com',
                '- **LinkedIn:** linkedin.com/in/ada',
                '- **Photo:** images/ada.jpg',
                '- **LinkedIn QR code:** images/missing.png',
                '',
                'Wrote the first program.',
            ].join('\n'),
            { 'images/ada.jpg': '/assets/ada-123.jpg' },
        );

        expect(speaker).toEqual({
            name: 'Ada Lovelace',
            title: 'Analyst',
            email: 'ada@example.com',
            linkedIn: 'linkedin.com/in/ada',
            photoUrl: '/assets/ada-123.jpg',
            linkedInQrCodeUrl: null,
            bio: 'Wrote the first program.',
        });
    });

    it('recognises placeholders', () => {
        expect(isPlaceholder('[Name]')).toBe(true);
        expect(isPlaceholder('')).toBe(true);
        expect(isPlaceholder('Ada')).toBe(false);
    });
});
