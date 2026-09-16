using System.Globalization;

namespace PI.SearchApi.Pipeline.Fusion;

// Stage 4 — Reciprocal Rank Fusion (the fusion step of hybrid search)
//
// What:     RRF(d) = Σᵢ wᵢ / (k + rᵢ(d)), with rᵢ(d) the 1-based rank of d in list i;
//           a list that doesn't contain d contributes 0. k defaults to 60.
// Strength: Keyword scores (ts_rank_cd) and vector distances live on incompatible scales,
//           so they can't be added. Ranks can: no normalisation, no calibration, and the
//           formula fits on a slide.
// Failure:  Ignores *how much* better one result is than the next — a near tie and a
//           landslide fuse the same way. And it improves relevance, not correctness: an
//           incompatible charger both retrievers like still ranks near the top.
// Decision: docs/decisions/0011-hybrid-search-rrf.md
public sealed class ReciprocalRankFusion : IRankFusion
{
    public IReadOnlyList<FusedItem> Fuse(IReadOnlyList<RankedList> lists, int k)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(k, 1);

        // Step 1: each document's 1-based rank in each list (first occurrence wins if a list repeats an ID).
        var ranksById = new Dictionary<string, Dictionary<string, int?>>();

        foreach (var list in lists)
        {
            for (var position = 0; position < list.OrderedIds.Count; position++)
            {
                var id = list.OrderedIds[position];

                if (!ranksById.TryGetValue(id, out var ranks))
                {
                    ranks = lists.ToDictionary(l => l.Name, _ => (int?)null);
                    ranksById[id] = ranks;
                }

                ranks[list.Name] ??= position + 1;
            }
        }

        // Step 2: sum wᵢ / (k + rᵢ) over the lists that contain the document.
        // With k = 60: rank 1 contributes 1/61 = 0.01639 and rank 2 contributes 1/62 = 0.01613 — being 1st
        // rather than 2nd barely matters, but appearing in both lists roughly doubles the score.
        var scored = ranksById.Select(entry =>
        {
            var score = lists
                .Where(list => entry.Value[list.Name] is not null)
                .Sum(list => list.Weight / (k + entry.Value[list.Name]!.Value));

            var bestRank = entry.Value.Values.Where(r => r is not null).Min() ?? int.MaxValue;

            return (Id: entry.Key, Score: score, BestRank: bestRank, Ranks: entry.Value);
        });

        // Step 3: deterministic order — score, then best individual rank, then ID.
        var ordered = scored
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.BestRank)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToList();

        return
        [
            .. ordered.Select((item, index) => new FusedItem(
                item.Id,
                item.Score,
                index + 1,
                item.Ranks,
                Formula(item.Id, item.Score, item.Ranks, lists, k))),
        ];
    }

    // "PROD-0012: 1/(60+2) + 1/(60+1) = 0.03252", or "PROD-0031: 1/(60+7) + — = 0.01493 (not in vector list)".
    private static string Formula(string id, double score, IReadOnlyDictionary<string, int?> ranks, IReadOnlyList<RankedList> lists, int k)
    {
        var terms = lists.Select(list => ranks[list.Name] is { } rank
            ? string.Create(CultureInfo.InvariantCulture, $"{FormatWeight(list.Weight)}/({k}+{rank})")
            : "—");

        var missing = lists.Where(list => ranks[list.Name] is null).Select(list => list.Name).ToList();
        var suffix = missing.Count == 0 ? "" : $" (not in {string.Join(" or ", missing)} list)";

        return string.Create(CultureInfo.InvariantCulture, $"{id}: {string.Join(" + ", terms)} = {score:0.00000}{suffix}");
    }

    private static string FormatWeight(double weight) => weight.ToString("0.##", CultureInfo.InvariantCulture);
}
