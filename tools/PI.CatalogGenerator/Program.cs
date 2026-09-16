// Catalog growth: distractors around the curated core (ADR-0005)
//
// What:     Rewrites the generated part of products.json: 240 template-built products
//           (PROD-1001 onwards) after the 60 hand-written ones, so the catalog reaches 300.
// Strength: Rankings stop being trivially small. Chargers, drives and batteries compete with
//           plausible look-alikes, and most results are products nobody asked for, as in a real shop.
// Failure:  Templates repeat themselves. The distractors add volume, not new talk moments; every
//           moment still comes from the curated core.
// Decision: docs/decisions/0005-curated-dataset-and-golden-queries.md
//
// Usage (from the repository root):
//   dotnet run --project tools/PI.CatalogGenerator
//
// The output is deterministic: the same templates and seed always write the same file. After running
// it, re-embed the catalog once (Embeddings:Rebuild = true) and commit products.json and nomic.jsonl.

using PI.CatalogGenerator;

var catalogPath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.GetFullPath(Path.Combine("src", "PI.SearchApi", "assets", "data", "products.json"));

if (!File.Exists(catalogPath))
{
    Console.Error.WriteLine($"No catalog at {catalogPath}. Run from the repository root, or pass the path to products.json.");
    return 1;
}

var catalogText = File.ReadAllText(catalogPath);
var curatedCore = CatalogFile.ReadCuratedCore(catalogText);

var distractors = new DistractorCatalog(seed: DistractorCatalog.DefaultSeed).Generate();
DistractorCatalog.CheckAgainstCuratedCore(distractors, curatedCore);

File.WriteAllText(catalogPath, CatalogFile.Write(curatedCore, distractors));

Console.WriteLine($"Wrote {catalogPath}: {curatedCore.ProductCount} curated + {distractors.Count} generated = {curatedCore.ProductCount + distractors.Count} products.");
Console.WriteLine("Next: run the AppHost once with Embeddings__Rebuild=true, then commit products.json and embeddings/nomic.jsonl.");
return 0;
