using System.Diagnostics.CodeAnalysis;

namespace PI.SearchApi.Llm;

/// <summary>
/// A model named the way requests name it (ADR-0019): <c>provider/model</c>, e.g. <c>ollama/qwen3.6:35b</c>.
/// Only the first <c>/</c> separates the two, because model names may contain slashes of their own
/// (<c>compat/meta-llama/llama-3.1-8b</c>).
/// </summary>
public readonly record struct ModelRef(string Provider, string Model)
{
    public override string ToString() => $"{Provider}/{Model}";

    /// <summary>Parses a reference. Fails for a missing half or a provider not in <see cref="LlmProviders"/>.</summary>
    public static bool TryParse([NotNullWhen(true)] string? value, out ModelRef model)
    {
        model = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var slash = value.IndexOf('/', StringComparison.Ordinal);
        if (slash <= 0 || slash == value.Length - 1)
        {
            return false;
        }

        var provider = value[..slash];
        if (LlmProviders.Find(provider) is null)
        {
            return false;
        }

        model = new ModelRef(provider, value[(slash + 1)..]);
        return true;
    }
}
