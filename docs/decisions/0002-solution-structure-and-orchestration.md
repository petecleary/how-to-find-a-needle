# ADR-0002: Solution structure & orchestration

- **Status:** Accepted
- **Area:** Foundation
- **Related:** [ADR-0003](0003-search-api-contract-and-debug-trace.md), [ADR-0004](0004-pipeline-composition.md), [ADR-0006](0006-database-schema-and-seeding.md), [ADR-0014](0014-web-ui-architecture.md), [ADR-0015](0015-llm-hosting-and-client.md)

## Context

A search system is more than an algorithm. This one needs Postgres with pgvector, a .NET API, a local embedding model, a React UI and an LLM. A learner should be able to start all of it with one command, then read the code without first learning a large framework.

## Decision

### One command: .NET Aspire

`src/PI.AppHost` declares every resource, and `aspire run` starts them in order:

| Resource | What it is |
|---|---|
| `postgres` | The `pgvector/pgvector` image, with a persistent data volume and one database |
| `searchapi` | `PI.SearchApi`: seeds the database, then serves the seven search stages |
| `web-ui` | The Vite dev server for the React UI, which proxies `/api` to the Search API |

The LLM is not a resource: the API talks to an Ollama you already run, or to a hosted provider with your own key ([ADR-0015](0015-llm-hosting-and-client.md)). The API isn't supported as a standalone app; it fails fast without the connection string Aspire provides.

### Repository layout

```text
src/
  PI.AppHost/                    # Aspire: declares Postgres, the API and the UI
  PI.SearchApi/
    Contracts/                   # the one request and response every stage shares (ADR-0003)
    Data/                        # schema, catalog loader, seeder (ADR-0006)
    Embeddings/                  # the ONNX embedding model (ADR-0009)
    Llm/                         # the chat client factory (ADR-0015)
    Pipeline/                    # one folder per technique (ADR-0004)
    Endpoints/                   # thin FastEndpoints handlers
    assets/data/                 # products.json, golden-queries.json, domain-ontology.ttl, init.sql, SPARQL
    assets/prompts/              # LLM prompts as markdown files
  web-ui/                        # React: the talk, the demo, the glossary and these decisions (ADR-0014)
tests/
  PI.SearchApi.Tests/            # fast unit tests, no Docker
  PI.SearchApi.IntegrationTests/ # starts the AppHost and runs the golden queries
tools/
  PI.CatalogGenerator/           # grows the catalog with generated products (ADR-0005)
```

### Code conventions

- **FastEndpoints, one folder per endpoint** (the REPR pattern: request, endpoint, response). Endpoints are thin: validate, call one pipeline service, map the result.
- **Raw Npgsql, with SQL as `const string` next to the service that runs it.** No ORM. The exact SQL is what a learner needs to see, and it is copied into the debug trace.
- **Shared build settings** in `Directory.Build.props` (nullable on, warnings as errors) and **central package versions** in `Directory.Packages.props`.
- **Comments teach search, not C#.** Every pipeline service opens with a short block: what the technique does, its strength, its failure mode and the decision behind it.

### Tests

- **Unit tests** cover pure logic (RRF maths, pooling, rule operators, validators) and check the catalog against the ontology. No Docker; they run in seconds.
- **Integration tests** start the whole AppHost with `Aspire.Hosting.Testing` and run every golden query against every stage ([ADR-0005](0005-curated-dataset-and-golden-queries.md)). LLM stages get structural checks only, never exact wording. Tests skip with a clear message when a model isn't available.

## Consequences

- One command gives a learner the database, the API, the UI and the Aspire dashboard's traces and logs.
- Raw SQL means more mapping code, but every query is visible and matches what the trace shows.
- Warnings as errors add friction while building, and keep the code a learner clones clean.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| Docker Compose | Loses Aspire's dashboard, service discovery and typed resource wiring |
| Minimal APIs instead of FastEndpoints | Viable; endpoint folders map neatly onto the stages |
| EF Core or Dapper | Hides or abstracts the SQL, which is the thing learners should see (full-text and pgvector operators especially) |
| A separate ServiceDefaults project | The Aspire template's default, but unnecessary with one .NET service |
| One project per stage | Too much ceremony; stages share data, contracts and models |

## What to take away

- Orchestration is part of the architecture. Search is a database, models and services working together.
- Keep endpoints thin and services rich. That split is what lets later stages reuse earlier ones.
- Put the SQL where people can read it. In search, the query *is* the logic.
