using BolaDemo.Api.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BolaDemo.Api.Tests.Hosting;

public sealed class ApiHostTests : IClassFixture<HealthApiFactory>
{
    private readonly HealthApiFactory _factory;

    public ApiHostTests(HealthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_does_not_require_authentication()
    {
        using var client = _factory.CreateClient();
        var body = await DemoHttp.GetAsync(client, "/api/health", HttpStatusCode.OK);
        Assert.Contains("ok", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Missing_demo_header_is_unauthorized_before_a_database_read()
    {
        using var client = _factory.CreateClient();

        await DemoHttp.GetAsync(client, "/api/insecure/orders/2002", HttpStatusCode.Unauthorized);
        await DemoHttp.GetAsync(client, "/api/secure/orders/2002", HttpStatusCode.Unauthorized);
        await DemoHttp.GetAsync(client, "/api/secure/orders/2002/policy", HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("user-b")]
    [InlineData("admin")]
    [InlineData("user-c@example.com")]
    [InlineData("user-a")]
    public async Task Arbitrary_user_ids_and_unknown_emails_are_rejected(string header)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation(DemoAuthenticationDefaults.HeaderName, header);

        await DemoHttp.GetAsync(client, "/api/secure/orders/2001", HttpStatusCode.Unauthorized);
    }
}

public sealed class HealthApiFactory : WebApplicationFactory<Program>
{
    public HealthApiFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:BolaDemo",
            "Server=127.0.0.1,1;Database=BolaDemo_Unused;User Id=sa;Password=unused;TrustServerCertificate=True;Encrypt=False;Connect Timeout=1");
        builder.UseSetting("DemoAuth:Enabled", "true");
        builder.UseSetting("Database:InitializeOnStartup", "false");
    }
}
