using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using PI.SearchApi.Embeddings;
using Xunit;

namespace PI.SearchApi.Tests.Embeddings;

/// <summary>
/// Runs the real Nomic model when it has been downloaded, and skips with a clear message when it hasn't
/// (the models are gitignored, so CI never has them).
/// </summary>
public sealed class NomicOnnxEmbeddingGeneratorTests
{
    private static readonly string ModelDirectory = Path.Combine(TestPaths.RepositoryRoot, "src", "PI.SearchApi", "assets", "models", "nomic");

    private static NomicOnnxEmbeddingGenerator CreateGenerator() => new(ModelDirectory, NullLogger<NomicOnnxEmbeddingGenerator>.Instance);

    private static void SkipUnlessModelIsPresent() =>
        Assert.SkipUnless(CreateGenerator().IsAvailable, $"Nomic model not downloaded to {ModelDirectory}; see assets/models/README.md.");

    [Fact]
    public async Task GenerateAsync_ProducesUnitLength768DimensionVectors()
    {
        SkipUnlessModelIsPresent();
        using var generator = CreateGenerator();

        var embeddings = await generator.GenerateAsync(["search_query: power brick for laptop"], cancellationToken: TestContext.Current.CancellationToken);

        var vector = embeddings[0].Vector.ToArray();
        Assert.Equal(768, vector.Length);
        Assert.Equal(1.0, Math.Sqrt(vector.Sum(v => (double)v * v)), 3);
    }

    [Fact]
    public async Task GenerateAsync_RelatedTextsAreCloserThanUnrelatedOnes()
    {
        SkipUnlessModelIsPresent();
        using var generator = CreateGenerator();

        var embeddings = await generator.GenerateAsync(
            [
                "search_query: power brick for my laptop",
                "search_document: A 65W USB-C laptop charger and AC power adapter.",
                "search_document: Over-ear wireless headphones with noise cancellation.",
            ],
            cancellationToken: TestContext.Current.CancellationToken);

        var query = embeddings[0].Vector.Span;
        var charger = embeddings[1].Vector.Span;
        var headphones = embeddings[2].Vector.Span;

        // Vectors are unit length, so the dot product is the cosine similarity.
        Assert.True(Dot(query, charger) > Dot(query, headphones));
    }

    [Fact]
    public async Task GenerateAsync_SameTextTwice_EmbedsIdentically()
    {
        // Embedding is deterministic: the committed vectors can be reproduced by a rebuild.
        SkipUnlessModelIsPresent();
        using var generator = CreateGenerator();
        var ct = TestContext.Current.CancellationToken;

        var first = await generator.GenerateAsync(["search_query: SSD"], cancellationToken: ct);
        var second = await generator.GenerateAsync(["search_query: SSD"], cancellationToken: ct);

        Assert.Equal(first[0].Vector.ToArray(), second[0].Vector.ToArray());
    }

    [Fact]
    public async Task GenerateAsync_BatchWithoutPadding_MatchesSingleCall()
    {
        // Two identical texts need no padding, so batching alone doesn't change a vector. (Padding does,
        // slightly, with this int8 model — which is why the seeder embeds one product per call, ADR-0009.)
        SkipUnlessModelIsPresent();
        using var generator = CreateGenerator();
        var ct = TestContext.Current.CancellationToken;

        var alone = await generator.GenerateAsync(["search_query: SSD"], cancellationToken: ct);
        var batched = await generator.GenerateAsync(["search_query: SSD", "search_query: SSD"], cancellationToken: ct);

        Assert.True(Dot(alone[0].Vector.Span, batched[0].Vector.Span) > 0.99999);
    }

    [Fact]
    public async Task GenerateAsync_ModelMissing_ThrowsUnavailableWithGuidance()
    {
        using var generator = new NomicOnnxEmbeddingGenerator(Path.Combine(Path.GetTempPath(), "no-such-model"), NullLogger<NomicOnnxEmbeddingGenerator>.Instance);

        var ex = await Assert.ThrowsAsync<EmbeddingModelUnavailableException>(
            () => generator.GenerateAsync(["anything"], cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("assets/models/README.md", ex.Message);
    }

    private static double Dot(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        var sum = 0.0;
        for (var i = 0; i < a.Length; i++)
        {
            sum += a[i] * b[i];
        }

        return sum;
    }
}
