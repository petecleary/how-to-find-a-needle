using FastEndpoints;
using PI.SearchApi.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddFastEndpoints();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// The connection string is injected by the Aspire AppHost — run the app via PI.AppHost.
var postgresConnectionString = builder.Configuration.GetConnectionString("pi-teach-db-search")
    ?? throw new InvalidOperationException(
        "Connection string 'pi-teach-db-search' was not found. Start the app via the PI.AppHost project.");

builder.Services.AddSingleton<IDatabaseManager>(_ => new DatabaseManager(postgresConnectionString));
builder.Services.AddSingleton<IProductRepository>(_ => new ProductRepository(postgresConnectionString));

var app = builder.Build();

await app.Services.GetRequiredService<IDatabaseManager>().InitDbAsync();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseFastEndpoints();

app.MapDefaultEndpoints();

app.Run();
