# CLAUDE.md — tests

Repo-wide rules are in the [root CLAUDE.md](../CLAUDE.md). Decisions: [ADR-0002](../docs/adr/0002-solution-structure-and-orchestration.md) (test projects), [ADR-0005](../docs/adr/0005-curated-dataset-and-golden-queries.md) (golden queries).

**Tests are teaching material too.** The golden-query suite is the talk's argument expressed as passing tests: synonym miss → vector hit; keyword trap → hybrid fix; near miss → ontology flag with reason; Spanish query → chargers via ontology labels.

## Projects

| Project | Tooling | Scope |
|---|---|---|
| `PI.SearchApi.Tests` | xUnit, **no Docker**, runs in seconds | Pure logic: RRF maths, pooling and L2 normalisation, base64 vector round-trip, `SqlFilterBuilder`, tsquery builder, label matching, rule operators, validators, citation and heading validators |
| `PI.SearchApi.IntegrationTests` | xUnit + `Aspire.Hosting.Testing` | Starts the AppHost; runs every golden query against the stages built so far |
| `src/web-ui` (Vitest) | Vitest, tests beside the code | Hooks, trace-renderer selection, content integrity. No end-to-end browser tests |

## What to test

- **Unit-test pure logic exhaustively**, especially RRF (ties, missing ranks, weights, k) and each domain rule operator.
- **Catalog validation tests:** unique product IDs; golden-query product IDs exist; categories are taxonomy notations; vocabulary-backed spec values are known; units are numeric; `nomic.jsonl` hashes match `products.json`.
- **Golden queries** assert per-stage expectations from `golden-queries.json` (hit in top N, miss, flagged with reason), not exact scores or full orderings.
- **LLM stages (6–7): structural assertions only.** The answer completes; citations reference evidence product IDs; Stage 7 headings are present; the audience and the pedagogy toggle change the text but not the facts. **Never assert exact wording.**
- Integration tests **skip with a clear message** when ONNX models or the LLM are unavailable; they don't fail with a stack trace.

## Naming

- Test class: `{TypeUnderTest}Tests` (e.g. `ReciprocalRankFusionTests`). Folders mirror the API project (`Pipeline/Fusion/…`).
- Unit test method: `Method_Scenario_ExpectedResult`, e.g. `Fuse_ItemInBothLists_RanksAboveItemInOneList`.
- Golden-query test method: `{GoldenQueryId}_{Stage}_{Expectation}`, e.g. `GQ01_Ontology_FlagsNearMissWithReason`.
- Integration test classes are grouped per stage: `KeywordStageGoldenQueryTests`.

## Style

- Arrange / Act / Assert, separated by blank lines. No `// Arrange` labels needed unless the test is long.
- Prefer small hand-made inputs whose expected output you can check by hand. Put the arithmetic in a comment:

```csharp
// PROD-A: 1/(60+1) + 1/(60+3) = 0.03226; PROD-B: 1/(60+2) = 0.01613 → A first.
```

- A golden-query test opens with one line naming the talk moment it proves:

```csharp
// Talk moment: vector search ranks the 45W barrel charger highly — similarity ≠ compatibility.
```

- Use `[Theory]` with inline data for rule operators and validator limits.
- No sleeps and no reliance on test order. Integration tests share one AppHost fixture.
