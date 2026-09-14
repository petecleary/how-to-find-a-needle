namespace PI.SearchApi.Embeddings;

/// <summary>
/// The embedding model can't be used — for example, the ONNX files haven't been downloaded. This is an
/// expected "unavailable" state, not a bug, so it maps to <c>503 Service Unavailable</c> with fix-it
/// guidance instead of a stack trace (ADR-0003, ADR-0009). Stages that don't embed keep working.
/// </summary>
public sealed class EmbeddingModelUnavailableException(string message) : Exception(message);
