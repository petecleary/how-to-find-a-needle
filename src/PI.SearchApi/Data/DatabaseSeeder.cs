using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using Pgvector;
using PI.SearchApi.Embeddings;

namespace PI.SearchApi.Data;

// Catalog seeding (ADR-0006)
//
// What:     Applies the idempotent schema, upserts products.json by content hash, then fills
//           in vectors: from the committed embeddings file when the hash and model match,
//           otherwise live from the embedding model.
// Strength: Restarts are instant once seeded; a fresh clone loads committed vectors in
//           seconds, and a changed product loses only its own vectors, never every product's.
// Failure:  Seeding is synchronous and blocks app.Run() until it finishes — simple and
//           predictable for a demo, but a much larger catalog would need a background job
//           instead (see "Alternatives considered" in the decision below).
// Decision: docs/adr/0006-database-schema-and-seeding.md
public sealed class DatabaseSeeder(
    NpgsqlDataSource dataSource,
    ISearchEmbedder embedder,
    IOptions<EmbeddingsOptions> embeddingsOptions,
    IHostEnvironment environment,
    ILogger<DatabaseSeeder> logger)
{
    // One product per inference call (ADR-0009). With the int8 model, padding a short text to the
    // length of a longer batch-mate shifts its vector slightly (cosine ≈ 0.989 instead of 1.0), so
    // batching would make a product's vector depend on which products shared its batch.
    private const int EmbeddingBatchSize = 1;

    // Vectors from different models are never comparable, so switching provider clears the old ones.
    private const string ClearOtherModelsSql = """
        UPDATE products
        SET embedding_dense = NULL, embedding_model = NULL
        WHERE embedding_model IS NOT NULL AND embedding_model <> @model;
        """;

    // The upsert nulls a product's vectors when its content hash changes, so "no vector" means
    // "new, edited, or never embedded".
    private const string ProductsWithoutVectorsSql = """
        SELECT id FROM products WHERE embedding_dense IS NULL;
        """;

    private const string StoreVectorSql = """
        UPDATE products SET embedding_dense = @vector, embedding_model = @model WHERE id = @id;
        """;

    private const string LegacyTableCheckSql = """
        SELECT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name = 'products' AND column_name = 'content_hash'
        );
        """;

    private const string UpsertProductSql = """
        INSERT INTO products (id, name, brand, categories, price, currency, description, reviews, specs, content_hash, updated_at)
        VALUES (@id, @name, @brand, @categories, @price, @currency, @description, @reviews, @specs::jsonb, @contentHash, now())
        ON CONFLICT (id) DO UPDATE SET
            name = EXCLUDED.name,
            brand = EXCLUDED.brand,
            categories = EXCLUDED.categories,
            price = EXCLUDED.price,
            currency = EXCLUDED.currency,
            description = EXCLUDED.description,
            reviews = EXCLUDED.reviews,
            specs = EXCLUDED.specs,
            content_hash = EXCLUDED.content_hash,
            updated_at = now(),
            -- content_hash covers exactly the text that gets embedded (name, brand, categories,
            -- description, reviews — ProductDocument.BuildText). If it changed, the old vectors
            -- no longer describe this row, so null them out; Phase 2's embedding step then
            -- re-embeds only what actually changed. A price or spec edit alone leaves the hash
            -- (and so the vectors) untouched.
            embedding_dense = CASE WHEN products.content_hash IS DISTINCT FROM EXCLUDED.content_hash THEN NULL ELSE products.embedding_dense END,
            embedding_model = CASE WHEN products.content_hash IS DISTINCT FROM EXCLUDED.content_hash THEN NULL ELSE products.embedding_model END
        WHERE products.name         IS DISTINCT FROM EXCLUDED.name
           OR products.brand        IS DISTINCT FROM EXCLUDED.brand
           OR products.categories   IS DISTINCT FROM EXCLUDED.categories
           OR products.price        IS DISTINCT FROM EXCLUDED.price
           OR products.currency     IS DISTINCT FROM EXCLUDED.currency
           OR products.description  IS DISTINCT FROM EXCLUDED.description
           OR products.reviews      IS DISTINCT FROM EXCLUDED.reviews
           OR products.specs        IS DISTINCT FROM EXCLUDED.specs
           OR products.content_hash IS DISTINCT FROM EXCLUDED.content_hash
        -- xmax = 0 means this row was just inserted: Postgres only ever sets xmax on an update
        -- or delete, never on a fresh insert. A row the WHERE clause skipped returns no row at
        -- all, which the caller below reads as "unchanged".
        RETURNING (xmax = 0) AS inserted;
        """;

    private const string DeleteRemovedProductsSql = """
        DELETE FROM products WHERE NOT (id = ANY(@ids));
        """;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "assets", "data");

        await ApplySchemaAsync(dataDirectory, ct);
        await GuardAgainstLegacySchemaAsync(ct);

        var products = CatalogLoader.LoadProducts(dataDirectory);

        var (inserted, updated) = await UpsertProductsAsync(products, ct);
        var deleted = await DeleteRemovedProductsAsync(products, ct);

        logger.LogInformation(
            "Seeded {Count} products ({Inserted} inserted, {Updated} updated, {Deleted} deleted)",
            products.Count, inserted, updated, deleted);

        await SeedEmbeddingsAsync(products, ct);
    }

    private async Task SeedEmbeddingsAsync(IReadOnlyList<CatalogProduct> products, CancellationToken ct)
    {
        var options = embeddingsOptions.Value;
        var model = embedder.ModelId;

        // The committed file is read from and written to the project folder (the content root), not
        // bin/: a rebuild must update the file that gets committed, and models live there too.
        var filePath = Path.Combine(environment.ContentRootPath, "assets", "data", "embeddings", $"{options.Provider}.jsonl");

        await ExecuteAsync(ClearOtherModelsSql, command => command.Parameters.AddWithValue("model", model), ct);

        var productsById = products.ToDictionary(p => p.Id);
        var needed = options.Rebuild
            ? [.. products.Select(p => p.Id)]
            : await ReadProductIdsWithoutVectorsAsync(ct);
        var alreadyCurrent = products.Count - needed.Count;

        // 1. Load from the file: only lines whose model and content hash still match the product.
        var fromFile = new Dictionary<string, float[]>();

        if (!options.Rebuild)
        {
            var neededIds = needed.ToHashSet();

            foreach (var entry in EmbeddingFile.Read(filePath))
            {
                if (neededIds.Contains(entry.Id)
                    && productsById.TryGetValue(entry.Id, out var product)
                    && entry.Model == model
                    && entry.ContentHash == ProductDocument.ContentHash(product))
                {
                    fromFile[entry.Id] = VectorEncoding.FromBase64(entry.Vector);
                }
            }
        }

        // 2. Embed the gaps live: new or edited products, or everything when rebuilding.
        var toEmbed = needed.Where(id => !fromFile.ContainsKey(id)).ToList();
        var embeddedLive = new Dictionary<string, float[]>();

        if (toEmbed.Count > 0 && !options.Rebuild)
        {
            logger.LogWarning(
                "{Count} products have no current vector in {File}, so they will be embedded live. " +
                "Set Embeddings:Rebuild=true once to regenerate the committed file.",
                toEmbed.Count, filePath);
        }

        try
        {
            foreach (var batch in toEmbed.Chunk(EmbeddingBatchSize))
            {
                var texts = batch.Select(id => ProductDocument.BuildText(productsById[id])).ToList();
                var vectors = await embedder.EmbedDocumentsAsync(texts, ct);

                for (var i = 0; i < batch.Length; i++)
                {
                    embeddedLive[batch[i]] = vectors[i];
                }
            }
        }
        catch (EmbeddingModelUnavailableException ex)
        {
            // Expected on a machine without the model: leave the vectors NULL. Stages 1–2 still work,
            // and the stages that need vectors return 503 with the same guidance (ADR-0006).
            logger.LogWarning("Embeddings ({Provider}) could not be generated: {Reason}", options.Provider, ex.Message);
        }

        await StoreVectorsAsync(fromFile.Concat(embeddedLive), model, ct);

        // 3. A rebuild overwrites the committed file — but only if every product was embedded.
        if (options.Rebuild && embeddedLive.Count == products.Count)
        {
            EmbeddingFile.Write(filePath, embeddedLive.Select(kv => new EmbeddingFileEntry(
                kv.Key,
                ProductDocument.ContentHash(productsById[kv.Key]),
                model,
                kv.Value.Length,
                VectorEncoding.ToBase64(kv.Value))));

            logger.LogWarning("Rewrote {File}. Set Embeddings:Rebuild back to false, and commit the file.", filePath);
        }

        var missing = toEmbed.Count - embeddedLive.Count;

        logger.LogInformation(
            "Embeddings ({Provider}): {Loaded} loaded from file, {Live} embedded live, {Missing} missing ({Current} already current)",
            options.Provider, fromFile.Count, embeddedLive.Count, missing, alreadyCurrent);
    }

    private async Task<List<string>> ReadProductIdsWithoutVectorsAsync(CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(ProductsWithoutVectorsSql);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var ids = new List<string>();
        while (await reader.ReadAsync(ct))
        {
            ids.Add(reader.GetString(0));
        }

        return ids;
    }

    private async Task StoreVectorsAsync(IEnumerable<KeyValuePair<string, float[]>> vectors, string model, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        foreach (var (id, vector) in vectors)
        {
            await using var command = new NpgsqlCommand(StoreVectorSql, connection, transaction);
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("model", model);
            command.Parameters.AddWithValue("vector", new Vector(vector));
            await command.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }

    private async Task ExecuteAsync(string sql, Action<NpgsqlCommand> addParameters, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(sql);
        addParameters(command);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task ApplySchemaAsync(string dataDirectory, CancellationToken ct)
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(dataDirectory, "init.sql"), ct);

        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task GuardAgainstLegacySchemaAsync(CancellationToken ct)
    {
        // A development machine may still have the pre-ADR-0006 products table: init.sql only
        // creates the table if it's missing, so an old table is left in place rather than
        // silently short a dozen columns. Fail fast with the fix, instead of a confusing
        // "column content_hash does not exist" error from the first upsert below.
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(LegacyTableCheckSql, connection);
        var hasContentHash = (bool)(await command.ExecuteScalarAsync(ct))!;

        if (!hasContentHash)
        {
            throw new InvalidOperationException(
                "The 'products' table predates content-hash seeding (ADR-0006). Reset the data " +
                "volume: stop the AppHost, run `docker volume rm pgvector-data-search`, then `aspire run` again.");
        }
    }

    private async Task<(int Inserted, int Updated)> UpsertProductsAsync(IReadOnlyList<CatalogProduct> products, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var inserted = 0;
        var updated = 0;

        foreach (var product in products)
        {
            await using var command = new NpgsqlCommand(UpsertProductSql, connection, transaction);
            command.Parameters.AddWithValue("id", product.Id);
            command.Parameters.AddWithValue("name", product.Name);
            command.Parameters.AddWithValue("brand", product.Brand);
            command.Parameters.AddWithValue("categories", product.Categories.ToArray());
            command.Parameters.AddWithValue("price", product.Price);
            command.Parameters.AddWithValue("currency", product.Currency);
            command.Parameters.AddWithValue("description", product.Description);
            command.Parameters.AddWithValue("reviews", product.Reviews.ToArray());
            command.Parameters.AddWithValue("specs", JsonSerializer.Serialize(product.Specs));
            command.Parameters.AddWithValue("contentHash", ProductDocument.ContentHash(product));

            // A row the WHERE clause skipped returns no rows, so ExecuteScalarAsync gives null:
            // "unchanged", counted as neither an insert nor an update.
            switch (await command.ExecuteScalarAsync(ct))
            {
                case true:
                    inserted++;
                    break;
                case false:
                    updated++;
                    break;
            }
        }

        await transaction.CommitAsync(ct);
        return (inserted, updated);
    }

    private async Task<int> DeleteRemovedProductsAsync(IReadOnlyList<CatalogProduct> products, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(DeleteRemovedProductsSql, connection);
        command.Parameters.AddWithValue("ids", products.Select(p => p.Id).ToArray());

        return await command.ExecuteNonQueryAsync(ct);
    }
}
