using System.Text.Encodings.Web;
using System.Text.Json;

namespace PI.SearchApi.Data;

/// <summary>
/// Reads and writes the committed product embedding files, <c>assets/data/embeddings/{provider}.jsonl</c>
/// (ADR-0009). One JSON object per line; the vector is base64 over little-endian float32.
/// </summary>
/// <remarks>
/// Embeddings are derived data, a cache of the catalog's text. Committing them is safe because every
/// line records the model that made it and the hash of the text it describes: if either no longer
/// matches, the seeder ignores that line and re-embeds the product.
/// </remarks>
public static class EmbeddingFile
{
    // The relaxed encoder keeps base64's '+' as '+' rather than "+". The file is never embedded in
    // HTML, so HTML-safe escaping only makes the committed lines harder to read.
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static IReadOnlyList<EmbeddingFileEntry> Read(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        return
        [
            .. File.ReadLines(path)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => JsonSerializer.Deserialize<EmbeddingFileEntry>(line, Options)
                    ?? throw new InvalidOperationException($"{path} contains an empty JSON line.")),
        ];
    }

    /// <summary>Writes one line per entry, ordered by product ID so diffs stay small and reviewable.</summary>
    public static void Write(string path, IEnumerable<EmbeddingFileEntry> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var lines = entries
            .OrderBy(e => e.Id, StringComparer.Ordinal)
            .Select(e => JsonSerializer.Serialize(e, Options));

        File.WriteAllText(path, string.Join('\n', lines) + '\n');
    }
}

/// <summary>One product's vector in an embedding file.</summary>
/// <param name="ContentHash">SHA-256 of the embedded text (<c>ProductDocument.ContentHash</c>).</param>
/// <param name="Model">The model that produced the vector; vectors from different models are never mixed.</param>
/// <param name="Vector">Base64 of little-endian float32 values (<c>VectorEncoding</c>).</param>
public sealed record EmbeddingFileEntry(string Id, string ContentHash, string Model, int Dimensions, string Vector);
