# CLAUDE.md — PI.SearchApi (C# / API)

Repo-wide rules (teaching principles, commenting standard, vocabulary) are in the [root CLAUDE.md](../../CLAUDE.md). Decisions: [ADR-0002](../../docs/adr/0002-solution-structure-and-orchestration.md) (structure), [ADR-0003](../../docs/adr/0003-search-api-contract-and-debug-trace.md) (contract and trace), [ADR-0004](../../docs/adr/0004-pipeline-composition.md) (pipeline).

## Layout and responsibilities

| Folder | Holds |
|---|---|
| `Program.cs` | Composition root only: one clearly commented DI block per stage; seeding before `app.Run()` |
| `Extensions.cs` | Aspire service defaults (kept in-project; no ServiceDefaults project) |
| `Contracts/` | `SearchRequest`, `SearchResponse`, `ProductResult`, `DebugTrace`: shared by every stage |
| `Pipeline/` | Shared types (`Candidate`, `StageResult`, `TraceStep`, `SqlFilterBuilder`) |
| `Pipeline/{Technique}/` | One technique service + interface: `Structured/ Keyword/ Vector/ Fusion/ Ontology/ Rag/ Pedagogy/` |
| `Embeddings/` | `ISearchEmbedder`, `NomicOnnxEmbeddingGenerator` |
| `Endpoints/Search/{Stage}/` | Thin FastEndpoints endpoint + validator |
| `Endpoints/Demo/` | Golden queries, devices, taxonomy |
| `Data/` | `init.sql` runner, catalog loader, `DatabaseSeeder` |
| `assets/` | `data/` (catalog, ontology, `.rq`, embeddings, `init.sql`), `prompts/`, `models/` (gitignored) |

## Endpoints (FastEndpoints, REPR)

- **Thin:** validate → call **one** top-level pipeline service → page once → map to `SearchResponse`. No SQL, ranking or rule logic in an endpoint.
- Search is **POST** with the shared contract. Stages 6–7 also have `/answer` (SSE, or JSON when `Accept: application/json`).
- One FluentValidation validator per endpoint, in the same folder. Limits are in ADR-0003 (e.g. `pageSize` 1–50, `candidateDepth` 10–200).
- Errors are ProblemDetails. Missing models or an unreachable LLM return **503** with fix-it guidance in `detail`, never a stack trace.
- Every stage starts an OpenTelemetry `Activity`, so the Aspire dashboard shows the same pipeline as the trace.

## Naming conventions

- Standard .NET casing: PascalCase types, members and constants; camelCase locals and parameters; `I` prefix for interfaces; `Async` suffix on async methods.
- `CancellationToken ct` is the last parameter of every async method.
- Primary constructors instead of `_field` assignments.
- File name = type name, one public type per file. **Exception:** an endpoint folder's request, validator and endpoint may share one file.
- Namespaces follow folders: `PI.SearchApi.Pipeline.Keyword`.

| Kind | Pattern | Example |
|---|---|---|
| Endpoint | `{Stage}SearchEndpoint` | `KeywordSearchEndpoint` |
| Validator | `{Stage}SearchRequestValidator` | `HybridSearchRequestValidator` |
| Technique service | `I{Technique}Search` / `{Technique}Search` | `IVectorSearch` / `VectorSearch` |
| Fusion | `IRankFusion` / `ReciprocalRankFusion` | |
| LLM services | `IAnswerGenerator`, `IPedagogyEngine`, `LlmClientFactory` | |
| Embedding generator | `{Model}{Runtime}EmbeddingGenerator` | `NomicOnnxEmbeddingGenerator` |
| Builders / seeders | `*Builder`, `*Seeder` | `SqlFilterBuilder`, `DatabaseSeeder` |
| SQL constant | `private const string {Purpose}Sql` | `SearchByTsQuerySql` |
| Config section | PascalCase path | `Embeddings:Provider`, `Llm:Model` |

