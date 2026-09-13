using Npgsql;

namespace PI.SearchApi.Data;

// NOTE: Reading and processing scripts from the file system is NOT advised.
// This is done only for a teaching example so the database can be bootstrapped from files.
public sealed class DatabaseManager(string connectionString) : IDatabaseManager
{
    public async Task InitDbAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var sqlPath = Path.Combine(AppContext.BaseDirectory, "assets", "data", "init.sql");
        var sql = await File.ReadAllTextAsync(sqlPath, cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
