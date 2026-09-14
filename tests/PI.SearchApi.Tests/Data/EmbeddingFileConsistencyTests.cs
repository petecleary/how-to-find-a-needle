using PI.SearchApi.Data;
using PI.SearchApi.Embeddings;
using Xunit;

namespace PI.SearchApi.Tests.Data;

/// <summary>
/// The committed nomic.jsonl must describe the current catalog (ADR-0009). When a product's text
/// changes without a rebuild, this fails in CI, instead of a fresh clone silently re-embedding live
/// and drifting from the rehearsed results.
/// </summary>
public sealed class EmbeddingFileConsistencyTests
{
    private static readonly string NomicFile = Path.Combine(TestPaths.SourceDataDirectory, "embeddings", "nomic.jsonl");

    [Fact]
    public void NomicFile_EveryProductHasALineWithAMatchingContentHash()
    {
        var products = CatalogLoader.LoadProducts(TestPaths.SourceDataDirectory);
        var entries = EmbeddingFile.Read(NomicFile).ToDictionary(e => e.Id);

        var problems = new List<string>();

        foreach (var product in products)
        {
            if (!entries.TryGetValue(product.Id, out var entry))
            {
                problems.Add($"{product.Id} has no line");
            }
            else if (entry.ContentHash != ProductDocument.ContentHash(product))
            {
                problems.Add($"{product.Id} has a stale content hash");
            }
        }

        Assert.True(
            problems.Count == 0,
            $"{NomicFile} is out of date ({string.Join("; ", problems)}). Run the AppHost once with Embeddings__Rebuild=true and commit the file.");
    }

    [Fact]
    public void NomicFile_HasNoLinesForRemovedProducts()
    {
        var productIds = CatalogLoader.LoadProducts(TestPaths.SourceDataDirectory).Select(p => p.Id).ToHashSet();

        var orphans = EmbeddingFile.Read(NomicFile).Where(e => !productIds.Contains(e.Id)).Select(e => e.Id).ToList();

        Assert.Empty(orphans);
    }

    [Fact]
    public void NomicFile_EveryVectorIs768DimensionsFromTheNomicModel()
    {
        var entries = EmbeddingFile.Read(NomicFile);

        Assert.NotEmpty(entries);
        Assert.All(entries, entry =>
        {
            Assert.Equal(NomicOnnxEmbeddingGenerator.ModelId, entry.Model);
            Assert.Equal(768, entry.Dimensions);
            Assert.Equal(768, VectorEncoding.FromBase64(entry.Vector).Length);
        });
    }
}
