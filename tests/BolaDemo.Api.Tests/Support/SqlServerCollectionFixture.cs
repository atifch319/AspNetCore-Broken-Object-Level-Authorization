using BolaDemo.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BolaDemo.Api.Tests.Support;

public sealed class SqlServerCollectionFixture : IAsyncLifetime
{
    public bool IsAvailable { get; private set; }

    public string UnavailableReason { get; private set; } =
        "SQL Server has not been probed. EF Core InMemory is not used.";

    public string? ServerVersion { get; private set; }

    public int? ProductMajorVersion { get; private set; }

    public string? DatabaseName { get; private set; }

    public BolaApiFactory? Factory { get; private set; }

    public int UserCount { get; private set; }

    public int OrderCount { get; private set; }

    private string? _masterConnectionString;

    public async Task InitializeAsync()
    {
        if (!SqlConnectionSettings.TryResolveMasterConnection(out var masterConnectionString, out var reason))
        {
            IsAvailable = false;
            UnavailableReason = reason;
            return;
        }

        _masterConnectionString = masterConnectionString;

        string probe;
        try
        {
            probe = await SqlDatabaseLifetime.ProbeAsync(masterConnectionString);
        }
        catch (Exception exception)
        {
            IsAvailable = false;
            UnavailableReason =
                "SQL Server is not reachable. EF Core InMemory is not used. " + Sanitize(exception.Message);
            return;
        }

        var separator = probe.IndexOf('|');
        if (separator > 0 && int.TryParse(probe[..separator], out var major))
        {
            ProductMajorVersion = major;
            ServerVersion = probe[(separator + 1)..];
        }
        else
        {
            ServerVersion = probe;
        }

        DatabaseName = "BolaDemoTests_" + Guid.NewGuid().ToString("N");
        var databaseConnectionString = SqlConnectionSettings.ForDatabase(masterConnectionString, DatabaseName);

        try
        {
            await SqlDatabaseLifetime.CreateDatabaseAsync(masterConnectionString, DatabaseName);
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(databaseConnectionString)
                .Options;
            await using (var db = new AppDbContext(options))
            {
                await db.Database.MigrateAsync();
                await DemoDataSeeder.SeedAsync(db);
                UserCount = await db.Users.CountAsync();
                OrderCount = await db.Orders.CountAsync();
            }

            Factory = new BolaApiFactory(databaseConnectionString);
            IsAvailable = true;
            UnavailableReason = string.Empty;
        }
        catch (Exception exception)
        {
            await TryDropAsync();
            throw new InvalidOperationException(
                $"Connected to SQL Server but failed to migrate or seed {DatabaseName}. {Sanitize(exception.Message)}",
                exception);
        }
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        await TryDropAsync();
    }

    public void Require()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(UnavailableReason);
        }
    }

    public AppDbContext CreateDbContext()
    {
        if (!IsAvailable || Factory is null || DatabaseName is null || _masterConnectionString is null)
        {
            throw new InvalidOperationException(UnavailableReason);
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(SqlConnectionSettings.ForDatabase(_masterConnectionString, DatabaseName))
            .Options;
        return new AppDbContext(options);
    }

    private async Task TryDropAsync()
    {
        if (_masterConnectionString is null || string.IsNullOrEmpty(DatabaseName))
        {
            return;
        }

        try
        {
            await SqlDatabaseLifetime.DropTestDatabaseAsync(_masterConnectionString, DatabaseName);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Failed to drop {DatabaseName}: {Sanitize(exception.Message)}");
        }
    }

    private static string Sanitize(string message)
    {
        var passwordIndex = message.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
        if (passwordIndex >= 0)
        {
            message = message[..passwordIndex] + "Password=(redacted)";
        }

        return message.Length <= 500 ? message : message[..500];
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerCollectionFixture>
{
    public const string Name = "SqlServer";
}
