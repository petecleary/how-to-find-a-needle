using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Ontology;

// Stage 5, step 5 — Which devices does it fit?
//
// What:     With no target device, runs the normal device checks for an accessory against every catalog device
//           its rules name: "fits 6 of 13 laptops: Blackbird Aerobook 14, Corvid Slate 13, …".
// Strength: "Unknown" stops being a dead end. The shopper sees who a product is for without choosing a device,
//           and the LLM stages get concrete facts to answer from.
// Failure:  It only knows devices in this catalog. "Fits none of our laptops" says nothing about the laptop
//           the shopper actually owns, and the cost grows with candidates × devices.
// Decision: docs/decisions/0013-domain-ontology-and-compatibility.md
public sealed class DeviceFitFinder(IOntology ontology, CompatibilityEvaluator evaluator)
{
    /// <summary>
    /// For each device type the candidate's rules name, the devices every check passes for. Null when no rule applies
    /// to the candidate, so products without rules (bags, cables) carry no fits list at all.
    /// </summary>
    public IReadOnlyList<DeviceFit>? FitsFor(ProductSummary candidate, IReadOnlyList<ProductSummary> devices)
    {
        var deviceTypes = evaluator.RulesForAccessory(candidate)
            .Select(rule => rule.DeviceTypeNotation)
            .Distinct()
            .ToList();

        if (deviceTypes.Count == 0)
        {
            return null;
        }

        return
        [
            .. deviceTypes.Select(deviceType =>
            {
                var devicesOfType = devices
                    .Where(device => device.Id != candidate.Id
                        && device.Categories.Any(category => ontology.IsNarrowerOrSelf(category, deviceType)))
                    .ToList();

                var fitting = devicesOfType
                    .Where(device => evaluator.Evaluate(candidate, device).Result.Status == CompatibilityStatus.Compatible)
                    .Select(device => new FittingDevice(device.Id, device.Name))
                    .ToList();

                var label = ontology.TryGetConcept(deviceType, out var concept)
                    ? concept.PrefLabels.GetValueOrDefault("en") ?? deviceType
                    : deviceType;

                return new DeviceFit(deviceType, label, devicesOfType.Count, fitting);
            }),
        ];
    }
}
