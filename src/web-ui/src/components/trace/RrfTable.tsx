import type { SearchResponse } from '@/api/client';
import { CompatibilityBadge } from '@/components/CompatibilityBadge';
import { ProductCell } from '@/components/trace/ProductCell';
import { ShowMoreButton } from '@/components/trace/ShowMoreButton';
import { TraceSection, traceTableClass } from '@/components/trace/TraceSection';
import { useShowMore } from '@/hooks/useShowMore';
import { productNames, type RrfDetails } from '@/lib/traceDetails';

// RrfTable — shows each item's per-list ranks and the RRF sum, so the audience can check the maths. On
// Stage 5 a last column shows what the rules later decided, so a high fused rank next to "Incompatible"
// makes the point: fusion improves relevance, not correctness.

export interface RrfTableProps {
    details: RrfDetails;
    response: SearchResponse;
}

export function RrfTable({ details, response }: RrfTableProps) {
    const showMore = useShowMore(details.rows, 10);
    const names = productNames(response);
    const compatibilityById = new Map(response.results.map((product) => [product.id, product.compatibility]));
    const hasRuleResults = response.results.some(
        (product) => product.compatibility.status !== 'NotEvaluated',
    );

    const weights = Object.entries(details.weights)
        .map(([list, weight]) => `${list} × ${weight}`)
        .join(' · ');
    const listSizes = Object.entries(details.listSizes)
        .map(([list, size]) => `${list} ${size}`)
        .join(' · ');

    return (
        <div className="flex flex-col gap-4">
            <TraceSection title="Formula">
                <p className="font-mono text-[15px]">
                    {details.formula ?? '—'}, k = {details.k ?? '—'}
                </p>
                <p className="text-sm text-muted-foreground">
                    Weights {weights || '—'} · list sizes {listSizes || '—'} · in both lists{' '}
                    {details.overlap ?? '—'}
                </p>
            </TraceSection>

            <TraceSection title={`Fused order · ${details.rows.length}`}>
                <div className="overflow-x-auto">
                    <table className={traceTableClass}>
                        <thead>
                            <tr>
                                <th scope="col">#</th>
                                <th scope="col">Product</th>
                                <th scope="col">keyword + vector ranks</th>
                                <th scope="col">RRF</th>
                                {hasRuleResults ? <th scope="col">The rules say</th> : null}
                            </tr>
                        </thead>
                        <tbody>
                            {showMore.visible.map((row, index) => {
                                const compatibility = compatibilityById.get(row.id);

                                return (
                                    <tr key={row.id}>
                                        <td className="font-mono">{index + 1}</td>
                                        <td>
                                            <ProductCell id={row.id} productNames={names} />
                                        </td>
                                        <td className="font-mono whitespace-nowrap">{row.expression}</td>
                                        <td className="font-mono font-bold">{row.total}</td>
                                        {hasRuleResults ? (
                                            <td>
                                                {compatibility === undefined ? null : (
                                                    <CompatibilityBadge compatibility={compatibility} />
                                                )}
                                            </td>
                                        ) : null}
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                </div>
                <ShowMoreButton showMore={showMore} />
            </TraceSection>
        </div>
    );
}
