using System.Text.Json.Serialization;
using FastEndpoints;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Npgsql;
using PI.SearchApi.Data;
using PI.SearchApi.Embeddings;
using PI.SearchApi.Endpoints;
using PI.SearchApi.Llm;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Fusion;
using PI.SearchApi.Pipeline.Hybrid;
using PI.SearchApi.Pipeline.Keyword;
using PI.SearchApi.Pipeline.Ontology;
using PI.SearchApi.Pipeline.Pedagogy;
using PI.SearchApi.Pipeline.Rag;
using PI.SearchApi.Pipeline.Structured;
using PI.SearchApi.Pipeline.Vector;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// --- API plumbing (ADR-0003) --------------------------------------------------
// Errors are RFC 9457 ProblemDetails. Missing models become a 503 with fix-it guidance.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ServiceUnavailableExceptionHandler>();
builder.Services.AddFastEndpoints();

// Enums travel as PascalCase strings ("Incompatible", "InConcept"), both in responses and in the
// OpenAPI document the UI generates its types from (ADR-0003, ADR-0014). Strict number handling
// stops the document describing every number as "number | string".
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});
builder.Services.AddOpenApi(options => options.AddOperationTransformer<FastEndpointsSummaryTransformer>());

var dataDirectory = Path.Combine(AppContext.BaseDirectory, "assets", "data");

// --- Data (ADR-0006) --------------------------------------------------------
// Aspire injects the connection string by name; the API is not supported standalone
// (root CLAUDE.md), so a missing connection string means "run this via PI.AppHost".
// UseVector() registers the Npgsql <-> pgvector type mappings (Vector).
builder.AddNpgsqlDataSource("pi-teach-db-search", configureDataSourceBuilder: b => b.UseVector());
builder.Services.AddSingleton<DatabaseSeeder>();

// --- Embeddings (ADR-0009) --------------------------------------------------
// The generator loads its ONNX model once, on first use, so it's a singleton. Models are downloaded
// into the project folder (not copied to bin/), so the path is relative to the content root.
builder.Services.Configure<EmbeddingsOptions>(builder.Configuration.GetSection(EmbeddingsOptions.SectionName));
builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(services => new NomicOnnxEmbeddingGenerator(
    Path.Combine(builder.Environment.ContentRootPath, "assets", "models", "nomic"),
    services.GetRequiredService<ILogger<NomicOnnxEmbeddingGenerator>>()));
builder.Services.AddSingleton<ISearchEmbedder, SearchEmbedder>();

// --- Shared pipeline services (ADR-0004, ADR-0013) ----------------------------
// The ontology loads the Turtle graph once, so it's a singleton. The filter builder only reads it.
builder.Services.AddSingleton<IOntology>(_ => new DomainOntology(dataDirectory));
builder.Services.AddSingleton<SqlFilterBuilder>();
builder.Services.AddTransient<ProductLookup>();

// --- Stage 1: Structured search (ADR-0007) ----------------------------------
builder.Services.AddTransient<IStructuredSearch, StructuredSearch>();

// --- Stage 2: Keyword search, BM25-style (ADR-0008) -------------------------
builder.Services.AddTransient<IKeywordSearch, KeywordSearch>();

// --- Stage 3: Vector search, pgvector (ADR-0010) ----------------------------
builder.Services.AddTransient<IVectorSearch, VectorSearch>();

// --- Stage 4: Hybrid search, Reciprocal Rank Fusion (ADR-0011) ---------------
// RRF is a pure function with no state, so one instance serves every request.
builder.Services.AddSingleton<IRankFusion, ReciprocalRankFusion>();
builder.Services.AddTransient<IHybridSearch, HybridSearch>();

// --- Stage 5: Ontology — concepts, expansion and domain rules (ADR-0013) ------
// The matcher indexes every label once, and the other steps only read the ontology: singletons.
builder.Services.AddSingleton<LabelMatcher>();
builder.Services.AddSingleton<QueryExpander>();
builder.Services.AddSingleton<ConceptClassifier>();
builder.Services.AddSingleton<CompatibilityEvaluator>();
builder.Services.AddSingleton<QueryRequirementExtractor>();
builder.Services.AddSingleton<DeviceFitFinder>();
builder.Services.AddTransient<TargetDeviceResolver>();
builder.Services.AddTransient<IOntologySearch, OntologySearch>();

// --- LLM for Stages 6–7 (ADR-0015) --------------------------------------------
// One IChatClient, built from the Llm settings (appsettings.json + the user-secret API key). It's a singleton
// created on first use: a misconfigured or stopped LLM turns into a 503 on the AI stages, never a failed startup.
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.SectionName));
builder.Services.AddSingleton<IChatClient>(services => LlmClientFactory.Create(
    services.GetRequiredService<IOptions<LlmOptions>>().Value,
    services.GetRequiredService<ILoggerFactory>(),
    builder.Environment.IsDevelopment()));
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<LlmWarmUpService>();
}

// --- Stage 6: RAG — evidence set, streamed grounded answer, validation (ADR-0016) ------
// Prompts are markdown files read once. The evidence builder only reads the ontology: a singleton.
builder.Services.AddSingleton(new PromptLibrary(Path.Combine(AppContext.BaseDirectory, "assets", "prompts")));
builder.Services.AddSingleton<EvidenceSetBuilder>();
builder.Services.AddTransient<IRagSearch, RagSearch>();
builder.Services.AddTransient<IAnswerGenerator, AnswerGenerator>();

// --- Stage 7: Pedagogy — the answer, then an audience-aware explanation or the baseline (ADR-0017) ---
// Reuses Stage 6's retrieval and answer; adds the prompt builder (reads prompt files only: a singleton) and the engine.
builder.Services.AddSingleton<PedagogyPromptBuilder>();
builder.Services.AddTransient<IPedagogyEngine, PedagogyEngine>();

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

app.UseFastEndpoints(c =>
{
    c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
    c.Serializer.Options.NumberHandling = JsonNumberHandling.Strict;
    c.Errors.UseProblemDetails();
});

app.MapDefaultEndpoints();

app.Run();
