using System.Text.RegularExpressions;

namespace PI.SearchApi.Pipeline;

/// <summary>
/// Fills <c>{{name}}</c> placeholders in a prompt file. Deliberately tiny, with no template language: the prompt the
/// trace shows is the file with values pasted in, nothing more. A placeholder with no value is an error, so a typo
/// can't silently send the model a literal "{{evidence}}".
/// </summary>
public static partial class PromptTemplate
{
    public static string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        var missing = Placeholder().Matches(template)
            .Select(m => m.Groups["name"].Value)
            .Where(name => !values.ContainsKey(name))
            .Distinct()
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"The prompt has placeholders with no value: {string.Join(", ", missing)}.");
        }

        return Placeholder().Replace(template, m => values[m.Groups["name"].Value]);
    }

    [GeneratedRegex(@"\{\{(?<name>[a-zA-Z][a-zA-Z0-9]*)\}\}")]
    private static partial Regex Placeholder();
}
