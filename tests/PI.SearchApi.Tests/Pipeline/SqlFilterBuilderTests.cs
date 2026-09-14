using System.Text.Json;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Ontology;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline;

public sealed class SqlFilterBuilderTests
{
    private static readonly DomainOntology Ontology = new(Path.Combine(AppContext.BaseDirectory, "assets", "data"));

    private static SqlFilterBuilder CreateBuilder() => new(Ontology);

    [Fact]
    public void Build_NoFilters_ProducesNoConditions()
    {
        var filter = CreateBuilder().Build(new SearchFilters());

        Assert.Empty(filter.Conditions);
        Assert.Empty(filter.Parameters.ForTrace());
        Assert.Equal("", filter.WhereClause());
    }

    [Fact]
    public void Build_Brand_IsCaseInsensitiveAndParameterised()
    {
        var filter = CreateBuilder().Build(new SearchFilters { Brand = "brakk" });

        Assert.Equal("lower(brand) = lower(@brand)", Assert.Single(filter.Conditions));
        Assert.Equal("brakk", filter.Parameters.ForTrace()["brand"]);
    }

    [Fact]
    public void Build_BroadCategory_ExpandsToNarrowerConcepts()
    {
        var filter = CreateBuilder().Build(new SearchFilters { Categories = ["chargers"] });

        Assert.Equal("categories && @categories", Assert.Single(filter.Conditions));
        var categories = Assert.IsType<string[]>(filter.Parameters.ForTrace()["categories"]);
        Assert.Contains("chargers", categories);
        Assert.Contains("laptop-chargers", categories);
        Assert.Contains("usb-c-pd-chargers", categories);
        Assert.DoesNotContain("power-banks", categories); // a sibling under Power, not under Chargers
        Assert.True(filter.Details.ContainsKey("categoryExpansion"));
    }

    [Fact]
    public void Build_PriceRange_AddsBothBounds()
    {
        var filter = CreateBuilder().Build(new SearchFilters { MinPrice = 20m, MaxPrice = 100m });

        Assert.Equal(["price >= @minPrice", "price <= @maxPrice"], filter.Conditions);
    }

    [Fact]
    public void Build_Specs_UsesJsonbContainmentWithNumbersKeptAsNumbers()
    {
        var specs = new Dictionary<string, JsonElement> { ["voltageV"] = JsonSerializer.SerializeToElement(18) };

        var filter = CreateBuilder().Build(new SearchFilters { Specs = specs });

        Assert.Equal("specs @> @specs::jsonb", Assert.Single(filter.Conditions));
        Assert.Equal("""{"voltageV":18}""", filter.Parameters.ForTrace()["specs"]);
    }

    [Fact]
    public void WhereClause_StageConditionsFirst_ThenFilterConditionsJoinedWithAnd()
    {
        var filter = CreateBuilder().Build(new SearchFilters { MaxPrice = 50m });

        var where = filter.WhereClause("search_vector @@ q");

        Assert.Equal("WHERE search_vector @@ q\n  AND price <= @maxPrice", where);
    }
}
