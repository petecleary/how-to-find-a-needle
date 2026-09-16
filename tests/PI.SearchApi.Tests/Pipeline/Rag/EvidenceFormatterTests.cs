using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Rag;
using Xunit;
using static PI.SearchApi.Tests.Pipeline.Rag.RagTestData;

namespace PI.SearchApi.Tests.Pipeline.Rag;

public sealed class EvidenceFormatterTests
{
    [Fact]
    public void FormatProduct_StartsWithTheCitableIdAndAGbpPrice()
    {
        var item = Gq01Evidence().Find("PROD-0012")!;

        var text = EvidenceFormatter.FormatProduct(item);

        Assert.StartsWith("[PROD-0012] Voltline 65W USB-C GaN Charger — Voltline — £49.99 — specs: connector: usb-c, wattageW: 65, powerDelivery: yes", text);
        Assert.Contains("\nCompatibility: Compatible", text);
        Assert.Contains("\nDescription: Compact 65W charger.", text);
    }

    [Fact]
    public void FormatProduct_Reasons_UseWordsInsteadOfSymbols()
    {
        var text = EvidenceFormatter.FormatProduct(Gq01Evidence().Find("PROD-0014")!);

        Assert.Contains("Compatibility: Incompatible — do not recommend", text);
        Assert.Contains("  - Failed: The charger's plug must fit", text);
        Assert.DoesNotContain("✗", text);
    }

    [Fact]
    public void FormatProducts_LeavesOutTheTargetDevice()
    {
        var evidence = Gq01Evidence();

        Assert.DoesNotContain("[PROD-0001]", EvidenceFormatter.FormatProducts(evidence));
        Assert.StartsWith("[PROD-0001] Blackbird Aerobook 14", EvidenceFormatter.FormatTargetDevice(evidence));
    }

    [Fact]
    public void FormatTargetDevice_NoDeviceButStatedRequirements_NamesWhatWasAskedFor()
    {
        var evidence = new EvidenceSet([], [], []) { StatedRequirements = ["\"USB-C\": connector is usb-c", "\"65W\": wattageW at least 65"] };

        var text = EvidenceFormatter.FormatTargetDevice(evidence);

        Assert.StartsWith("None: the shopper hasn't said which device they own.", text);
        Assert.Contains("\"USB-C\": connector is usb-c; \"65W\": wattageW at least 65", text);
    }

    [Fact]
    public void FormatProduct_Fits_ListsTheCatalogDevices()
    {
        var item = Gq01Evidence().Find("PROD-0012")!;
        item = item with
        {
            Compatibility = item.Compatibility with
            {
                Fits = [new DeviceFit("laptops", "Laptops", 3, [new FittingDevice("PROD-0001", "Blackbird Aerobook 14")])],
            },
        };

        Assert.Contains("\nFits 1 of 3 laptops in this catalog: Blackbird Aerobook 14.", EvidenceFormatter.FormatProduct(item));
    }

    [Fact]
    public void Truncate_LongDescription_CutsAtAWordBoundaryWithAnEllipsis()
    {
        var description = string.Join(' ', Enumerable.Repeat("charger", 60)); // 479 characters

        var truncated = EvidenceFormatter.Truncate(description);

        Assert.True(truncated.Length <= EvidenceFormatter.MaxDescriptionLength + 1);
        Assert.EndsWith("charger…", truncated);
    }

    [Fact]
    public void RenderUserPrompt_Gq01Evidence_FillsEverySectionWithNoPlaceholdersLeft()
    {
        var prompts = new PromptLibrary(Path.Combine(AppContext.BaseDirectory, "assets", "prompts"));

        var prompt = AnswerGenerator.RenderUserPrompt(prompts, "power adapter for my laptop", Gq01Evidence());

        Assert.DoesNotContain("{{", prompt);
        Assert.Contains("## Question\n\npower adapter for my laptop", prompt);
        Assert.Contains("## The shopper's device\n\n[PROD-0001] Blackbird Aerobook 14", prompt);
        Assert.Contains("[PROD-0012] Voltline 65W USB-C GaN Charger", prompt);
        Assert.Contains("[PROD-0014] Voltline 45W Barrel Charger", prompt);
        Assert.Contains("- Laptop chargers (laptop-chargers). Also called: power adapter, power brick. Definition: A charger for a laptop.", prompt);
        Assert.Contains("- chargers → laptops: The charger's plug must fit the laptop's charging port.", prompt);
    }
}
