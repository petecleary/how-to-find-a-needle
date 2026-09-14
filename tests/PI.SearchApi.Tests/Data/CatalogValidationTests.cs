using System.Text.Json;
using System.Text.RegularExpressions;
using PI.SearchApi.Data;
using PI.SearchApi.Pipeline.Ontology;
using Xunit;

namespace PI.SearchApi.Tests.Data;

/// <summary>
/// Checks products.json and golden-queries.json against the real ontology, so a typo in a
/// category, a spec value or a product ID fails a fast test instead of a confusing runtime
/// error three stages later (tests/CLAUDE.md, ADR-0005, ADR-0013).
/// </summary>
public sealed class CatalogValidationTests
{
    private static readonly string DataDirectory = Path.Combine(AppContext.BaseDirectory, "assets", "data");
    private static readonly Regex ProductIdPattern = new(@"^PROD-\d{4}$");
    private static readonly Regex GoldenQueryIdPattern = new(@"^GQ-(\d{2})$");

    // Unit-suffixed spec keys must hold a number, never a string like "65W" (ADR-0005,
    // matching the pattern products.schema.json enforces).
    private static readonly Regex UnitSuffixedSpecKey = new("(W|V|Gb|Ah|Mah|In|Kg)$");

    private static IReadOnlyList<CatalogProduct> LoadProducts() => CatalogLoader.LoadProducts(DataDirectory);

    private static IReadOnlyList<GoldenQuery> LoadGoldenQueries() => CatalogLoader.LoadGoldenQueries(DataDirectory);

    private static DomainOntology LoadOntology() => new(DataDirectory);

