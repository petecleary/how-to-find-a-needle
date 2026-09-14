# ADR-0002: Solution structure & orchestration

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0003, ADR-0004, ADR-0006, ADR-0014, ADR-0015; roadmap Phase 1

## Context

The talk runs Postgres + pgvector, a .NET API, local ONNX models, a React UI and a local LLM. Learners must be able to clone the repo and run all of it with one command, and read the code without first learning a large framework.

The current scaffold already uses .NET Aspire (`src/PI.AppHost`) and FastEndpoints (`src/PI.SearchApi`).

## Decision

### Orchestration

**.NET Aspire** is the single entry point. `PI.AppHost` declares every resource:

| Resource | Aspire integration | Introduced in |
|---|---|---|
| `postgres` (`pgvector/pgvector` image) + `pi-teach-db-search` database, persistent data volume | `Aspire.Hosting.PostgreSQL` | Phase 1 (exists) |
| `searchapi` (PI.SearchApi) | project reference | exists |
| `web-ui` (Vite dev server) | `Aspire.Hosting.JavaScript` | Phase 3 ([ADR-0014](0014-web-ui-architecture.md)) |
| LLM provider settings (existing local Ollama, OpenAI or Anthropic; no container) | AppHost parameters passed to the API | Phase 4 ([ADR-0015](0015-llm-hosting-and-client.md)) |

Learners run `aspire run` (or F5 on "Aspire: Launch default AppHost"). The API is not supported as a standalone app, and `Program.cs` fails fast if it is started without its connection string.

### Repository layout (target)

```text
how-to-find-a-needle.slnx
Directory.Build.props            # shared C# settings (nullable, warnings as errors, analyzers)
Directory.Packages.props         # central package versions
.editorconfig
src/
  PI.AppHost/
  PI.SearchApi/
    Program.cs                   # composition root only
    Extensions.cs                # Aspire service defaults (kept in-project; no separate ServiceDefaults project)
    Contracts/                   # shared request/response/trace types (ADR-0003)
    Data/                        # schema init, catalog loader, seeder (ADR-0006)
    Embeddings/                  # ONNX embedders (ADR-0009, ADR-0012)
    Pipeline/                    # one folder per technique service (ADR-0004)
      Structured/ Keyword/ Vector/ Fusion/ BgeM3/ Ontology/ Rag/ Pedagogy/
    Endpoints/
      Search/{Stage}/            # FastEndpoints REPR: Endpoint + Validator (thin)
      Demo/                      # golden queries + device list for the UI
    assets/
      data/                      # products.json, golden-queries.json, domain-ontology.ttl, init.sql
        embeddings/              # nomic.jsonl, openai.jsonl (+ bge-m3.jsonl if built): committed product vectors (ADR-0009)
      models/                    # downloaded ONNX models (gitignored, README committed)
  web-ui/                        # React + Vite: the talk, the demo, glossary and ADR pages (ADR-0014)
    content/                     # speaker.md, talk.json + talk/*.md, stages/*.md, glossary.json
tests/
  PI.SearchApi.Tests/            # fast unit tests, no Docker
  PI.SearchApi.IntegrationTests/ # Aspire.Hosting.Testing, golden queries
```

### Code conventions

- **FastEndpoints with the REPR pattern.** Each endpoint folder contains the endpoint class and its FluentValidation validator. The shared request and response types live in `Contracts/` because every stage uses the same contract ([ADR-0003](0003-search-api-contract-and-debug-trace.md)).
- **Endpoints are thin.** An endpoint validates the request, calls one pipeline service, and maps the result. Search logic lives in `Pipeline/` ([ADR-0004](0004-pipeline-composition.md)).
- **Raw Npgsql with SQL as `const string`, next to the service that uses it.** No EF Core or Dapper. The exact SQL is visible to learners and is copied into the debug trace.
- **`Directory.Build.props`:** `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest`. **`Directory.Packages.props`:** central package management, so versions live in one place.
- **Comments explain *why* and teach.** Every pipeline service opens with a short comment covering the technique, its strength, and its failure mode.
- **Primary constructors and `sealed` records/classes** by default, matching the existing code.
- Detailed standards (naming, commenting, style) are in [CLAUDE.md](../../CLAUDE.md) and its area-specific files.

### Testing

- **`PI.SearchApi.Tests`** (xUnit): pure logic, including RRF maths, tokeniser/pooling helpers, catalog → RDF projection, compatibility rules and validators. No Docker; runs in seconds.
- **`PI.SearchApi.IntegrationTests`** (xUnit + `Aspire.Hosting.Testing`): starts the AppHost, then runs every golden query ([ADR-0005](0005-curated-dataset-and-golden-queries.md)) against stages 1–6 and checks the expected hits and misses. The LLM stages (7–8) get *structural* assertions (the answer completes, citations reference evidence product IDs, Stage 8 headings are present), never exact wording.
- Integration tests skip with a clear message if the ONNX models are not downloaded.

### Continuous integration

If the repo is hosted on GitHub: a GitHub Actions workflow builds the solution and runs the unit tests on every push. It type-checks and lints `web-ui` from Phase 3. Integration tests run on demand, because they need Docker and the model downloads.

## Consequences

- One command starts the whole system; the Aspire dashboard gives learners traces and logs for free.
- Keeping `Extensions.cs` inside the API is a deliberate simplification. With only one .NET service, a ServiceDefaults project would add indirection without adding anything.
- `TreatWarningsAsErrors` keeps the teaching code clean, at the cost of some friction during development.
- Raw SQL means more mapping code, but that code is honest and easy to trace.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Docker Compose | Loses Aspire's dashboard, service discovery and typed resource wiring |
| Minimal APIs instead of FastEndpoints | Viable; FastEndpoints is kept because REPR folders map one-to-one onto the talk's stages |
| EF Core | Hides the SQL, which is the thing we want learners to see (especially pgvector and FTS operators) |
| Separate ServiceDefaults project | Standard Aspire template, but unnecessary with a single .NET service |
| One project per stage | Too much ceremony; stages share data, contracts and models |

## Teaching notes

- Orchestration is part of the architecture: a search system is a database, models and services working together, not just one algorithm.
- Show the "thin endpoint, rich service" split. It is what lets later stages reuse earlier ones.
