using Xunit;

namespace PI.SearchApi.Tests.Data;

public sealed class DataAssetsTests
{
    // The catalog validation tests read the API's data files from the test output folder.
    // This checks the project reference copies them, so a failure here points at the build, not the data.
    [Fact]
    public void InitSql_AfterBuild_IsCopiedToTestOutput()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "assets", "data", "init.sql");

        Assert.True(File.Exists(path), $"Expected the schema file at {path}.");
    }
}
