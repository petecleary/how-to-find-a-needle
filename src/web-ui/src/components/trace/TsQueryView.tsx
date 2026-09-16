import { ProductCell } from '@/components/trace/ProductCell';
import { ShowMoreButton } from '@/components/trace/ShowMoreButton';
import { TraceSection, traceChipClass, traceTableClass } from '@/components/trace/TraceSection';
import { useShowMore } from '@/hooks/useShowMore';
import type { KeywordDetails } from '@/lib/traceDetails';

// TsQueryView — the query as Postgres understood it: stemmed lexemes joined by & (AND), | (OR) and <->
// (followed by), then each match with its ts_rank_cd score and the lexemes it contained. An empty match list
// next to an all-AND tsquery shows why one word the catalogue never uses returns nothing.

export interface TsQueryViewProps {
    details: KeywordDetails;
    productNames: Map<string, string>;
}

export function TsQueryView({ details, productNames }: TsQueryViewProps) {
    const showMore = useShowMore(details.matches, 10);

    return (
        <div className="flex flex-col gap-4">
            <TraceSection title={details.isExpanded ? 'tsquery, with ontology expansion' : 'tsquery'}>
                <pre className="overflow-x-auto rounded-xl bg-muted px-3 py-2 font-mono text-[15px] whitespace-pre-wrap">
                    {details.tsquery ?? '—'}
                </pre>
                {details.tsqueryExpression === null ? null : (
                    <p className="text-sm text-muted-foreground">
                        Built with <code className="font-mono break-all">{details.tsqueryExpression}</code>
                    </p>
                )}
                {details.ranking === null ? null : (
                    <p className="text-sm text-muted-foreground">Ranked by {details.ranking}</p>
                )}
            </TraceSection>

            <TraceSection title={`Matches · ${details.matches.length}`}>
                {details.matches.length === 0 ? (
                    <p className="text-unknown-ink">
                        No product contains every lexeme, so keyword search returns nothing.
                    </p>
                ) : (
                    <>
                        <div className="overflow-x-auto">
                            <table className={traceTableClass}>
                                <thead>
                                    <tr>
                                        <th scope="col">#</th>
                                        <th scope="col">Product</th>
                                        <th scope="col">ts_rank_cd</th>
                                        <th scope="col">Lexemes found</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {showMore.visible.map((match) => (
                                        <tr key={match.id}>
                                            <td className="font-mono">{match.rank ?? '—'}</td>
                                            <td>
                                                <ProductCell id={match.id} productNames={productNames} />
                                            </td>
                                            <td className="font-mono">{match.score?.toFixed(5) ?? '—'}</td>
                                            <td>
                                                <span className="flex flex-wrap gap-1">
                                                    {match.matchedTerms.map((term) => (
                                                        <span key={term} className={traceChipClass}>
                                                            {term}
                                                        </span>
                                                    ))}
                                                </span>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                        <ShowMoreButton showMore={showMore} />
                    </>
                )}
            </TraceSection>
        </div>
    );
}
