using System.ClientModel;
using System.ClientModel.Primitives;
using Anthropic;
using Microsoft.Extensions.AI;
using OpenAI;
using PI.SearchApi.Pipeline;

namespace PI.SearchApi.Llm;

// LLM provider — one IChatClient for Stages 6–7
//
// What:     Builds the single Microsoft.Extensions.AI IChatClient the AI stages depend on, from the Llm
//           settings: Ollama (through its OpenAI-compatible /v1 API), OpenAI, or Claude through Anthropic's
//           official SDK. Middleware adds OpenTelemetry, so prompts and timings show in the Aspire dashboard.
// Strength: The LLM is a replaceable dependency, not the architecture. Every provider difference (endpoints,
//           keys, sampling, reasoning) is in this folder; RAG and pedagogy code sees only IChatClient.
// Failure:  An abstraction can't hide behaviour: models still differ in fluency, formatting and speed, which
//           is why the answers are validated after generation, whatever the provider.
// Decision: docs/adr/0015-llm-hosting-and-client.md
public static class LlmClientFactory
{
    /// <summary>
    /// Builds the chat client. Throws <see cref="LlmUnavailableException"/> (→ 503 with guidance) for a missing
    /// key or an unknown provider, so a misconfigured LLM never stops Stages 1–5 from starting.
    /// </summary>
    public static IChatClient Create(LlmOptions options, ILoggerFactory loggerFactory, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(options.Model))
        {
            throw LlmUnavailableException.Misconfigured(options, "Llm:Model is not set.");
        }

        var providerClient = options.Provider switch
        {
            LlmProviders.Ollama => CreateOllamaClient(options),
            LlmProviders.OpenAI => CreateOpenAIClient(options),
            LlmProviders.Anthropic => CreateAnthropicClient(options),
            _ => throw LlmUnavailableException.Misconfigured(
                options,
                $"Llm:Provider '{options.Provider}' is not one of: {string.Join(", ", LlmProviders.All)}."),
        };

        var builder = providerClient.AsBuilder()
            // Emits the GenAI semantic-convention spans under our own source name, which the Aspire service
            // defaults already subscribe to. Sensitive data (the prompts and output) is recorded because the
            // point of this app is to show them; never enable it for real user data in production.
            .UseOpenTelemetry(loggerFactory, PipelineTelemetry.SourceName, client => client.EnableSensitiveData = true);

        if (isDevelopment)
        {
            builder.UseLogging(loggerFactory);
        }

        return builder.Build();
    }

    // Ollama speaks the OpenAI chat-completions protocol under /v1, so the OpenAI client serves it. It needs no
    // key, but the protocol requires one to be sent, so a placeholder is used.
    private static IChatClient CreateOllamaClient(LlmOptions options)
    {
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint))
        {
            throw LlmUnavailableException.Misconfigured(options, "Llm:Endpoint is not a valid URL.");
        }

        var clientOptions = OpenAIClientOptionsFor(options);
        clientOptions.Endpoint = new Uri(endpoint, "/v1");

        return new OpenAIClient(new ApiKeyCredential("ollama"), clientOptions)
            .GetChatClient(options.Model)
            .AsIChatClient();
    }

    private static IChatClient CreateOpenAIClient(LlmOptions options)
    {
        var apiKey = RequireApiKey(options);

        return new OpenAIClient(new ApiKeyCredential(apiKey), OpenAIClientOptionsFor(options))
            .GetChatClient(options.Model)
            .AsIChatClient();
    }

    // The official SDK's own IChatClient, not Anthropic's OpenAI-compatible endpoint: native streaming,
    // effort and refusal reporting (a refusal arrives as ChatFinishReason.ContentFilter).
    private static IChatClient CreateAnthropicClient(LlmOptions options)
    {
        var apiKey = RequireApiKey(options);

        var client = new AnthropicClient
        {
            ApiKey = apiKey,
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
            // No retries: a failed demo call should fail visibly, not hang while the SDK quietly tries again.
            MaxRetries = 0,
        };

        return client.AsIChatClient(options.Model, options.MaxOutputTokens, AnthropicThinkingMode.Adaptive);
    }

    private static OpenAIClientOptions OpenAIClientOptionsFor(LlmOptions options) => new()
    {
        NetworkTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
        // No retries (ADR-0015): the SDK's default policy would retry a failed call and hide the failure.
        RetryPolicy = new ClientRetryPolicy(maxRetries: 0),
    };

    private static string RequireApiKey(LlmOptions options) =>
        string.IsNullOrWhiteSpace(options.ApiKey)
            ? throw LlmUnavailableException.Misconfigured(options, $"Llm:ApiKey is not set for the {options.Provider} provider.")
            : options.ApiKey;
}