    [Fact]
    public void Products_Ids_AreUnique()
    {
        var products = LoadProducts();

        var duplicates = products.GroupBy(p => p.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Products_Ids_MatchPattern()
    {
        var products = LoadProducts();

        var malformed = products.Where(p => !ProductIdPattern.IsMatch(p.Id)).Select(p => p.Id).ToList();

        Assert.Empty(malformed);
    }

    [Fact]
    public void Products_Currency_IsGbp()
    {
        var products = LoadProducts();

        var wrongCurrency = products.Where(p => p.Currency != "GBP").Select(p => p.Id).ToList();

        Assert.Empty(wrongCurrency);
    }

    [Fact]
    public void Products_Price_IsPositive()
    {
        var products = LoadProducts();

        var nonPositive = products.Where(p => p.Price <= 0).Select(p => p.Id).ToList();

        Assert.Empty(nonPositive);
    }

    [Fact]
    public void Products_Categories_AreTaxonomyNotations()
    {
        var products = LoadProducts();
        var ontology = LoadOntology();

        var unknown = products
            .SelectMany(p => p.Categories.Select(category => (Product: p.Id, Category: category)))
            .Where(pc => !ontology.TryGetConcept(pc.Category, out _))
            .ToList();

        Assert.Empty(unknown);
    }

    [Fact]
    public void Products_UnitSuffixedSpecs_AreNumeric()
    {
        var products = LoadProducts();

        var nonNumeric = products
            .SelectMany(p => p.Specs.Select(spec => (Product: p.Id, Key: spec.Key, Value: spec.Value)))
            .Where(s => UnitSuffixedSpecKey.IsMatch(s.Key) && s.Value.ValueKind != JsonValueKind.Number)
            .Select(s => $"{s.Product}.{s.Key}")
            .ToList();

        Assert.Empty(nonNumeric);
    }

    // Every check whose ex:valueScheme is set compares an accessory's spec against a
    // device's spec as vocabulary concepts (ADR-0013): "usb-c" must be a real connector,
    // not a typo. Numeric checks (no valueScheme) are exempt.
    [Fact]
    public void Products_VocabularyBackedSpecValues_AreKnownNotationsOrLabels()
    {
        var products = LoadProducts();
        var ontology = LoadOntology();

        var badValues = new List<string>();

        foreach (var rule in ontology.Rules)
        {
            foreach (var check in rule.Checks.Where(c => c.ValueSchemeNotation is not null))
            {
                var vocabulary = ontology.VocabularyValues(check.ValueSchemeNotation!);

                badValues.AddRange(FindBadSpecValues(products, ontology, rule.AccessoryTypeNotation, check.AccessorySpec, vocabulary));
                badValues.AddRange(FindBadSpecValues(products, ontology, rule.DeviceTypeNotation, check.DeviceSpec, vocabulary));
            }
        }

        Assert.Empty(badValues);
    }

    private static IEnumerable<string> FindBadSpecValues(
        IReadOnlyList<CatalogProduct> products,
        IOntology ontology,
        string typeNotation,
        string specName,
        IReadOnlyList<VocabularyValue> vocabulary)
    {
        foreach (var product in products)
        {
            if (!product.Categories.Any(c => ontology.IsNarrowerOrSelf(c, typeNotation)))
            {
                continue;
            }

            if (!product.Specs.TryGetValue(specName, out var value) || value.ValueKind != JsonValueKind.String)
            {
                continue; // Missing-spec coverage is Products_DeviceAndAccessoryTypes_HaveSpecsTheirRulesCompare's job.
            }

            var text = value.GetString()!;
            var isKnown = vocabulary.Any(v => v.Notation == text || v.Labels.Contains(text));

            if (!isKnown)
            {
                yield return $"{product.Id}.{specName}={text}";
            }
        }
    }

    // Every product under a rule's accessory or device type must carry the spec(s) that
    // rule compares — otherwise Stage 6 (Phase 2) can only ever report Unknown for it.
    [Fact]
    public void Products_DeviceAndAccessoryTypes_HaveSpecsTheirRulesCompare()
    {
        var products = LoadProducts();
        var ontology = LoadOntology();

        var missing = new List<string>();

        foreach (var rule in ontology.Rules)
        {
            foreach (var check in rule.Checks)
            {
                missing.AddRange(FindMissingSpecs(products, ontology, rule.AccessoryTypeNotation, check.AccessorySpec));
                missing.AddRange(FindMissingSpecs(products, ontology, rule.DeviceTypeNotation, check.DeviceSpec));
            }
        }

        Assert.Empty(missing);
    }

    private static IEnumerable<string> FindMissingSpecs(
        IReadOnlyList<CatalogProduct> products, IOntology ontology, string typeNotation, string specName) =>
        products
            .Where(p => p.Categories.Any(c => ontology.IsNarrowerOrSelf(c, typeNotation)))
            .Where(p => !p.Specs.ContainsKey(specName))
            .Select(p => $"{p.Id} is missing '{specName}' (required by a rule for '{typeNotation}')");

    [Fact]
    public void GoldenQueries_Ids_AreUniqueAndSequential()
    {
        var goldenQueries = LoadGoldenQueries();

        var numbers = goldenQueries
            .Select(gq => GoldenQueryIdPattern.Match(gq.Id))
            .Select(m => m.Success ? int.Parse(m.Groups[1].Value) : (int?)null)
            .ToList();

        Assert.All(numbers, n => Assert.NotNull(n));
        Assert.Equal(numbers.Distinct().Count(), numbers.Count);
        Assert.Equal(Enumerable.Range(1, goldenQueries.Count), numbers.OrderBy(n => n).Cast<int>());
    }

    [Fact]
    public void GoldenQueries_ProductIds_ExistInCatalog()
    {
        var products = LoadProducts();
        var goldenQueries = LoadGoldenQueries();
        var catalogIds = products.Select(p => p.Id).ToHashSet();

        var missing = new List<string>();

        foreach (var goldenQuery in goldenQueries)
        {
            if (goldenQuery.Request.Context?.TargetProductId is { } targetId && !catalogIds.Contains(targetId))
            {
                missing.Add($"{goldenQuery.Id}: targetProductId {targetId}");
            }

            foreach (var (stage, expectations) in goldenQuery.Expectations)
            {
                foreach (var expectation in expectations.Where(e => !catalogIds.Contains(e.ProductId)))
                {
                    missing.Add($"{goldenQuery.Id}.{stage}: {expectation.ProductId}");
                }
            }
        }

        Assert.Empty(missing);
    }

    [Fact]
    public void Ontology_Turtle_NamesNoProductIds()
    {
        var ttl = File.ReadAllText(Path.Combine(DataDirectory, "domain-ontology.ttl"));

        Assert.DoesNotContain("PROD-", ttl);
    }
}
