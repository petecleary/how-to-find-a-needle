// Citations in LLM text (ADR-0016). The model writes `[PROD-0012]` after each product claim; the UI turns each one
// into a chip that shows that product in the evidence set, so every claim can be checked against what the model
// was given. The API validates the finished text; here the IDs are only found and linked.

export const insufficientEvidenceSentinel = 'INSUFFICIENT_EVIDENCE';

/** The Markdown link scheme a citation becomes; `Markdown` renders it with the page's chip. */
export const productLinkScheme = 'product:';

// A bracket holding only product IDs, one or several ("[PROD-0012, PROD-0011]"), and not already a markdown link.
const citationGroup = /\[((?:\s*PROD-\d{4}\s*,?)+)\](?!\()/g;
const productId = /PROD-\d{4}/g;

/** Every product ID cited in square brackets, once each, in order of first appearance. */
export function citedProductIds(text: string): string[] {
    const ids = [...text.matchAll(citationGroup)].flatMap((match) => match[1]?.match(productId) ?? []);
    return [...new Set(ids)];
}

/** Rewrites each citation as a `product:` link, one per ID: `[PROD-0012](product:PROD-0012)`. */
export function linkCitations(markdown: string): string {
    return markdown.replace(citationGroup, (_match, group: string) =>
        (group.match(productId) ?? []).map((id) => `[${id}](${productLinkScheme}${id})`).join(' '),
    );
}

/**
 * The text without the `INSUFFICIENT_EVIDENCE` first line, which the UI shows as a badge instead. While the answer
 * streams, a first line like "INSUFFIC" could be the start of the sentinel, so it is held back until its line ends.
 */
export function withoutSentinel(markdown: string): string {
    const trimmed = markdown.trimStart();
    const newline = trimmed.indexOf('\n');
    const firstLine = (newline === -1 ? trimmed : trimmed.slice(0, newline))
        .trim()
        .replace(/^[*_`]+|[*_`]+$/g, '');

    if (firstLine === insufficientEvidenceSentinel) {
        return newline === -1 ? '' : trimmed.slice(newline + 1).trimStart();
    }

    const mayBeSentinel =
        newline === -1 &&
        /^[A-Z_]{3,}$/.test(firstLine) &&
        insufficientEvidenceSentinel.startsWith(firstLine);

    return mayBeSentinel ? '' : markdown;
}
