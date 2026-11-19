using Microsoft.EntityFrameworkCore.Design;

namespace BolaDemo.Api.Data;

/// <summary>
/// Used by <c>dotnet ef</c>. Migrations are generated from the model and do not open this connection.
/// Set <c>BOLA_DEMO_CONNECTION_STRING</c> before <c>dotnet ef database update</c>.
/// The fallback contains a placeholder password and is not a credential.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public const string PlaceholderConnectionString =
        "Server=127.0.0.1,1433;Database=BolaDemo;User Id=sa;Password=Your_local_sa_password;TrustServerCertificate=True;Encrypt=True;Connect Timeout=5";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BOLA_DEMO_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = PlaceholderConnectionString;
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
