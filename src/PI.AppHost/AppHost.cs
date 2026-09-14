var builder = DistributedApplication.CreateBuilder(args);

// Postgres with the pgvector extension: one database serves structured filters (B-tree, JSONB),
// full-text search (tsvector) and vector search (pgvector), so every stage queries the same rows.
// The image tag is pinned: a floating tag could change pgvector's behaviour between rehearsals.
// The data volume persists between runs, so the seeder only writes products that changed
// (ADR-0006). Delete the volume to reset the data.
var dbName = "pi-teach-db-search";
var dataVolume = "pgvector-data-search";
var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "0.8.3-pg16")
    // Aspire.Hosting.PostgreSQL 13.4.6 bakes in pgAdmin 9.15.0; pin the current release
    // explicitly instead of carrying pgAdmin's own "update available" nag every run.
    .WithPgAdmin(pgAdmin => pgAdmin.WithExplicitStart().WithImageTag("9.17"))
    .WithDataVolume(dataVolume);
var postgresdb = postgres.AddDatabase(dbName);

// The API seeds the database before it accepts requests, so WaitFor + the health check
// keep the dashboard honest: "healthy" means the data is ready.
var searchApi = builder.AddProject<global::Projects.PI_SearchApi>("searchapi")
    .WithReference(postgresdb)
    .WaitFor(postgresdb)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithUrlForEndpoint("https", url =>
    {
        url.Url = "/scalar/v1";
        url.DisplayText = "Search API (Scalar)";
    });

builder.Build().Run();
