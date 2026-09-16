// Colour contrast as WCAG 2.2 defines it, so the theme tokens can be checked by a test rather than by eye.
// Text needs 4.5:1 against its background (3:1 for large text), and a focus indicator 3:1 (WCAG 1.4.3, 1.4.11).

/** Relative luminance of an `#rrggbb` colour: 0 for black, 1 for white. */
export function relativeLuminance(hex: string): number {
    const [red, green, blue] = parseHex(hex).map((channel) => {
        const value = channel / 255;
        return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
    }) as [number, number, number];

    return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
}

/** (lighter + 0.05) / (darker + 0.05): 1 for identical colours, 21 for black on white. */
export function contrastRatio(first: string, second: string): number {
    const [lighter, darker] = [relativeLuminance(first), relativeLuminance(second)].sort((a, b) => b - a) as [
        number,
        number,
    ];

    return (lighter + 0.05) / (darker + 0.05);
}

/** The `--name: value;` custom properties declared in one CSS block, e.g. `:root { … }`. */
export function readTokens(css: string, selector: string): Record<string, string> {
    const escaped = selector.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const block = new RegExp(`(?:^|\\n)${escaped}\\s*\\{([^}]*)\\}`).exec(css)?.[1] ?? '';

    return Object.fromEntries(
        [...block.matchAll(/--([\w-]+):\s*([^;]+);/g)].map((match) => [
            match[1] ?? '',
            (match[2] ?? '').trim(),
        ]),
    );
}

/** A token's colour, following `var(--other)` references. */
export function resolveToken(tokens: Readonly<Record<string, string>>, name: string): string {
    const value = tokens[name];
    const reference = value === undefined ? undefined : /^var\(--([\w-]+)\)$/.exec(value)?.[1];

    if (value === undefined) {
        throw new Error(`No --${name} token.`);
    }

    return reference === undefined ? value : resolveToken(tokens, reference);
}

function parseHex(hex: string): [number, number, number] {
    const match = /^#([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})$/i.exec(hex);
    if (match === null) {
        throw new Error(`"${hex}" is not an #rrggbb colour.`);
    }

    return [
        Number.parseInt(match[1] ?? '', 16),
        Number.parseInt(match[2] ?? '', 16),
        Number.parseInt(match[3] ?? '', 16),
    ];
}
