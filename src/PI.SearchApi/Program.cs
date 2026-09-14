using Npgsql;
using PI.SearchApi.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// --- Data (ADR-0006) --------------------------------------------------------
// Aspire injects the connection string by name; the API is not supported standalone
// (root CLAUDE.md), so a missing connection string means "run this via PI.AppHost".
// UseVector() registers the Npgsql <-> pgvector type mappings (Vector, SparseVector).
builder.AddNpgsqlDataSource("pi-teach-db-search", configureDataSourceBuilder: b => b.UseVector());
builder.Services.AddSingleton<DatabaseSeeder>();

var app = builder.Build();

// Seed before the app starts accepting requests, so a "healthy" health check means the
// catalog is actually ready (ADR-0006). The Aspire dashboard's WaitFor relies on this.
await app.Services.GetRequiredService<DatabaseSeeder>().SeedAsync();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// TODO(Phase 2): builder.Services.AddFastEndpoints() and app.UseFastEndpoints() come back
// with the first endpoint (ADR-0003's GET /api/demo/queries). FastEndpoints throws at
// startup if it finds no endpoint declarations at all, so Phase 1 — which has none yet —
// leaves both calls out rather than adding a placeholder endpoint just to satisfy it.

app.MapDefaultEndpoints();

app.Run();
