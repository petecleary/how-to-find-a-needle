import type { ProductResult } from '@/api/client';
import type { PipelineStage } from './stageGroup';

// Which per-technique signals a result row shows. `signals` keeps each technique's own rank and score
// (ADR-0003), so on Hybrid the audience can see that an item was 1st for keyword but 12th for vector, and
// check how RRF placed it. A score means only what its stage says it means, so each badge says which it is.

export interface SignalBadge {
    /** Short technique name: KW, SIM, VEC or RRF. */
    label: string;
    /** "#3", "0.832", or "–" when that technique didn't retrieve the item. */
    value: string;
    /** Shown on hover and read by screen readers. */
    description: string;
}

export function signalBadges(stage: PipelineStage, product: ProductResult): SignalBadge[] {
    const { signals, score } = product;

    switch (stage) {
        case 'keyword':
            return [
                {
                    label: 'KW',
                    value: formatScore(score, 3),
                    description: 'ts_rank_cd score (BM25-style): higher means the terms matched more closely',
                },
            ];
        case 'vector':
            return [
                {
                    label: 'SIM',
                    value: formatScore(score, 3),
                    description: 'Cosine similarity, 1 − cosine distance: higher means closer in meaning',
                },
            ];
        case 'hybrid':
        case 'ontology':
            return [
                rankBadge('KW', signals.keywordRank, 'keyword'),
                rankBadge('VEC', signals.vectorRank, 'vector'),
                {
                    label: 'RRF',
                    value: formatScore(score, 5),
                    description: 'Reciprocal Rank Fusion: Σ wᵢ / (k + rankᵢ) over the lists the item is in',
                },
            ];
        default:
            // Stage 1 has no score: it filters, then orders by price and ID. Stages 6–7 cite Stage 5's results.
            return [];
    }
}

/** What the stage's `score` is, for the line above the results. */
export function scoreMeaning(stage: PipelineStage): string | null {
    switch (stage) {
        case 'structured':
            return 'no score: ordered by price, then ID';
        case 'keyword':
            return 'score = ts_rank_cd (BM25-style)';
        case 'vector':
            return 'score = cosine similarity';
        case 'hybrid':
        case 'ontology':
            return 'score = RRF sum';
        default:
            return null;
    }
}

function rankBadge(label: string, rank: number | null | undefined, technique: string): SignalBadge {
    if (rank == null) {
        return { label, value: '–', description: `Not retrieved by ${technique} search` };
    }

    return { label, value: `#${rank}`, description: `Rank ${rank} in the ${technique} results` };
}

function formatScore(score: number | null | undefined, digits: number): string {
    return score == null ? '–' : score.toFixed(digits);
}
