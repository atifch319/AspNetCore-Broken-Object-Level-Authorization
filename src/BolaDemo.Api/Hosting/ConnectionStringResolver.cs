using Microsoft.Data.SqlClient;

namespace BolaDemo.Api.Hosting;

public static class ConnectionStringResolver
{
    public const string ConnectionStringName = "BolaDemo";
    public const string LocalDataSource = "127.0.0.1,1433";
    public const string LocalDatabaseName = "BolaDemo";

    public static string Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration.GetConnectionString(ConnectionStringName);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var explicitConnection = configuration["BOLA_DEMO_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(explicitConnection))
        {
            return explicitConnection;
        }

        if (environment.IsDevelopment())
        {
            var password = configuration["MSSQL_SA_PASSWORD"];
            if (!string.IsNullOrWhiteSpace(password))
            {
                return new SqlConnectionStringBuilder
                {
                    DataSource = LocalDataSource,
                    InitialCatalog = LocalDatabaseName,
                    UserID = "sa",
                    Password = password,
                    Encrypt = true,
                    TrustServerCertificate = true,
                    ConnectTimeout = 30
                }.ConnectionString;
            }
        }

        throw new InvalidOperationException(
            "Connection string 'BolaDemo' is not configured. " +
            "Copy .env.example to .env and start the SQL Server 2022 container, " +
            "or set ConnectionStrings__BolaDemo / BOLA_DEMO_CONNECTION_STRING. See README.md.");
    }
}
