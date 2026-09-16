// Display formats shared by the UI. Prices are GBP in en-GB format (root CLAUDE.md).

const gbp = new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP' });

/** e.g. 1599.99 → "£1,599.99". */
export function formatPrice(price: number): string {
    return gbp.format(price);
}

/** e.g. (1, 50, 60) → "1–50 of 60": which results are shown, out of everything that matched. */
export function formatResultRange(firstRank: number, shownCount: number, totalResults: number): string {
    if (shownCount === 0) {
        return `0 of ${totalResults}`;
    }

    return `${firstRank}–${firstRank + shownCount - 1} of ${totalResults}`;
}

/** e.g. 19.968 → "19.97 ms": the precision the trace timings are worth reading at. */
export function formatMilliseconds(milliseconds: number): string {
    return `${milliseconds.toFixed(2)} ms`;
}

/**
 * e.g. 56.3 → "56 ms", 6231 → "6.2 s": a wait as a person reads it, for LLM timings on the projector. The trace keeps
 * `formatMilliseconds` for its exact values.
 */
export function formatDuration(milliseconds: number): string {
    return milliseconds < 1000 ? `${Math.round(milliseconds)} ms` : `${(milliseconds / 1000).toFixed(1)} s`;
}
