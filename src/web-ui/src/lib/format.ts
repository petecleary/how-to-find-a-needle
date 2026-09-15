// Display formats shared by the UI. Prices are GBP in en-GB format (root CLAUDE.md).

const gbp = new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP' });

/** e.g. 1599.99 → "£1,599.99". */
export function formatPrice(price: number): string {
    return gbp.format(price);
}

/** e.g. 19.968 → "19.97 ms": the precision the trace timings are worth reading at. */
export function formatMilliseconds(milliseconds: number): string {
    return `${milliseconds.toFixed(2)} ms`;
}
