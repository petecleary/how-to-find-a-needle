namespace PI.SearchApi.Contracts;

/// <summary>
/// The body of a <c>400 Bad Request</c>: RFC 9457 ProblemDetails with one entry per failed validation rule (ADR-0003).
/// </summary>
/// <remarks>
/// FastEndpoints writes this JSON itself (<c>Errors.UseProblemDetails()</c> in <c>Program.cs</c>). Its own
/// ProblemDetails class is also an <c>IResult</c>, which ASP.NET Core's OpenAPI generator leaves out of the
/// document, so this record describes the same JSON for the document and the UI's generated types.
/// <c>OpenApiDocumentTests</c> checks a real 400 against it.
/// </remarks>
public sealed record ValidationProblem(
    string Type,
    string Title,
    int Status,
    string Instance,
    string TraceId,
    string Detail,
    IReadOnlyList<ValidationProblemError> Errors);
