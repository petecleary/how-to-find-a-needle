using System.Text.Json;

namespace PI.SearchApi.Contracts;

/// <summary>One product in a search response, with the evidence for where it ranked (ADR-0003).</summary>
public sealed record ProductResult
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Brand { get; init; }

    /// <summary>Taxonomy notations; the first supplies the UI icon.</summary>
    public required IReadOnlyList<string> Categories { get; init; }

    /// <summary>Price in GBP.</summary>
    public required decimal Price { get; init; }

    public required IReadOnlyDictionary<string, JsonElement> Specs { get; init; }

    /// <summary>
    /// Means something different in each stage, and the trace says what: null in Stage 1,
    /// ts_rank_cd in Stage 2, cosine similarity in Stage 3, the RRF sum in Stages 4 and 6.
    /// </summary>
    public double? Score { get; init; }

    /// <summary>Each technique's own rank and score, kept side by side for badges.</summary>
    public required CandidateSignals Signals { get; init; }

    public required CompatibilityResult Compatibility { get; init; }
}
