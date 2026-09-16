namespace PI.CatalogGenerator;

/// <summary>
/// Builds the generated distractors from templates: fixed product lines × variants (capacity, wattage,
/// size, colour), with prices and reviews picked by a seeded <see cref="Random"/>.
/// </summary>
/// <remarks>
/// Templates, not an LLM: the output is reproducible, reviewable in a diff, and needs no API key
/// (ADR-0005 § Alternatives considered). The product groups live in the partial files beside this one.
/// Every product must pass the catalog validation tests, which check categories and vocabulary-backed
/// spec values against the ontology; this class only checks what those tests can't know about.
/// </remarks>
public sealed partial class DistractorCatalog(int seed)
{
    public const int DefaultSeed = 20260916;

    // The curated core's talk moments depend on these brands staying small and hand-written:
    // GQ-08's device-name trap needs Blackbird's brand pull to come from the curated items only,
    // and GQ-01, GQ-02 and GQ-07 are all about Voltline's specific chargers.
    private static readonly HashSet<string> ReservedBrands = ["Blackbird", "Voltline"];

    private Random _random = new(seed);
    private readonly List<GeneratedProduct> _products = [];

    /// <summary>Generates the full distractor list. Calling it again with the same seed returns the same products.</summary>
    public IReadOnlyList<GeneratedProduct> Generate()
    {
        _random = new Random(seed);
        _products.Clear();

        AddLaptops();
        AddLaptopChargers();
        AddPhoneChargers();
        AddPowerBanks();
        AddSsds();
        AddMemory();
        AddExternalStorage();
        AddDrills();
        AddAngleGrinders();
        AddToolBatteries();
        AddToolBatteryChargers();
        AddOtherPowerTools();
        AddPowerToolAccessories();
        AddCordlessPhones();
        AddPhoneBatteries();
        AddPhoneAccessories();
        AddAudio();
        AddPeripherals();
        AddBags();
        AddUsbHubs();
        AddCables();

        return [.. _products];
    }

    /// <summary>
    /// Rules the curated core relies on that no schema or ontology test can see. Failing here, before the
    /// file is written, is cheaper than discovering a broken golden query after re-embedding 500 products.
    /// </summary>
    public static void CheckAgainstCuratedCore(IReadOnlyList<GeneratedProduct> products, CuratedCore core)
    {
        var problems = new List<string>();
        var names = new HashSet<string>(core.Names, StringComparer.OrdinalIgnoreCase);

        foreach (var product in products)
        {
            if (!CatalogFile.IsGeneratedId(product.Id) || core.Ids.Contains(product.Id))
            {
                problems.Add($"{product.Id} is outside the generated range (PROD-{CatalogFile.FirstGeneratedNumber} onwards)");
            }

            if (!names.Add(product.Name))
            {
                problems.Add($"{product.Id} repeats the name \"{product.Name}\"");
            }

            if (ReservedBrands.Contains(product.Brand))
            {
                problems.Add($"{product.Id} uses {product.Brand}, a brand reserved for the curated core");
            }

            // GQ-04 asserts that brand = Brakk, voltageV = 18, maxPrice = £100 returns exactly six products.
            var isBrakk18V = product.Brand == "Brakk" && product.Specs.Any(s => s is { Key: "voltageV", Value: 18 });
            if (isBrakk18V && product.Price <= 100m)
            {
                problems.Add($"{product.Id} is a Brakk 18V product at £{product.Price}; GQ-04 needs every generated one above £100");
            }
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException("The generated catalog breaks the curated core:\n  " + string.Join("\n  ", problems));
        }
    }

    private void Add(
        string brand,
        string name,
        string[] categories,
        decimal basePrice,
        string description,
        string[] reviewPool,
        params Spec[] specs)
    {
        var id = $"PROD-{CatalogFile.FirstGeneratedNumber + _products.Count:0000}";

        _products.Add(new GeneratedProduct(
            id,
            $"{brand} {name}",
            brand,
            categories,
            PriceNear(basePrice),
            description,
            PickReviews(reviewPool),
            specs));
    }

    // Shop prices: within ±8% of the template's price, ending in .99.
    // Example: base 49 × 1.05 = 51.45 → £51.99.
    private decimal PriceNear(decimal basePrice)
    {
        var factor = 0.92m + (decimal)_random.NextDouble() * 0.16m;
        var pounds = Math.Max(1m, Math.Floor(basePrice * factor));

        return pounds + 0.99m;
    }

    // One to three distinct reviews. Review text is part of what gets embedded and keyword-indexed
    // (at the lowest weight, D), so distractors need it as much as the curated products do.
    private string[] PickReviews(string[] pool)
    {
        var count = Math.Min(pool.Length, 1 + _random.Next(3));

        return [.. pool.OrderBy(_ => _random.Next()).Take(count)];
    }

    private static Spec S(string key, object value) => new(key, value);
}
