using System.Globalization;

namespace PI.SearchApi.Pipeline.Keyword;

/// <summary>
/// Builds the tsquery expression for keyword search, always from parameterised fragments (ADR-0008).
/// </summary>
/// <remarks>
/// Plain Stage 2 uses <c>websearch_to_tsquery</c>, which accepts natural input and never throws on
/// user syntax. It can't express "any of these phrases, AND the rest", so Stage 5's expanded query is
/// assembled instead: <c>(phraseto_tsquery(@g0t0) || phraseto_tsquery(@g0t1)) &amp;&amp; websearch_to_tsquery(@rest)</c>.
/// <c>||</c> is tsquery OR, <c>&amp;&amp;</c> is tsquery AND, and phraseto_tsquery keeps multi-word
/// labels like "power brick" together as a phrase.
/// </remarks>
public static class TsQueryBuilder
{
    /// <summary>The expression for an unexpanded query: <c>websearch_to_tsquery('english', @query)</c>.</summary>
    public static (string Expression, SqlParameters Parameters) ForQuery(string query) =>
        ("websearch_to_tsquery('english', @query)", new SqlParameters().Add("query", query));

    /// <summary>The expression for an ontology-expanded query. Falls back to <paramref name="originalQuery"/> if the expansion is empty.</summary>
    public static (string Expression, SqlParameters Parameters) ForExpansion(KeywordExpansion expansion, string originalQuery)
    {
        var parts = new List<string>();
        var parameters = new SqlParameters();

        for (var g = 0; g < expansion.OrGroups.Count; g++)
        {
            var terms = expansion.OrGroups[g];
            if (terms.Count == 0)
            {
                continue;
            }

            var alternatives = new List<string>();

            for (var t = 0; t < terms.Count; t++)
            {
                var name = string.Create(CultureInfo.InvariantCulture, $"g{g}t{t}");
                alternatives.Add($"phraseto_tsquery('english', @{name})");
                parameters.Add(name, terms[t]);
            }

            parts.Add("(" + string.Join(" || ", alternatives) + ")");
        }

        if (!string.IsNullOrWhiteSpace(expansion.RemainingText))
        {
            parts.Add("websearch_to_tsquery('english', @rest)");
            parameters.Add("rest", expansion.RemainingText);
        }

        return parts.Count == 0
            ? ForQuery(originalQuery)
            : (string.Join(" && ", parts), parameters);
    }
}
