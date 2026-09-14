using System.Text.Json;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Ontology;

namespace PI.SearchApi.Pipeline;

// Shared structured pre-filters (ADR-0007)
//
// What:     Turns a request's filters into WHERE fragments and parameters. Every stage that
//           queries products uses this builder, so "brand = Brakk" means the same thing in
//           Stage 1 as it does under Stage 3's vector ordering or Stage 6's rule checks.
// Strength: Filters narrow first and exactly; ranking only orders what's left. Values are always
//           parameters, and fragments come from a fixed list, so the dynamic SQL is still safe.
// Failure:  Only understands attributes someone has already structured; it can't turn "something
//           to charge my laptop" into a filter. That's what the later stages are for.
// Decision: docs/adr/0007-structured-search.md
public sealed class SqlFilterBuilder(IOntology ontology)
{
    /// <summary>Builds the filter SQL. An empty <see cref="SearchFilters"/> produces no conditions.</summary>
    public SqlFilter Build(SearchFilters filters)
    {
        var conditions = new List<string>();
        var parameters = new SqlParameters();
        var details = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(filters.Brand))
        {
            // lower() on both sides makes the match case-insensitive. At this catalog size that's
            // fine; a large catalog would index lower(brand), or store brands already normalised.
            conditions.Add("lower(brand) = lower(@brand)");
            parameters.Add("brand", filters.Brand);
        }

        if (filters.Categories is { Count: > 0 } categories)
        {
            // Broader concepts include their narrower ones: "chargers" becomes chargers, laptop-chargers,
            // usb-c-pd-chargers and phone-chargers. The ontology decides that, not a hard-coded list.
            var expansion = categories.ToDictionary(
                c => c,
                c => ontology.NarrowerOrSelf(c).Order(StringComparer.Ordinal).ToArray());
            var expanded = expansion.Values.SelectMany(v => v).Distinct().Order(StringComparer.Ordinal).ToArray();

            // && is array overlap: true if the product shares at least one category with the list.
            // It uses the GIN index on categories.
            conditions.Add("categories && @categories");
            parameters.Add("categories", expanded);
            details["categoryExpansion"] = expansion;
        }

        if (filters.MinPrice is { } minPrice)
        {
            conditions.Add("price >= @minPrice");
            parameters.Add("minPrice", minPrice);
        }

        if (filters.MaxPrice is { } maxPrice)
        {
            conditions.Add("price <= @maxPrice");
            parameters.Add("maxPrice", maxPrice);
        }

        if (filters.Specs is { Count: > 0 } specs)
        {
            // @> is JSONB containment: the product's specs must contain every given key with an equal
            // value. Numbers compare as numbers, so {"voltageV": 18} matches 18 but not "18".
            // The GIN index (jsonb_path_ops) makes this an index lookup rather than a scan.
            var specsJson = JsonSerializer.Serialize(specs);
            conditions.Add("specs @> @specs::jsonb");
            parameters.Add("specs", specsJson);
        }

        return new SqlFilter(conditions, parameters, details);
    }
}
