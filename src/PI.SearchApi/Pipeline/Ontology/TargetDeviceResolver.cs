using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Ontology;

/// <summary>
/// Works out which device the shopper owns, and where the query names it (ADR-0013, step 1):
/// <c>context.targetProductId</c> if given, otherwise the longest device name found in the query, otherwise none.
/// </summary>
/// <remarks>
/// "A device name is context, not intent." Knowing where the query names the device lets Stage 6 remove
/// those words from what keyword and vector search receive, and use the device for rule checks instead.
/// </remarks>
public sealed class TargetDeviceResolver(ProductLookup products)
{
    public async Task<TargetDevice> ResolveAsync(SearchRequest request, IReadOnlyList<Token> queryTokens, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Context.TargetProductId))
        {
            var byId = await products.GetByIdAsync(request.Context.TargetProductId, ct);

            if (byId is null)
            {
                return new TargetDevice(null, $"context.targetProductId '{request.Context.TargetProductId}' is not a product, so no device was used", null);
            }

            // The query may name the same device as well ("charger for my Blackbird Aerobook 14").
            return new TargetDevice(byId, "context.targetProductId", FindMention(queryTokens, [byId])?.Span);
        }

        var devices = await products.GetDevicesAsync(ct);
        var mention = FindMention(queryTokens, devices);

        return mention is null
            ? new TargetDevice(null, "none: no context.targetProductId, and no device name appears in the query", null)
            : new TargetDevice(mention.Device, "device name found in the query", mention.Span);
    }

    /// <summary>
    /// Finds the device whose whole name appears in the query as a run of folded tokens. The longest name wins,
    /// so "Corvid Slate 15 Pro" beats "Corvid Slate 15". Deliberately exact: "my Aerobook" alone isn't a match.
    /// </summary>
    public static DeviceMention? FindMention(IReadOnlyList<Token> queryTokens, IEnumerable<ProductSummary> devices)
    {
        var query = queryTokens.Select(t => t.Folded).ToList();
        DeviceMention? best = null;

        foreach (var device in devices)
        {
            var name = TextNormaliser.Tokenise(device.Name).Select(t => t.Folded).ToList();

            if (name.Count == 0 || name.Count > query.Count || (best is not null && name.Count <= best.Span.Count))
            {
                continue;
            }

            for (var start = 0; start + name.Count <= query.Count; start++)
            {
                if (query.Skip(start).Take(name.Count).SequenceEqual(name))
                {
                    best = new DeviceMention(device, new TokenSpan(start, name.Count));
                    break;
                }
            }
        }

        return best;
    }
}

/// <summary>The resolved target device (or null), how it was found, and where the query names it (if it does).</summary>
public sealed record TargetDevice(ProductSummary? Product, string Method, TokenSpan? Mention);

/// <summary>A device and the tokens of the query that name it.</summary>
public sealed record DeviceMention(ProductSummary Device, TokenSpan Span);

/// <summary>A run of query tokens: <paramref name="Count"/> tokens starting at index <paramref name="Start"/>.</summary>
public sealed record TokenSpan(int Start, int Count);
