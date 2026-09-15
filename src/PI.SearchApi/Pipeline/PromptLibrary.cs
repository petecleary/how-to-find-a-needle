using System.Collections.Concurrent;

namespace PI.SearchApi.Pipeline;

/// <summary>
/// Reads prompt files from <c>assets/prompts/</c> (root CLAUDE.md: prompts live in markdown files, not C# strings), so
/// a prompt can be read, reviewed and changed without touching code. Each file is read once and kept.
/// </summary>
public sealed class PromptLibrary(string promptsDirectory)
{
    private readonly ConcurrentDictionary<string, string> _prompts = new();

    public string Directory => promptsDirectory;

    /// <param name="fileName">A file in the prompts folder, e.g. <c>rag-system.md</c>.</param>
    public string Get(string fileName) =>
        _prompts.GetOrAdd(fileName, name => File.ReadAllText(Path.Combine(promptsDirectory, name)).ReplaceLineEndings("\n").Trim());
}
