using System.Diagnostics;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Fusion;
using PI.SearchApi.Pipeline.Keyword;
using PI.SearchApi.Pipeline.Vector;

namespace PI.SearchApi.Pipeline.Hybrid;

// Stage 4 — Hybrid search (Reciprocal Rank Fusion)
//
// What:     Runs keyword and vector search, then fuses their rankings:
//           RRF(d) = Σ wᵢ / (k + rᵢ(d)), with k = 60.
// Strength: Keeps exact-term hits (keyword) and meaning hits (vector) without
//           comparing their incompatible raw scores; only ranks are fused.
// Failure:  Still has no idea what "compatible" means: a near miss that both
//           retrievers like ranks near the top.
// Decision: docs/decisions/0011-hybrid-search-rrf.md
public sealed class HybridSearch(IKeywordSearch keywordSearch, IVectorSearch vectorSearch, IRankFusion fusion) : IHybridSearch
{
    public async Task<StageResult> SearchAsync(
        SearchRequest request,
        KeywordExpansion? keywordExpansion,
        string? embeddingText,
        CancellationToken ct)
    {
        using var activity = PipelineTelemetry.Source.StartActivity("Stage 4: hybrid search");

        // Both retrievers run at the same time, each on its own pooled connection, and each retrieves
        // candidateDepth items with the same filters — so fusion sees deep lists, not two first pages.
        var keywordTask = keywordSearch.SearchAsync(request, keywordExpansion, ct);
        var vectorTask = vectorSearch.SearchAsync(request, embeddingText, ct);
        await Task.WhenAll(keywordTask, vectorTask);

        var keyword = await keywordTask;
        var vector = await vectorTask;

        var start = Stopwatch.GetTimestamp();

        RankedList[] lists =
        [
            new("keyword", request.Options.KeywordWeight, [.. keyword.Candidates.Select(c => c.Product.Id)]),
            new("vector", request.Options.VectorWeight, [.. vector.Candidates.Select(c => c.Product.Id)]),
        ];

        var fused = fusion.Fuse(lists, request.Options.RrfK);

        // Merge each product's signals from both retrievers onto one candidate.
        var keywordById = keyword.Candidates.ToDictionary(c => c.Product.Id);
        var vectorById = vector.Candidates.ToDictionary(c => c.Product.Id);

        var candidates = fused.Select(item =>
        {
            keywordById.TryGetValue(item.Id, out var fromKeyword);
            vectorById.TryGetValue(item.Id, out var fromVector);

            var signals = new CandidateSignals
            {
                KeywordRank = fromKeyword?.Signals.KeywordRank,
                KeywordScore = fromKeyword?.Signals.KeywordScore,
                VectorRank = fromVector?.Signals.VectorRank,
                VectorDistance = fromVector?.Signals.VectorDistance,
                FusedRank = item.FusedRank,
            };

            var product = (fromKeyword ?? fromVector)!.Product; // every fused ID came from one of the two lists

            return new Candidate(product, Math.Round(item.Score, 5), signals, CompatibilityResult.NotEvaluated);
        }).ToList();

        var overlap = keywordById.Keys.Count(vectorById.ContainsKey);

        var fusionStep = new TraceStep
        {
            Stage = "hybrid",
            Title = $"Reciprocal Rank Fusion (k={request.Options.RrfK})",
            DurationMs = PipelineTelemetry.ElapsedMs(start),
            Details = new Dictionary<string, object?>
            {
                ["formula"] = "RRF(d) = Σ wᵢ / (k + rᵢ(d))",
                ["k"] = request.Options.RrfK,
                ["weights"] = new { keyword = request.Options.KeywordWeight, vector = request.Options.VectorWeight },
                ["listSizes"] = new { keyword = keyword.Candidates.Count, vector = vector.Candidates.Count },
                ["overlap"] = overlap,
                ["formulas"] = fused.Select(item => item.Formula).ToList(),
                ["optionsUsed"] = new { request.Options.CandidateDepth, request.Options.RrfK, request.Options.KeywordWeight, request.Options.VectorWeight },
            },
            Notes =
            [
                "Fuse ranks, not scores: ts_rank_cd and cosine distance come from different universes and can't be added.",
                $"With k = {request.Options.RrfK}, being 1st rather than 2nd barely matters; appearing in both lists matters a lot.",
                "RRF ignores how far apart two results were: a near tie and a landslide fuse the same way.",
                "Fusion improves relevance, not correctness: an incompatible item both retrievers like still ranks well.",
            ],
        };

        // Trace steps accumulate: the retrievers' steps first, then this fusion step (ADR-0004).
        return new StageResult(candidates, [.. keyword.Trace, .. vector.Trace, fusionStep]);
    }
}
