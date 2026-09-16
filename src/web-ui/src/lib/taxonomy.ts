import type { TaxonomyNode } from '@/api/client';

// Lookups in the SKOS concept tree from GET /api/taxonomy. The UI knows no category by name: labels,
// narrower concepts and icons all come from domain-ontology.ttl (ADR-0013).

/** Finds a concept anywhere in the taxonomy tree by its notation (e.g. "laptop-chargers"). */
export function findConcept(taxonomy: readonly TaxonomyNode[], notation: string): TaxonomyNode | null {
    for (const node of taxonomy) {
        if (node.notation === notation) {
            return node;
        }

        const found = findConcept(node.narrower, notation);
        if (found !== null) {
            return found;
        }
    }

    return null;
}

/** Every concept below this one, at any depth: SKOS `narrower`, followed transitively. */
export function narrowerNotations(concept: TaxonomyNode): string[] {
    return concept.narrower.flatMap((child) => [child.notation, ...narrowerNotations(child)]);
}

/** A concept's preferred label, or its notation while the taxonomy is loading or if it isn't found. */
export function conceptLabel(taxonomy: readonly TaxonomyNode[] | null, notation: string): string {
    return (taxonomy === null ? null : findConcept(taxonomy, notation))?.label ?? notation;
}

/**
 * The Lucide icon name for a product, from its first category's `ex:icon` in the TTL. There are no product
 * images (ADR-0014), so the icon is what tells a charger row from a laptop row at a glance.
 */
export function categoryIcon(
    taxonomy: readonly TaxonomyNode[] | null,
    categories: readonly string[],
): string | null {
    const [firstCategory] = categories;
    if (taxonomy === null || firstCategory === undefined) {
        return null;
    }

    return findConcept(taxonomy, firstCategory)?.icon ?? null;
}
