using System.Text.Json;
using FluentValidation;
using PI.SearchApi.Contracts;
using PI.SearchApi.Endpoints.Search;
using PI.SearchApi.Pipeline.Ontology;
using Xunit;

namespace PI.SearchApi.Tests.Endpoints.Search;

public sealed class SearchRequestRulesTests
{
    private static readonly DomainOntology Ontology = new(Path.Combine(AppContext.BaseDirectory, "assets", "data"));

    private static InlineValidator<SearchRequest> CreateValidator(bool requireQuery = true)
    {
        var validator = new InlineValidator<SearchRequest>();
        SearchRequestRules.AddTo(validator, Ontology, requireQuery);
        return validator;
    }

    private static SearchRequest Valid() => new() { Query = "power brick for laptop" };

    [Fact]
    public void Validate_DefaultRequestWithQuery_IsValid()
    {
        Assert.True(CreateValidator().Validate(Valid()).IsValid);
    }

    [Fact]
    public void Validate_EmptyQuery_InvalidWhenRequired_ValidForStructured()
    {
        var request = Valid() with { Query = "" };

        Assert.False(CreateValidator(requireQuery: true).Validate(request).IsValid);
        Assert.True(CreateValidator(requireQuery: false).Validate(request).IsValid);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(51, false)]
    public void Validate_PageSize_Between1And50(int pageSize, bool expectedValid)
    {
        Assert.Equal(expectedValid, CreateValidator().Validate(Valid() with { PageSize = pageSize }).IsValid);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Validate_CandidateDepth_Between10And200(int depth, bool expectedValid)
    {
        var request = Valid() with { Options = new SearchOptions { CandidateDepth = depth } };

        Assert.Equal(expectedValid, CreateValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void Validate_RrfK_Between1And1000(int k, bool expectedValid)
    {
        var request = Valid() with { Options = new SearchOptions { RrfK = k } };

        Assert.Equal(expectedValid, CreateValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData(-0.1, false)]
    [InlineData(0.0, true)]
    [InlineData(10.0, true)]
    [InlineData(10.1, false)]
    public void Validate_Weights_Between0And10(double weight, bool expectedValid)
    {
        var request = Valid() with { Options = new SearchOptions { KeywordWeight = weight, VectorWeight = weight } };

        Assert.Equal(expectedValid, CreateValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData("novice", true)]
    [InlineData("expert", true)]
    [InlineData("Novice", false)]
    [InlineData("guru", false)]
    public void Validate_Audience_IsOneOfTheThreeAudiences(string audience, bool expectedValid)
    {
        var request = Valid() with { Options = new SearchOptions { Audience = audience } };

        Assert.Equal(expectedValid, CreateValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Validate_UnknownCategory_IsInvalid()
    {
        var request = Valid() with { Filters = new SearchFilters { Categories = ["chargers", "chargerz"] } };

        var result = CreateValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("chargerz"));
    }

    [Theory]
    [InlineData("voltageV", true)]
    [InlineData("9volts", false)]
    [InlineData("voltage-v", false)]
    public void Validate_SpecKeys_MustBeCamelCaseIdentifiers(string key, bool expectedValid)
    {
        var specs = new Dictionary<string, JsonElement> { [key] = JsonSerializer.SerializeToElement(18) };
        var request = Valid() with { Filters = new SearchFilters { Specs = specs } };

        Assert.Equal(expectedValid, CreateValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Validate_MaxPriceBelowMinPrice_IsInvalid()
    {
        var request = Valid() with { Filters = new SearchFilters { MinPrice = 100m, MaxPrice = 50m } };

        Assert.False(CreateValidator().Validate(request).IsValid);
    }
}
