using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace BolaDemo.Api.Tests.Support;

internal static partial class SqlConnectionSettings
{
    public static bool TryResolveMasterConnection(out string connectionString, out string reason)
    {
        var explicitConnection = Environment.GetEnvironmentVariable("BOLA_DEMO_MASTER_CONNECTION");
        if (!string.IsNullOrWhiteSpace(explicitConnection))
        {
            connectionString = ToMaster(explicitConnection);
            reason = string.Empty;
            return true;
        }

        var password = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            password = TryReadEnvPassword();
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            connectionString = new SqlConnectionStringBuilder
            {
                DataSource = "127.0.0.1,1433",
                InitialCatalog = "master",
                UserID = "sa",
                Password = password,
                Encrypt = true,
                TrustServerCertificate = true,
                ConnectTimeout = 15
            }.ConnectionString;
            reason = string.Empty;
            return true;
        }

        connectionString = string.Empty;
        reason =
            "SQL Server is not configured. Set MSSQL_SA_PASSWORD for the Docker SQL Server on 127.0.0.1,1433, " +
            "or set BOLA_DEMO_MASTER_CONNECTION. EF Core InMemory is not used.";
        return false;
    }

    public static string ForDatabase(string masterConnectionString, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = databaseName,
            ConnectTimeout = 30
        };
        return builder.ConnectionString;
    }

    private static string ToMaster(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
            ConnectTimeout = 15
        };
        return builder.ConnectionString;
    }

    private static string? TryReadEnvPassword()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                var path = Path.Combine(directory.FullName, ".env");
                if (File.Exists(path))
                {
                    return ReadPassword(path);
                }

                directory = directory.Parent;
            }
        }

        return null;
    }

    private static string? ReadPassword(string path)
    {
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var match = EnvPasswordPattern().Match(line);
            if (match.Success)
            {
                return match.Groups["value"].Value.Trim().Trim('"');
            }
        }

        return null;
    }

    [GeneratedRegex(@"^(?:export\s+)?MSSQL_SA_PASSWORD=(?<value>.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex EnvPasswordPattern();
}
