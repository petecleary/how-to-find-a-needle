# Tests

The tests are teaching material too: the golden-query suite is the talk's argument written as passing tests. If a test here fails, a moment in the talk no longer happens.

| Project | Needs | What it covers |
|---|---|---|
| [`PI.SearchApi.Tests`](PI.SearchApi.Tests) | Nothing (no Docker), seconds | Pure logic: RRF maths, embedding pooling and normalisation, the tsquery and SQL filter builders, label matching, every domain-rule operator, request validation, citation and heading validators, model parsing and key handling, and catalogue checks (unique IDs, known categories, committed embeddings match the products) |
| [`PI.SearchApi.IntegrationTests`](PI.SearchApi.IntegrationTests) | Docker; the Nomic model for Stages 3–5; an LLM for Stages 6–7 | Starts the whole AppHost and runs every golden query against every stage, plus the API's OpenAPI document and the model endpoints |
| `src/web-ui` (Vitest) | Node | Hooks, parsers, URL state, trace-renderer choice and content integrity |

```sh
dotnet test tests/PI.SearchApi.Tests
dotnet test tests/PI.SearchApi.IntegrationTests
```

## Golden queries

[`golden-queries.json`](../src/PI.SearchApi/assets/data/golden-queries.json) lists the talk's moments. Each one has a request and, per stage, what must be true of the results:

```json
{
  "id": "GQ-03",
  "title": "Similarity is not compatibility",
  "request": { "query": "power adapter for my laptop", "context": { "targetProductId": "PROD-0001" } },
  "expectations": {
    "vector":   [{ "productId": "PROD-0014", "rank": { "max": 5 } }],
    "ontology": [{ "productId": "PROD-0014", "compatibility": "Incompatible" }]
  }
}
```

Read it as: *vector search ranks the 45W barrel charger in the top 5 because it reads like the right one; the ontology marks it Incompatible.* Expectations can also check `present`, `absentFromTop`, `conceptMatch` and `keywordRank`. They check positions and labels, never exact scores, so the tests survive harmless changes and fail on the ones that break the story.

If a golden query stops producing its moment, change the product **wording** in `products.json`, not the algorithm ([ADR-0005](../docs/decisions/0005-curated-dataset-and-golden-queries.md)).

## Skips are deliberate

An integration test **skips with a message**, rather than failing, when something it needs isn't installed: the Nomic model (Stages 3–5) or a running LLM (Stages 6–7). The message says what is missing. A skipped test is a setup step to finish, not a bug.

The LLM stages are checked **structurally**: the answer completes, every citation is a product from the evidence, Stage 7's headings are present. Wording is never asserted, because models phrase things differently on every run.

The integration tests use an empty, temporary `NEEDLE_HOME`, so your own saved model settings can't change their results.

## The model bake-off

An opt-in test that runs every golden query through Stages 6–7 repeatedly, for each model you name, and writes a report of validation results and latency to `PI.SearchApi.IntegrationTests/TestResults/`. It is how the default model was chosen ([ADR-0015](../docs/decisions/0015-llm-hosting-and-client.md)):

```sh
PI_BAKEOFF_MODELS=qwen3.6:35b,gemma4:31b dotnet test tests/PI.SearchApi.IntegrationTests --filter ModelBakeOff
```
