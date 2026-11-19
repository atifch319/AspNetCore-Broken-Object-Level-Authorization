using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BolaDemo.Api.Tests.Support;

public sealed class BolaApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public BolaApiFactory(string connectionString)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:BolaDemo", _connectionString);
        builder.UseSetting("DemoAuth:Enabled", "true");
        builder.UseSetting("Database:InitializeOnStartup", "false");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BolaDemo"] = _connectionString,
                ["DemoAuth:Enabled"] = "true",
                ["Database:InitializeOnStartup"] = "false"
            });
        });
    }
}
