using System.Text.Json;
using Npgsql;

namespace PI.SearchApi.Data;

// Catalog seeding (ADR-0006)
//
// What:     Applies the idempotent schema, then upserts products.json by content hash: an
//           unchanged product costs nothing, and a changed one loses only its own cached
//           vectors, never every product's.
// Strength: Restarts are instant once seeded; a fresh clone only pays the seeding cost once,
//           from the committed embedding files (added in Phase 2).
// Failure:  Seeding is synchronous and blocks app.Run() until it finishes — simple and
//           predictable for a demo, but a much larger catalog would need a background job
//           instead (see "Alternatives considered" in the decision below).
// Decision: docs/adr/0006-database-schema-and-seeding.md
public sealed class DatabaseSeeder(NpgsqlDataSource dataSource, ILogger<DatabaseSeeder> logger)
{
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
            embedding_dense      = CASE WHEN products.content_hash IS DISTINCT FROM EXCLUDED.content_hash THEN NULL ELSE products.embedding_dense END,
            embedding_model      = CASE WHEN products.content_hash IS DISTINCT FROM EXCLUDED.content_hash THEN NULL ELSE products.embedding_model END,
            embedding_bge_dense  = CASE WHEN products.content_hash IS DISTINCT FROM EXCLUDED.content_hash THEN NULL ELSE products.embedding_bge_dense END,
            embedding_bge_sparse = CASE WHEN products.content_hash IS DISTINCT FROM EXCLUDED.content_hash THEN NULL ELSE products.embedding_bge_sparse END
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

        // TODO(Phase 2): load or backfill embeddings from assets/data/embeddings/{provider}.jsonl
        // (ADR-0006, ADR-0009) once the Nomic embedder exists.
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
