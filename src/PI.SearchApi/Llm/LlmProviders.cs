namespace PI.SearchApi.Llm;

/// <summary>
/// The LLM providers the API can talk to (ADR-0015, ADR-0019). Every one except Anthropic speaks the OpenAI
/// chat-completions protocol, so it is "the OpenAI client with a different base URL"; Anthropic uses its own SDK.
/// </summary>
public static class LlmProviders
{
    /// <summary>A local Ollama server, reached through its OpenAI-compatible API. The presenter's default: no key, works offline.</summary>
    public const string Ollama = "ollama";

    /// <summary>The OpenAI API, with the learner's own key.</summary>
    public const string OpenAI = "openai";

    /// <summary>The Claude API through Anthropic's official SDK, with the learner's own key.</summary>
    public const string Anthropic = "anthropic";

    /// <summary>An Azure OpenAI resource, through its OpenAI-compatible v1 endpoint. Models are deployment names.</summary>
    public const string Azure = "azure";

    /// <summary>Google Gemini, through its OpenAI-compatible API.</summary>
    public const string Google = "google";

    /// <summary>Any other OpenAI-compatible server: LM Studio, vLLM, OpenRouter and so on.</summary>
    public const string Compatible = "compat";

    /// <summary>The catalogue, in the order the UI lists it: local first, then hosted.</summary>
    public static readonly IReadOnlyList<LlmProviderDefinition> Definitions =
    [
        new(Ollama, "Ollama", "Local models on this machine", KeyVariable: null,
            DefaultBaseUrl: "http://localhost:11434", NeedsKey: false, IsLocal: true, EnabledByDefault: true),
        new(OpenAI, "OpenAI", "GPT models, with your API key", "OPENAI_API_KEY",
            DefaultBaseUrl: "https://api.openai.com/v1", NeedsKey: true, IsLocal: false, EnabledByDefault: true),
        new(Anthropic, "Anthropic", "Claude models, with your API key", "ANTHROPIC_API_KEY",
            DefaultBaseUrl: "https://api.anthropic.com", NeedsKey: true, IsLocal: false, EnabledByDefault: true),
        new(Google, "Google Gemini", "Gemini models, through Google's OpenAI-compatible API", "GEMINI_API_KEY",
            DefaultBaseUrl: "https://generativelanguage.googleapis.com/v1beta/openai", NeedsKey: true, IsLocal: false, EnabledByDefault: true),
        // Azure has no default address: every resource has its own (https://<resource>.openai.azure.com/openai/v1).
        new(Azure, "Azure OpenAI", "Your Azure OpenAI resource; models are deployment names", "AZURE_OPENAI_API_KEY",
            DefaultBaseUrl: null, NeedsKey: true, IsLocal: false, EnabledByDefault: false),
        // LM Studio's default address. Off until a learner turns it on, so a server that isn't running isn't polled.
        new(Compatible, "OpenAI-compatible", "LM Studio, vLLM, OpenRouter or any server with an OpenAI-style API", "OPENAI_COMPAT_API_KEY",
            DefaultBaseUrl: "http://localhost:1234/v1", NeedsKey: false, IsLocal: false, EnabledByDefault: false),
    ];

    public static readonly IReadOnlyList<string> All = [.. Definitions.Select(d => d.Id)];

    /// <summary>The definition for a provider ID, or null if the API doesn't know it.</summary>
    public static LlmProviderDefinition? Find(string? id) => Definitions.FirstOrDefault(d => d.Id == id);
}
