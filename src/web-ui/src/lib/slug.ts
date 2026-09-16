/**
 * A heading's anchor, the way GitHub makes it, so links such as `0014-web-ui-architecture.md#visual-design`
 * work in the UI as they do on GitHub: lower case, punctuation removed, each space a hyphen.
 * "Stage 1 — Structured search" → "stage-1--structured-search".
 */
export function slugify(text: string): string {
    return text
        .trim()
        .toLowerCase()
        .replace(/[^\p{L}\p{N}\s_-]/gu, '')
        .replace(/\s/g, '-');
}
