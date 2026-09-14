using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Ontology;

/// <summary>
/// Works out which device the shopper owns, so Stage 6 has something to check rules against (ADR-0013):
/// <c>context.targetProductId</c> if given, otherwise the longest device name found in the query, otherwise none.
/// </summary>
public sealed class TargetDeviceResolver(ProductLookup products)
{
    public async Task<TargetDevice> ResolveAsync(SearchRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Context.TargetProductId))
        {
            var byId = await products.GetByIdAsync(request.Context.TargetProductId, ct);

            return byId is null
                ? new TargetDevice(null, $"context.targetProductId '{request.Context.TargetProductId}' is not a product, so no device was used")
                : new TargetDevice(byId, "context.targetProductId");
        }

        // "charger for my Blackbird Aerobook 14" contains the device name "Blackbird Aerobook 14".
        // The longest name wins, so "Corvid Slate 15 Pro" beats a hypothetical "Corvid Slate 15".
        var queryPhrase = " " + string.Join(' ', TextNormaliser.Tokenise(request.Query).Select(t => t.Folded)) + " ";
        var devices = await products.GetDevicesAsync(ct);

        var named = devices
            .Select(device => (Device: device, Phrase: TextNormaliser.FoldPhrase(device.Name)))
            .Where(entry => entry.Phrase.Length > 0 && queryPhrase.Contains(" " + entry.Phrase + " ", StringComparison.Ordinal))
            .OrderByDescending(entry => entry.Phrase.Length)
            .Select(entry => entry.Device)
            .FirstOrDefault();

        return named is null
            ? new TargetDevice(null, "none: no context.targetProductId, and no device name appears in the query")
            : new TargetDevice(named, "device name found in the query");
    }
}

/// <summary>The resolved target device (or null) and how it was found, for the trace.</summary>
public sealed record TargetDevice(ProductSummary? Product, string Method);
