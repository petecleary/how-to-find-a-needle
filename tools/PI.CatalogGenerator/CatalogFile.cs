using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PI.CatalogGenerator;

/// <summary>
/// Reads and writes products.json so that the curated core is copied byte for byte. The core is
/// hand-formatted (inline category lists and specs), and a serialiser round-trip would reformat
/// every product and turn <c>4.0</c> into <c>4</c>: the diff should show only generated products.
/// </summary>
public static class CatalogFile
{
    /// <summary>Generated products start at PROD-1001; everything below is hand-written.</summary>
    public const int FirstGeneratedNumber = 1001;

    private static readonly JsonSerializerOptions StringEscaping = new()
    {
        // Keeps apostrophes and "£" readable in the file; quotes and control characters are still escaped.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static bool IsGeneratedId(string id) =>
        int.TryParse(id.AsSpan("PROD-".Length), CultureInfo.InvariantCulture, out var number) && number >= FirstGeneratedNumber;

    /// <summary>
    /// Finds where the last curated product ends. Utf8JsonReader reports byte offsets, so the text before
    /// that point can be kept exactly as written, while any previously generated products after it are dropped.
    /// </summary>
    public static CuratedCore ReadCuratedCore(string catalogText)
    {
        var bytes = Encoding.UTF8.GetBytes(catalogText);
        var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow });

        // Depth 0 is the root object, 1 the "products" array, 2 one product, 3 its properties.
        const int ProductDepth = 2;

        var ids = new HashSet<string>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long endOfLastCurated = -1;
        var seenGenerated = false;
        string? id = null;
        string? name = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == ProductDepth + 1)
            {
                var property = reader.GetString();
                reader.Read();

                if (property == "id")
                {
                    id = reader.GetString();
                }
                else if (property == "name")
                {
                    name = reader.GetString();
                }
                else
                {
                    reader.Skip();
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == ProductDepth)
            {
                if (id is null || name is null)
                {
                    throw new InvalidDataException($"A product near byte {reader.TokenStartIndex} has no id or name.");
                }

                if (IsGeneratedId(id))
                {
                    seenGenerated = true;
                }
                else if (seenGenerated)
                {
                    throw new InvalidDataException(
                        $"{id} is curated but comes after generated products. Keep hand-written products (below PROD-{FirstGeneratedNumber}) first.");
                }
                else
                {
                    ids.Add(id);
                    names.Add(name);
                    endOfLastCurated = reader.BytesConsumed;
                }

                id = null;
                name = null;
            }
        }

        if (endOfLastCurated < 0)
        {
            throw new InvalidDataException("products.json has no curated products to grow from.");
        }

        var textUpToLastCurated = Encoding.UTF8.GetString(bytes, 0, (int)endOfLastCurated);
        return new CuratedCore(textUpToLastCurated, ids, names);
    }

    /// <summary>The curated core as written, then each generated product in the same layout, then the closing brackets.</summary>
    public static string Write(CuratedCore core, IReadOnlyList<GeneratedProduct> products)
    {
        var text = new StringBuilder(core.TextUpToLastCurated);

        foreach (var product in products)
        {
            text.Append(",\n");
            AppendProduct(text, product);
        }

        text.Append("\n  ]\n}\n");
        return text.ToString();
    }

    private static void AppendProduct(StringBuilder text, GeneratedProduct product)
    {
        const string Indent = "      ";

        text.Append("    {\n");
        text.Append(Indent).Append("\"id\": ").Append(Quote(product.Id)).Append(",\n");
        text.Append(Indent).Append("\"name\": ").Append(Quote(product.Name)).Append(",\n");
        text.Append(Indent).Append("\"brand\": ").Append(Quote(product.Brand)).Append(",\n");
        text.Append(Indent).Append("\"categories\": [").AppendJoin(", ", product.Categories.Select(Quote)).Append("],\n");
        text.Append(Indent).Append("\"price\": ").Append(product.Price.ToString("0.00", CultureInfo.InvariantCulture)).Append(",\n");
        text.Append(Indent).Append("\"currency\": \"GBP\",\n");
        text.Append(Indent).Append("\"description\": ").Append(Quote(product.Description)).Append(",\n");
        text.Append(Indent).Append("\"reviews\": [").AppendJoin(", ", product.Reviews.Select(Quote)).Append("],\n");
        text.Append(Indent).Append("\"specs\": ").Append(FormatSpecs(product.Specs)).Append('\n');
        text.Append("    }");
    }

    private static string FormatSpecs(IReadOnlyList<Spec> specs) =>
        specs.Count == 0
            ? "{}"
            : "{ " + string.Join(", ", specs.Select(spec => $"{Quote(spec.Key)}: {FormatValue(spec.Value)}")) + " }";

    private static string FormatValue(object value) => value switch
    {
        string s => Quote(s),
        bool b => b ? "true" : "false",
        int i => i.ToString(CultureInfo.InvariantCulture),
        // One decimal place, as the curated core writes capacities ("capacityAh": 4.0).
        decimal d => d.ToString("0.0##", CultureInfo.InvariantCulture),
        _ => throw new ArgumentException($"Unsupported spec value type {value.GetType().Name}"),
    };

    private static string Quote(string value) => JsonSerializer.Serialize(value, StringEscaping);
}

/// <summary>The hand-written part of products.json: its exact text, and the IDs and names generation must not reuse.</summary>
public sealed record CuratedCore(string TextUpToLastCurated, IReadOnlySet<string> Ids, IReadOnlySet<string> Names)
{
    public int ProductCount => Ids.Count;
}

/// <summary>One spec entry, kept in insertion order so the file reads the way the templates are written.</summary>
public sealed record Spec(string Key, object Value);

/// <summary>A generated distractor, shaped like a products.json entry (ADR-0005 § Product schema).</summary>
public sealed record GeneratedProduct(
    string Id,
    string Name,
    string Brand,
    IReadOnlyList<string> Categories,
    decimal Price,
    string Description,
    IReadOnlyList<string> Reviews,
    IReadOnlyList<Spec> Specs);
