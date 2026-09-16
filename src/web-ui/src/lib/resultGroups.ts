import type { ProductResult } from '@/api/client';

// Stage 5 orders its results in three groups (ADR-0013): unflagged first, then out of concept (with the
// target device itself), then Incompatible, each keeping its fused order. The API has already done the
// ordering; the UI only splits the list where the groups change. It never re-ranks and never drops an item.

export interface ConceptGroups {
    /** Unflagged: in a wanted concept, or `NoConcept` when the query matched no concept at all. */
    inConcept: ProductResult[];
    /** Kept, moved down: no category under a wanted concept, and the target device itself. */
    outOfConcept: ProductResult[];
    /** Failed a domain rule against the target device: the near misses. */
    flagged: ProductResult[];
}

export function groupByConcept(
    results: readonly ProductResult[],
    targetProductId: string | null,
): ConceptGroups {
    const groups: ConceptGroups = { inConcept: [], outOfConcept: [], flagged: [] };

    for (const product of results) {
        // An item that is both out of concept and Incompatible is flagged: failing a rule is the stronger signal.
        if (product.compatibility.status === 'Incompatible') {
            groups.flagged.push(product);
        } else if (product.signals.conceptMatch === 'OutOfConcept' || product.id === targetProductId) {
            groups.outOfConcept.push(product);
        } else {
            groups.inConcept.push(product);
        }
    }

    return groups;
}