- Postgres tables and columns: `snake_case`. JSON on the wire: `camelCase`. Stage slugs and routes: kebab-case.
- Enum values on the wire are PascalCase strings (`Incompatible`, `InConcept`), matching ADR-0003.

## C# style

- `sealed` classes and records by default. Records for contracts and shared pipeline types.
- Return `IReadOnlyList<T>` / `IReadOnlyDictionary<K,V>` from services.
- File-scoped namespaces. Nullable enabled; no `!` suppression without a comment saying why it's safe.
- `async` all the way down; pass `ct` through everything. Cancellation stops LLM generation when the user switches stage.
- Inject the pooled `NpgsqlDataSource` (with `UseVector()`); don't pass connection strings (replaces the scaffold pattern in Phase 1).
- **DI lifetimes:** embedders, the ontology graph and the chat client are singletons (they load once). Search services are scoped or transient.
- Exceptions are for the unexpected. Expected "unavailable" states (missing model, LLM down) map to 503.
- Structured logging with message templates, not string interpolation: `logger.LogInformation("Seeded {Count} products ({Inserted} inserted, …)", …)`. Match the log lines named in roadmap acceptance criteria.
- Time trace steps with `Stopwatch`.

## SQL

- Raw Npgsql; no EF Core or Dapper. The SQL is what learners come to see.
- SQL is a `const string` next to the service that runs it, with a comment on the operators it uses (`@@`, `<=>`, `&&`, `@>`, `ts_rank_cd`, `SET LOCAL hnsw.ef_search`).
- **Always parameterised.** Never interpolate user input. The trace shows the exact statement and its parameters side by side, never interpolated.
- Filters come from `SqlFilterBuilder` and apply as pre-filters in every stage.
- `init.sql` is idempotent (`IF NOT EXISTS`); schema changes go there.

## Pipeline service skeleton

```csharp
namespace PI.SearchApi.Pipeline.Keyword;

// Stage 2 — Keyword search (BM25-style)
//
// What:     Postgres full-text search: websearch_to_tsquery + ts_rank_cd over weighted fields.
// Strength: Fast and exact; great for names, model numbers and specific terms.
// Failure:  Matches words, not meaning: misses synonyms ("power brick" vs "adapter")
//           and is fooled by shared words ("cordless" phone vs drill battery).
// Decision: docs/adr/0008-keyword-search-bm25-style.md
public sealed class KeywordSearch(NpgsqlDataSource dataSource) : IKeywordSearch
{
    // Explain each clause that teaches something: weights A/B, why ts_rank_cd, why LIMIT is candidateDepth.
    private const string SearchByTsQuerySql = """
        SELECT ...
        LIMIT @candidateDepth;
        """;

    public async Task<StageResult> SearchAsync(SearchRequest request, CancellationToken ct)
    {
        // Retrieve deep, page late: return candidateDepth items; the endpoint pages.
        // ... run the query, map candidates, record timing ...
        return new StageResult(candidates, [traceStep]);
    }
}
```

Composed stages call earlier services and **append** their own trace step after the steps they received.

## LLM code (Stages 6–7)

- Depend on `IChatClient` only. All provider differences live in `LlmClientFactory` ([ADR-0015](../../docs/adr/0015-llm-hosting-and-client.md)).
- Sampling is provider-specific: `Temperature = 0.1` for Ollama/OpenAI; **never send `temperature` to Anthropic**.
- No retries; 60 s timeout; output-token cap for a short summary.
- Stream with `GetStreamingResponseAsync` as `meta` / `delta` / `final` / `done` / `error` events. Validate citations, sentinels and headings after completion ([ADR-0016](../../docs/adr/0016-rag-grounding-and-citations.md), [ADR-0017](../../docs/adr/0017-pedagogy-engine.md)).
- Prompts are loaded from `assets/prompts/*.md`, and the trace records prompts, raw output and timings. **Never put an API key in the trace or logs.**
