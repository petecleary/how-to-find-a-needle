namespace PI.SearchApi.Tests;

/// <summary>Locates files the build doesn't copy to the test output: committed embeddings and downloaded models.</summary>
public static class TestPaths
{
    public static string RepositoryRoot { get; } = FindRoot();

    public static string SourceDataDirectory => Path.Combine(RepositoryRoot, "src", "PI.SearchApi", "assets", "data");

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
