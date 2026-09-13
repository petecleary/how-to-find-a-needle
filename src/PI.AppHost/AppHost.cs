var builder = DistributedApplication.CreateBuilder(args);

// Spin up Postgres container and create a DB with pgvector support     
var dbName = "pi-teach-db-search";
var dataVolume = "pgvector-data-search";
var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "pg16")
    .WithPgAdmin(pgAdmin => pgAdmin.WithExplicitStart())
    .WithDataVolume(dataVolume);
var postgresdb = postgres.AddDatabase(dbName);


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

