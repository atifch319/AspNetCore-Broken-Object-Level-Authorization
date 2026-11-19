using Microsoft.Data.SqlClient;

namespace BolaDemo.Api.Tests.Support;

internal static class SqlDatabaseLifetime
{
    public static async Task<string> ProbeAsync(string masterConnectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CONCAT(
                CONVERT(nvarchar(20), SERVERPROPERTY('ProductMajorVersion')),
                N'|',
                CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion')),
                N' ',
                CONVERT(nvarchar(128), SERVERPROPERTY('Edition')))
            """;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as string ?? string.Empty;
    }

    public static async Task CreateDatabaseAsync(
        string masterConnectionString,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        var quoted = QuoteTestDatabase(databaseName);
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {quoted}";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task DropTestDatabaseAsync(
        string masterConnectionString,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        var quoted = QuoteTestDatabase(databaseName);
        SqlConnection.ClearAllPools();

        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{databaseName.Replace("'", "''", StringComparison.Ordinal)}') IS NOT NULL
            BEGIN
                ALTER DATABASE {quoted} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE {quoted};
            END
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string QuoteTestDatabase(string databaseName)
    {
        if (!databaseName.StartsWith("BolaDemoTests_", StringComparison.Ordinal)
            || databaseName.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
        {
            throw new InvalidOperationException("Refusing to create or drop a database outside the BolaDemoTests_ prefix.");
        }

        return "[" + databaseName + "]";
    }
}
