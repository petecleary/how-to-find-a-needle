import { describe, expect, it } from 'vitest';
import css from '../index.css?raw';
import { contrastRatio, readTokens, resolveToken } from './contrast';

// Checks the real theme tokens in index.css, in both themes, for every text-on-background pairing the components
// use (ADR-0014 § Quality bar: contrast ≥ 4.5:1). Disabled controls are exempt in WCAG and not listed.

const light = readTokens(css, ':root');
const dark = { ...light, ...readTokens(css, '.dark') };

const themes = [
    ['light', light],
    ['dark', dark],
] as const;

// [text token, background token]
const textPairs: [string, string][] = [
    ['foreground', 'background'],
    ['foreground', 'card'],
    ['foreground', 'muted'],
    ['muted-foreground', 'background'],
    ['muted-foreground', 'card'],
    ['muted-foreground', 'muted'],
    ['search-ink', 'background'],
    ['search-ink', 'card'],
    ['search-ink', 'search-tint'],
    ['ontology-ink', 'background'],
    ['ontology-ink', 'card'],
    ['ontology-ink', 'ontology-tint'],
    ['pedagogy-ink', 'background'],
    ['pedagogy-ink', 'card'],
    ['pedagogy-ink', 'pedagogy-tint'],
    ['on-search', 'search'],
    ['on-ontology', 'ontology'],
    ['on-pedagogy', 'pedagogy'],
    ['compatible-ink', 'compatible-tint'],
    ['compatible-ink', 'card'],
    ['incompatible-ink', 'incompatible-tint'],
    ['incompatible-ink', 'card'],
    ['unknown-ink', 'unknown-tint'],
    ['unknown-ink', 'card'],
    ['primary-foreground', 'primary'],
];

const cases = themes.flatMap(([theme, tokens]) =>
    textPairs.map(([text, background]) => [theme, text, background, tokens] as const),
);

describe('theme contrast', () => {
    it.each(cases)('%s: --%s on --%s is at least 4.5:1', (_, text, background, tokens) => {
        const ratio = contrastRatio(resolveToken(tokens, text), resolveToken(tokens, background));

        expect(ratio).toBeGreaterThanOrEqual(4.5);
    });

    it.each(themes)('%s: the focus ring stands out from the page and cards by at least 3:1', (_, tokens) => {
        expect(
            contrastRatio(resolveToken(tokens, 'ring'), resolveToken(tokens, 'background')),
        ).toBeGreaterThanOrEqual(3);
        expect(
            contrastRatio(resolveToken(tokens, 'ring'), resolveToken(tokens, 'card')),
        ).toBeGreaterThanOrEqual(3);
    });
});

describe('contrastRatio', () => {
    it('matches WCAG: black on white is 21:1', () => {
        expect(contrastRatio('#000000', '#ffffff')).toBeCloseTo(21, 5);
        expect(contrastRatio('#777777', '#ffffff')).toBeCloseTo(4.48, 2);
    });
});
