using Xunit;

namespace PI.SearchApi.IntegrationTests;

/// <summary>
/// Finds files in the repository from the test output folder: the golden queries the tests assert,
/// and the downloaded ONNX models whose absence turns vector tests into clear skips.
/// </summary>
public static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();

    public static string DataDirectory => Path.Combine(Root, "src", "PI.SearchApi", "assets", "data");

    public static string GoldenQueriesFile => Path.Combine(DataDirectory, "golden-queries.json");

    public static string NomicModelFile => Path.Combine(Root, "src", "PI.SearchApi", "assets", "models", "nomic", "model_int8.onnx");

    /// <summary>
    /// Vector, hybrid and ontology stages embed the query with the local Nomic model (ADR-0009).
    /// Without the model they return 503 by design, so their golden-query tests skip rather than fail.
    /// </summary>
    public static void SkipUnlessNomicModelIsPresent() =>
        Assert.SkipUnless(
            File.Exists(NomicModelFile),
            $"The Nomic ONNX model isn't downloaded ({NomicModelFile}). See src/PI.SearchApi/assets/models/README.md.");

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "how-to-find-a-needle.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not find the repository root (how-to-find-a-needle.slnx).");
    }
}
