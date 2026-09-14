using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Xunit;

namespace PI.SearchApi.IntegrationTests;

/// <summary>
/// Starts the whole AppHost (Postgres + seeded API) once and shares it across every
/// integration test class in the <see cref="AppHostCollection"/>.
/// </summary>
public sealed class AppHostFixture : IAsyncLifetime
{
    // Starting Postgres and seeding can take a while on a cold Docker cache.
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    private DistributedApplication? _app;

    public DistributedApplication App =>
        _app ?? throw new InvalidOperationException("The AppHost has not been started.");

    public HttpClient CreateSearchApiClient() => App.CreateHttpClient("searchapi");

    public async ValueTask InitializeAsync()
    {
        using var cts = new CancellationTokenSource(StartupTimeout);

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PI_AppHost>(cts.Token);
        _app = await appHost.BuildAsync(cts.Token);
        await _app.StartAsync(cts.Token);

        // The API seeds before it serves requests, so "healthy" means the catalog is loaded.
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("searchapi", cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class AppHostCollection : ICollectionFixture<AppHostFixture>
{
    public const string Name = "AppHost";
}
