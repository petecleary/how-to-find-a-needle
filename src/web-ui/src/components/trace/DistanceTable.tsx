import { ProductCell } from '@/components/trace/ProductCell';
import { ShowMoreButton } from '@/components/trace/ShowMoreButton';
import { TraceSection, traceTableClass } from '@/components/trace/TraceSection';
import { useShowMore } from '@/hooks/useShowMore';
import type { DistanceDetails } from '@/lib/traceDetails';

// DistanceTable — each candidate's cosine distance to the query vector, nearest first, with its similarity
// (1 − distance) as a bar. There is no threshold: the last neighbour is returned however far away it is.

export interface DistanceTableProps {
    details: DistanceDetails;
    productNames: Map<string, string>;
}

export function DistanceTable({ details, productNames }: DistanceTableProps) {
    const showMore = useShowMore(details.distances, 10);

    return (
        <div className="flex flex-col gap-4">
            <p className="text-sm text-muted-foreground">
                {details.metric ?? '—'} · model{' '}
                <code className="font-mono">{details.activeModel ?? '—'}</code> · HNSW ef_search{' '}
                <span className="font-mono">{details.efSearch ?? '—'}</span>
            </p>

            <TraceSection title={`Nearest neighbours · ${details.distances.length}`}>
                <div className="overflow-x-auto">
                    <table className={traceTableClass}>
                        <thead>
                            <tr>
                                <th scope="col">#</th>
                                <th scope="col">Product</th>
                                <th scope="col">Distance</th>
                                <th scope="col">Similarity</th>
                            </tr>
                        </thead>
                        <tbody>
                            {showMore.visible.map((item) => (
                                <tr key={item.id}>
                                    <td className="font-mono">{item.rank ?? '—'}</td>
                                    <td>
                                        <ProductCell id={item.id} productNames={productNames} />
                                    </td>
                                    <td className="font-mono">{item.distance?.toFixed(5) ?? '—'}</td>
                                    <td>
                                        <span className="flex items-center gap-2">
                                            <span className="font-mono">
                                                {item.similarity?.toFixed(5) ?? '—'}
                                            </span>
                                            <span
                                                aria-hidden="true"
                                                className="h-2 w-24 rounded-full bg-search-tint"
                                            >
                                                <span
                                                    className="block h-2 rounded-full bg-search"
                                                    style={{
                                                        width: `${Math.max(0, item.similarity ?? 0) * 100}%`,
                                                    }}
                                                />
                                            </span>
                                        </span>
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
