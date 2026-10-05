using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PI.SearchApi.Contracts;

namespace PI.SearchApi.Llm;

// LLM model registry — bring your own model (Stages 6–7)
//
// What:     Turns a request's options.model ("provider/model", or null for the configured default) into a chat
//           client for that one request, using the learner's saved base URLs and keys from configuration or the
//           session. Also lists each provider's models, live, for the UI's picker.
// Strength: The model becomes a request option like the audience: the same evidence, prompt and validators can be
//           run against two models side by side, with no restart, so you see what the model contributes.
// Failure:  More moving parts than one configured client: six providers, each with its own address, key and model
//           list. A provider that is down shows as a problem in the list and a 503 on the answer, never a silent fallback.
// Decision: docs/decisions/0019-bring-your-own-model.md
public sealed class LlmModelRegistry(
    IOptions<LlmOptions> defaults,
    LlmSettingsStore settings,
    ISecretStore secrets,
    IHttpClientFactory httpClients,
    ILoggerFactory loggerFactory,
    IHostEnvironment environment)
{
    /// <summary>The named HTTP client for model lists. It has no resilience handler: a retry would hide a provider that is down.</summary>
    public const string ModelListClientName = "llm-models";

    // A local server answers in milliseconds or isn't running; a hosted API may be slower to list its models.
    private static readonly TimeSpan LocalListTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan HostedListTimeout = TimeSpan.FromSeconds(15);

    /// <summary>The model used when a request doesn't name one: <c>Llm:Provider</c>/<c>Llm:Model</c> (ADR-0015).</summary>
    public ModelRef DefaultModel => new(defaults.Value.Provider, defaults.Value.Model);

    /// <summary>
    /// Builds the client for one request. A client is built every time, not cached: it costs microseconds, and a
    /// cache would be state the learner can't see. Throws <see cref="LlmUnavailableException"/> (→ 503) when the
    /// model can't be used: no key, no address, or an unknown provider.
    /// </summary>
    public LlmCall Resolve(string? model)
    {
        var options = OptionsFor(model);
        var client = LlmClientFactory.Create(options, loggerFactory, environment.IsDevelopment());

        return new LlmCall(client, options, LlmChatOptions.For(options));
    }

    /// <summary>The effective settings for a model: the <c>Llm</c> limits, plus the provider's address and key.</summary>
    public LlmOptions OptionsFor(string? model)
    {
        var configured = defaults.Value;
        ModelRef modelRef;

        if (model is null)
        {
            modelRef = DefaultModel;
        }
        else if (!ModelRef.TryParse(model, out modelRef))
        {
            // The request validator rejects this with a 400 first; this guards other callers.
            throw new LlmUnavailableException($"'{model}' is not a model reference: use provider/model with one of: {string.Join(", ", LlmProviders.All)}.");
        }

        var provider = LlmProviders.Find(modelRef.Provider);

        return new LlmOptions
        {
            Provider = modelRef.Provider,
            Model = modelRef.Model,
            Endpoint = provider is null ? configured.Endpoint : BaseUrlFor(provider),
            ApiKey = secrets.Get(modelRef.Provider),
            MaxOutputTokens = configured.MaxOutputTokens,
            TimeoutSeconds = configured.TimeoutSeconds,
        };
    }

    /// <summary>Every provider with its settings and key source. No network calls, so it is always fast.</summary>
    public IReadOnlyList<LlmProviderStatus> Providers() =>
    [
        .. LlmProviders.Definitions.Select(provider =>
        {
            var saved = settings.Current.For(provider.Id);

            return new LlmProviderStatus
            {
                Id = provider.Id,
                Name = provider.Name,
                Detail = provider.Detail,
                NeedsKey = provider.NeedsKey,
                IsLocal = provider.IsLocal,
                Enabled = IsEnabled(provider),
                KeyVariable = provider.KeyVariable,
                KeySource = secrets.SourceOf(provider.Id),
                DefaultBaseUrl = provider.DefaultBaseUrl,
                BaseUrl = BaseUrlFor(provider),
                ExtraModels = saved.ExtraModels,
            };
        }),
    ];

    /// <summary>
    /// Lists every enabled provider's models, all at once, live: nothing is cached, so a provider that just stopped
    /// shows as stopped. The default model is always listed, even if its provider can't be reached.
    /// </summary>
    public async Task<ModelCatalogue> ModelsAsync(CancellationToken ct)
    {
        var enabled = LlmProviders.Definitions.Where(IsEnabled).ToList();
        var lists = await Task.WhenAll(enabled.Select(provider => ListAsync(provider, ct)));

        return new ModelCatalogue
        {
            Default = DefaultModel.ToString(),
            Providers = [.. lists.Select(WithDefaultModel)],
        };
    }

    /// <summary>Lists one provider's models now, whether or not it is enabled, and says what happened.</summary>
    public async Task<ConnectionTest?> TestAsync(string providerId, CancellationToken ct)
    {
        if (LlmProviders.Find(providerId) is not { } provider)
        {
            return null;
        }

        var list = await ListAsync(provider, ct);
        if (list.Problem is not null)
        {
            return new ConnectionTest { Ok = false, Message = list.Problem, Models = 0 };
        }

        var count = list.Models.Count;
        var message = provider.IsLocal
            ? $"Connected · {count} local model{(count == 1 ? "" : "s")}"
            : $"Connected · {count} model{(count == 1 ? "" : "s")}";

        return new ConnectionTest { Ok = true, Message = message, Models = count };
    }

    private bool IsEnabled(LlmProviderDefinition provider) =>
        settings.Current.For(provider.Id).Enabled ?? (provider.EnabledByDefault || provider.Id == DefaultModel.Provider);

    // The learner's saved address wins; for Ollama, Llm:Endpoint comes next (ADR-0015 made it Ollama's setting);
    // then the catalogue's default.
    private string? BaseUrlFor(LlmProviderDefinition provider)
    {
        var baseUrl = settings.Current.For(provider.Id).BaseUrl;

        if (string.IsNullOrWhiteSpace(baseUrl) && provider.Id == LlmProviders.Ollama)
        {
            baseUrl = defaults.Value.Endpoint;
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = provider.DefaultBaseUrl;
        }

        return baseUrl?.TrimEnd('/');
    }

    private ProviderModels WithDefaultModel(ProviderModels list)
    {
        var defaultModel = DefaultModel;
        if (list.Id != defaultModel.Provider || list.Models.Any(m => m.Model == defaultModel.Model))
        {
            return list with { Models = [.. list.Models.Select(m => m with { IsDefault = m.Ref == defaultModel.ToString() })] };
        }

        ModelInfo configuredModel = new() { Ref = defaultModel.ToString(), Model = defaultModel.Model, IsDefault = true };
        return list with { Models = [configuredModel, .. list.Models] };
    }

    private async Task<ProviderModels> ListAsync(LlmProviderDefinition provider, CancellationToken ct)
    {
        ProviderModels Result(IReadOnlyList<ModelInfo> models, string? problem = null) => new()
        {
            Id = provider.Id,
            Name = provider.Name,
            IsLocal = provider.IsLocal,
            Models = models,
            Problem = problem,
        };

        var key = secrets.Get(provider.Id);
        var baseUrl = BaseUrlFor(provider);
        var extraModels = settings.Current.For(provider.Id).ExtraModels
            .Select(name => new ModelInfo { Ref = new ModelRef(provider.Id, name).ToString(), Model = name, IsDefault = false })
            .ToList();

        if (provider.NeedsKey && key is null)
        {
            return Result(extraModels, $"No API key. Add one in Models and API keys, or set {provider.KeyVariable} with dotnet user-secrets.");
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return Result(extraModels, $"{provider.Name} needs a base URL. Set it in Models and API keys.");
        }

        try
        {
            var listed = await FetchModelsAsync(provider, baseUrl, key, ct);
            return Result([.. listed.Concat(extraModels).DistinctBy(m => m.Model).OrderBy(m => m.Model, StringComparer.OrdinalIgnoreCase)]);
        }
        catch (Exception exception) when (!ct.IsCancellationRequested
            && exception is HttpRequestException or TaskCanceledException or JsonException or UriFormatException)
        {
            var problem = provider.IsLocal
                ? $"Not running at {baseUrl}. Start it (e.g. `ollama serve`) and test again."
                : $"Could not reach {provider.Name}: {exception.Message}";

            return Result(extraModels, problem);
        }
    }

    private async Task<IReadOnlyList<ModelInfo>> FetchModelsAsync(
        LlmProviderDefinition provider,
        string baseUrl,
        string? key,
        CancellationToken ct)
    {
        // Azure lists deployments only through its management API, not with an API key: they are typed in as extra models.
        if (provider.Id == LlmProviders.Azure)
        {
            return [];
        }

        using var http = httpClients.CreateClient(ModelListClientName);
        http.Timeout = provider.IsLocal ? LocalListTimeout : HostedListTimeout;

        // Each provider lists models its own way. Ollama's native /api/tags (not /v1/models) also reports capabilities.
        using var request = provider.Id switch
        {
            LlmProviders.Ollama => new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/tags"),
            LlmProviders.Anthropic => new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/v1/models?limit=100"),
            _ => new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/models"),
        };

        if (provider.Id == LlmProviders.Anthropic)
        {
            request.Headers.Add("x-api-key", key);
            request.Headers.Add("anthropic-version", "2023-06-01");
        }
        else if (key is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        }

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        return provider.Id == LlmProviders.Ollama
            ? ReadOllamaModels(document.RootElement)
            : ReadModelIds(provider, document.RootElement);
    }

    // { "models": [ { "name": "qwen3.6:35b", "capabilities": ["completion", "tools"], "details": { "context_length": 262144 } } ] }
    private static List<ModelInfo> ReadOllamaModels(JsonElement root)
    {
        var models = new List<ModelInfo>();
        if (!root.TryGetProperty("models", out var entries))
        {
            return models;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.TryGetProperty("name", out var name) && name.GetString() is { Length: > 0 } modelName)
            {
                string[]? capabilities = entry.TryGetProperty("capabilities", out var caps) && caps.ValueKind == JsonValueKind.Array
                    ? [.. caps.EnumerateArray().Select(c => c.GetString() ?? "")]
                    : null;

                int? contextLength = entry.TryGetProperty("details", out var details)
                    && details.TryGetProperty("context_length", out var context)
                    && context.TryGetInt32(out var tokens)
                        ? tokens
                        : null;

                models.Add(new ModelInfo
                {
                    Ref = new ModelRef(LlmProviders.Ollama, modelName).ToString(),
                    Model = modelName,
                    IsDefault = false,
                    Tools = capabilities?.Contains("tools"),
                    Vision = capabilities?.Contains("vision"),
                    ContextLength = contextLength,
                });
            }
        }

        return models;
    }

    // The OpenAI shape, which Anthropic also uses: { "data": [ { "id": "gpt-5" } ] }. Gemini prefixes ids with "models/".
    private static List<ModelInfo> ReadModelIds(LlmProviderDefinition provider, JsonElement root)
    {
        var models = new List<ModelInfo>();
        if (!root.TryGetProperty("data", out var entries))
        {
            return models;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("id", out var id) || id.GetString() is not { Length: > 0 } modelId)
            {
                continue;
            }

            modelId = modelId.StartsWith("models/", StringComparison.Ordinal) ? modelId["models/".Length..] : modelId;

            if (provider.Id == LlmProviders.OpenAI && !IsOpenAIChatModel(modelId))
            {
                continue;
            }

            models.Add(new ModelInfo { Ref = new ModelRef(provider.Id, modelId).ToString(), Model = modelId, IsDefault = false });
        }

        return models;
    }

    // OpenAI's list includes embedding, audio and image models, which can't write an answer.
    private static bool IsOpenAIChatModel(string id) =>
        (id.StartsWith("gpt-", StringComparison.Ordinal) || id.StartsWith('o') || id.StartsWith("chatgpt", StringComparison.Ordinal))
        && !new[] { "audio", "realtime", "transcribe", "tts", "image", "search", "embedding" }.Any(id.Contains);
}
