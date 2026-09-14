using System.Text.RegularExpressions;
using FluentValidation;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Ontology;

namespace PI.SearchApi.Endpoints.Search;

/// <summary>
/// The request limits every search endpoint's validator shares (ADR-0003). Each stage has its own
/// validator so a stage can differ (Stage 1 ignores the query), but the limits live here, once.
/// </summary>
public static partial class SearchRequestRules
{
    public static readonly IReadOnlyList<string> Audiences = ["novice", "enthusiast", "expert"];

    public static void AddTo(AbstractValidator<SearchRequest> validator, IOntology ontology, bool requireQuery)
    {
        validator.RuleFor(r => r.Page).GreaterThanOrEqualTo(1);
        validator.RuleFor(r => r.PageSize).InclusiveBetween(1, 50);

        if (requireQuery)
        {
            validator.RuleFor(r => r.Query).NotEmpty().MaximumLength(500);
        }

        validator.RuleFor(r => r.Filters).NotNull();
        validator.RuleFor(r => r.Context).NotNull();
        validator.RuleFor(r => r.Options).NotNull();

        validator.RuleFor(r => r.Options.CandidateDepth).InclusiveBetween(10, 200).When(r => r.Options is not null);
        validator.RuleFor(r => r.Options.RrfK).InclusiveBetween(1, 1000).When(r => r.Options is not null);
        validator.RuleFor(r => r.Options.KeywordWeight).InclusiveBetween(0, 10).When(r => r.Options is not null);
        validator.RuleFor(r => r.Options.VectorWeight).InclusiveBetween(0, 10).When(r => r.Options is not null);
        validator.RuleFor(r => r.Options.Audience)
            .Must(a => Audiences.Contains(a))
            .WithMessage("'Audience' must be one of: novice, enthusiast, expert.")
            .When(r => r.Options is not null);

        // Categories must be taxonomy notations: a typo would otherwise silently match nothing.
        validator.RuleForEach(r => r.Filters.Categories)
            .Must(notation => ontology.TryGetConcept(notation, out _))
            .WithMessage("'{PropertyValue}' is not a taxonomy notation. GET /api/taxonomy lists them.")
            .When(r => r.Filters is not null);

        // Spec keys are camelCase identifiers. Containment is safe with any key, but this keeps traces tidy (ADR-0007).
        validator.RuleForEach(r => r.Filters.Specs)
            .Must(spec => SpecKeyPattern().IsMatch(spec.Key))
            .WithMessage("Spec keys must match ^[a-zA-Z][a-zA-Z0-9]*$.")
            .When(r => r.Filters is not null);

        validator.RuleFor(r => r.Filters.MaxPrice)
            .GreaterThanOrEqualTo(r => r.Filters.MinPrice)
            .When(r => r.Filters is { MinPrice: not null, MaxPrice: not null });
    }

    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9]*$")]
    private static partial Regex SpecKeyPattern();
}
