using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PI.SearchApi.Embeddings;
using PI.SearchApi.Llm;

namespace PI.SearchApi.Endpoints;

/// <summary>
/// Turns "a dependency isn't available" into an RFC 9457 ProblemDetails <c>503</c>, with the fix in
/// <c>detail</c> (ADR-0003): a missing embedding model, or an LLM that isn't running or configured (ADR-0015).
/// Every other exception falls through to the default handler.
/// </summary>
public sealed class ServiceUnavailableExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<ServiceUnavailableExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var title = exception switch
        {
            EmbeddingModelUnavailableException => "Embedding model unavailable",
            LlmUnavailableException => "LLM unavailable",
            _ => null,
        };

        if (title is null)
        {
            return false;
        }

        logger.LogWarning("Returning 503: {Reason}", exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = title,
                Detail = exception.Message,
            },
        });
    }
}
