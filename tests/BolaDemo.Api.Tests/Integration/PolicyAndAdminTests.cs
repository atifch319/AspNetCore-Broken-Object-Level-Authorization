using BolaDemo.Api.Contracts;
using BolaDemo.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BolaDemo.Api.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class PolicyAndAdminTests : SqlIntegrationTest
{
    public PolicyAndAdminTests(SqlServerCollectionFixture fixture)
        : base(fixture)
    {
    }

    [SqlFact]
    public async Task Policy_endpoint_allows_the_owner()
    {
        RequireSql();
        using var client = CreateClient(DemoCatalog.UserAEmail);
        var body = await DemoHttp.GetAsync(client, "/api/secure/orders/2001/policy", HttpStatusCode.OK);
        var order = JsonSerializer.Deserialize<OrderDto>(body, DemoJson.Options);

        Assert.NotNull(order);
        Assert.Equal("ORD-2001", order.OrderNumber);
        OrderAssertions.DoesNotLeakInternalData(body);
    }

    [SqlFact]
    public async Task Policy_endpoint_denies_another_authenticated_user()
    {
        RequireSql();
        Assert.Equal(DemoCatalog.UserBId, await OwnerOfAsync(DemoCatalog.UserBPrimaryOrderId));

        using var userA = CreateClient(DemoCatalog.UserAEmail);
        var denied = await DemoHttp.GetAsync(userA, "/api/secure/orders/2002/policy", HttpStatusCode.NotFound);
        var missing = await DemoHttp.GetAsync(userA, "/api/secure/orders/9999/policy", HttpStatusCode.NotFound);

        Assert.DoesNotContain("ORD-2002", denied, StringComparison.Ordinal);
        OrderAssertions.DoesNotLeakInternalData(denied);
        AssertSameProblem(denied, missing);
    }

    [SqlFact]
    public async Task Admin_can_read_both_users_orders_through_the_policy_endpoint()
    {
        RequireSql();
        using var client = CreateClient(DemoCatalog.AdminEmail);

        var userAOrder = JsonSerializer.Deserialize<OrderDto>(
            await DemoHttp.GetAsync(client, "/api/secure/orders/2001/policy", HttpStatusCode.OK),
            DemoJson.Options);
        var userBOrder = JsonSerializer.Deserialize<OrderDto>(
            await DemoHttp.GetAsync(client, "/api/secure/orders/2002/policy", HttpStatusCode.OK),
            DemoJson.Options);

        Assert.Equal("ORD-2001", userAOrder!.OrderNumber);
        Assert.Equal("ORD-2002", userBOrder!.OrderNumber);
    }

    [SqlFact]
    public async Task Admin_endpoint_allows_admin_and_denies_non_admin()
    {
        RequireSql();

        using (var admin = CreateClient(DemoCatalog.AdminEmail))
        {
            var first = JsonSerializer.Deserialize<OrderDto>(
                await DemoHttp.GetAsync(admin, "/api/admin/orders/2001", HttpStatusCode.OK),
                DemoJson.Options);
            var second = JsonSerializer.Deserialize<OrderDto>(
                await DemoHttp.GetAsync(admin, "/api/admin/orders/2002", HttpStatusCode.OK),
                DemoJson.Options);
            Assert.Equal(2001, first!.Id);
            Assert.Equal(2002, second!.Id);
            await DemoHttp.GetAsync(admin, "/api/admin/orders/9999", HttpStatusCode.NotFound);
        }

        using var customer = CreateClient(DemoCatalog.UserAEmail);
        await DemoHttp.GetAsync(customer, "/api/admin/orders/2001", HttpStatusCode.Forbidden);
        await DemoHttp.GetAsync(customer, "/api/admin/orders/2002", HttpStatusCode.Forbidden);
        await DemoHttp.GetAsync(customer, "/api/admin/orders/9999", HttpStatusCode.Forbidden);
        await DemoHttp.GetAsync(customer, "/api/admin/orders/0", HttpStatusCode.Forbidden);

        using var anonymous = CreateClient();
        await DemoHttp.GetAsync(anonymous, "/api/admin/orders/2002", HttpStatusCode.Unauthorized);
    }

    [SqlFact]
    public async Task Missing_order_returns_404()
    {
        RequireSql();
        using var client = CreateClient(DemoCatalog.UserAEmail);

        await DemoHttp.GetAsync(client, "/api/secure/orders/9999", HttpStatusCode.NotFound);
        await DemoHttp.GetAsync(client, "/api/insecure/orders/9999", HttpStatusCode.NotFound);
        await DemoHttp.GetAsync(client, "/api/secure/orders/9999/policy", HttpStatusCode.NotFound);

        using var admin = CreateClient(DemoCatalog.AdminEmail);
        await DemoHttp.GetAsync(admin, "/api/admin/orders/9999", HttpStatusCode.NotFound);
    }

    private async Task<string> OwnerOfAsync(int orderId)
    {
        await using var db = Fixture.CreateDbContext();
        return await db.Orders.AsNoTracking()
            .Where(order => order.Id == orderId)
            .Select(order => order.CustomerUserId)
            .SingleAsync();
    }

    private static void AssertSameProblem(string left, string right)
    {
        using var leftDocument = JsonDocument.Parse(left);
        using var rightDocument = JsonDocument.Parse(right);
        Assert.Equal(404, leftDocument.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(
            leftDocument.RootElement.GetProperty("title").GetString(),
            rightDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            leftDocument.RootElement.GetProperty("detail").GetString(),
            rightDocument.RootElement.GetProperty("detail").GetString());
    }
}
