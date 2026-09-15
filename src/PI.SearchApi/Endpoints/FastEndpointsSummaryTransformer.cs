using FastEndpoints;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace PI.SearchApi.Endpoints;

/// <summary>
/// Copies each endpoint's FastEndpoints <c>Summary(...)</c> text into the OpenAPI document, so it reaches
/// Scalar and the UI's generated TypeScript types (ADR-0014).
/// </summary>
/// <remarks>
/// FastEndpoints writes summaries for its own Swagger package (FastEndpoints.Swagger). This API uses
/// ASP.NET Core's built-in generator (Microsoft.AspNetCore.OpenApi), which never reads them, so without
/// this transformer every operation in <c>/openapi/v1.json</c> had no summary.
/// </remarks>
public sealed class FastEndpointsSummaryTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)
    {
        // FastEndpoints stores each endpoint's definition in its routing metadata, which the generator exposes here.
        var definition = context.Description.ActionDescriptor.EndpointMetadata.OfType<EndpointDefinition>().FirstOrDefault();
        var summary = definition?.EndpointSummary;

        if (!string.IsNullOrWhiteSpace(summary?.Summary))
        {
            operation.Summary = summary.Summary;
        }

        if (!string.IsNullOrWhiteSpace(summary?.Description))
        {
            operation.Description = summary.Description;
        }

        return Task.CompletedTask;
    }
}
