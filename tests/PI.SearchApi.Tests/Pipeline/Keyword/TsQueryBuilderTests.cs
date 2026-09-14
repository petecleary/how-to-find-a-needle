using PI.SearchApi.Pipeline.Keyword;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline.Keyword;

public sealed class TsQueryBuilderTests
{
    [Fact]
    public void ForQuery_UsesWebsearchToTsQueryWithOneParameter()
    {
        var (expression, parameters) = TsQueryBuilder.ForQuery("cordless drill battery");

        Assert.Equal("websearch_to_tsquery('english', @query)", expression);
        Assert.Equal("cordless drill battery", parameters.ForTrace()["query"]);
    }

    [Fact]
    public void ForExpansion_OneGroupAndRest_OrsTheGroupAndAndsTheRest()
    {
        var expansion = new KeywordExpansion([["power brick", "ac adapter", "charger"]], "for laptop");

        var (expression, parameters) = TsQueryBuilder.ForExpansion(expansion, "power brick for laptop");

        Assert.Equal(
            "(phraseto_tsquery('english', @g0t0) || phraseto_tsquery('english', @g0t1) || phraseto_tsquery('english', @g0t2))"
            + " && websearch_to_tsquery('english', @rest)",
            expression);
        var trace = parameters.ForTrace();
        Assert.Equal("power brick", trace["g0t0"]);
        Assert.Equal("charger", trace["g0t2"]);
        Assert.Equal("for laptop", trace["rest"]);
    }

    [Fact]
    public void ForExpansion_TwoGroups_AreAndedTogether()
    {
        var expansion = new KeywordExpansion([["charger", "power adapter"], ["laptop", "notebook"]], "");

        var (expression, _) = TsQueryBuilder.ForExpansion(expansion, "charger laptop");

        Assert.Equal(
            "(phraseto_tsquery('english', @g0t0) || phraseto_tsquery('english', @g0t1))"
            + " && (phraseto_tsquery('english', @g1t0) || phraseto_tsquery('english', @g1t1))",
            expression);
    }

    [Fact]
    public void ForExpansion_NoGroupsAndNoRest_FallsBackToTheOriginalQuery()
    {
        var (expression, parameters) = TsQueryBuilder.ForExpansion(new KeywordExpansion([], "  "), "anything");

        Assert.Equal("websearch_to_tsquery('english', @query)", expression);
        Assert.Equal("anything", parameters.ForTrace()["query"]);
    }

    [Fact]
    public void ForExpansion_UserText_IsNeverInterpolatedIntoTheExpression()
    {
        var expansion = new KeywordExpansion([["'); DROP TABLE products; --"]], "x' OR '1'='1");

        var (expression, _) = TsQueryBuilder.ForExpansion(expansion, "ignored");

        Assert.DoesNotContain("DROP", expression);
        Assert.DoesNotContain("OR '1'", expression);
    }
}
