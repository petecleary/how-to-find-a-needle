using Xunit;

namespace PI.SearchApi.IntegrationTests.BakeOff;

/// <summary>
/// The bake-off starts its own AppHost for each model, so it must never run alongside the shared AppHost fixture:
/// both would use the same Postgres data volume.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class BakeOffCollection
{
    public const string Name = "BakeOff";
}
