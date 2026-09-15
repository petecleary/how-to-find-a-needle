import type { ConceptMatch } from '@/api/client';
import { ConceptBadge } from '@/components/ConceptBadge';
import { ProductCell } from '@/components/trace/ProductCell';
import { ShowMoreButton } from '@/components/trace/ShowMoreButton';
import { TraceSection, traceChipClass, traceTableClass } from '@/components/trace/TraceSection';
import { useShowMore } from '@/hooks/useShowMore';
import type { Classification, ClassificationDetails } from '@/lib/traceDetails';

// ClassificationTable — why each candidate is in or out of concept: the chain of broader concepts from its
// category up to a wanted concept (skos:broader*). For an out-of-concept item, the chains show where its
// categories really sit: the cordless phone battery is under Telephony, not Power tools.

export interface ClassificationTableProps {
    details: ClassificationDetails;
    productNames: Map<string, string>;
}

export function ClassificationTable({ details, productNames }: ClassificationTableProps) {
    const showMore = useShowMore(details.classifications, 10);

    return (
        <div className="flex flex-col gap-4">
            <p className="flex flex-wrap items-center gap-1.5 text-sm">
                <span className="text-muted-foreground">Wanted</span>
                {details.wantedConcepts.map((notation) => (
                    <span
                        key={notation}
                        className={`${traceChipClass} border-transparent bg-ontology-tint text-ontology-ink`}
                    >
                        {notation}
                    </span>
                ))}
                {details.wantedConcepts.length === 0 ? <span>none, so every item is NoConcept</span> : null}
            </p>

            <TraceSection title={`Candidates · ${details.classifications.length}`}>
                <div className="overflow-x-auto">
                    <table className={traceTableClass}>
                        <thead>
                            <tr>
                                <th scope="col">Product</th>
                                <th scope="col">Match</th>
                                <th scope="col">Broader chain</th>
                            </tr>
                        </thead>
                        <tbody>
                            {showMore.visible.map((item) => (
                                <tr key={item.id}>
                                    <td>
                                        <ProductCell id={item.id} productNames={productNames} />
                                    </td>
                                    <td>
                                        <ConceptBadge conceptMatch={toConceptMatch(item.conceptMatch)} />
                                    </td>
                                    <td className="font-mono text-[13px]">
                                        {chains(item).map((chain) => (
                                            <span key={chain} className="block">
                                                {chain}
                                            </span>
                                        ))}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
                <ShowMoreButton showMore={showMore} />
            </TraceSection>
        </div>
    );
}

// In concept: the one chain that reached a wanted concept. Otherwise: every category's chain to the top.
function chains(item: Classification): string[] {
    if (item.broaderChain.length > 0) {
        return [item.broaderChain.join(' › ')];
    }

    return Object.values(item.categoryChains).map((chain) => chain.join(' › '));
}

function toConceptMatch(value: string | null): ConceptMatch | undefined {
    return value === 'InConcept' || value === 'OutOfConcept' || value === 'NoConcept' ? value : undefined;
}
