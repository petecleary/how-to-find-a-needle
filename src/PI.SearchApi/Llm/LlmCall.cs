using Microsoft.Extensions.AI;

namespace PI.SearchApi.Llm;

/// <summary>
/// Everything one request needs to call its model (ADR-0019): the client, the settings it was built from (for the
/// trace and error messages) and the provider's per-call options. <see cref="LlmModelRegistry.Resolve"/> builds it.
/// </summary>
public sealed record LlmCall(IChatClient Client, LlmOptions Options, ChatOptions ChatOptions);
