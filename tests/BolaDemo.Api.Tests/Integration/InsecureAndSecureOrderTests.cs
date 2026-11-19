using BolaDemo.Api.Contracts;
using BolaDemo.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BolaDemo.Api.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class InsecureAndSecureOrderTests : SqlIntegrationTest
{
    public InsecureAndSecureOrderTests(SqlServerCollectionFixture fixture)
        : base(fixture)
    {
    }

    [SqlFact]
    public async Task Vulnerable_endpoint_lets_User_A_read_User_B_order_2002()
    {
        RequireSql();
        Assert.Equal(DemoCatalog.UserBId, await OwnerOfAsync(DemoCatalog.UserBPrimaryOrderId));

        using var client = CreateClient(DemoCatalog.UserAEmail);
        var body = await DemoHttp.GetAsync(client, "/api/insecure/orders/2002", HttpStatusCode.OK);
        var order = JsonSerializer.Deserialize<OrderDto>(body, DemoJson.Options);

        Assert.NotNull(order);
        Assert.Equal(2002, order.Id);
        Assert.Equal("ORD-2002", order.OrderNumber);
        Assert.Equal("Shipped", order.Status);
        Assert.Equal(120.00m, order.TotalAmount);
        OrderAssertions.DoesNotLeakInternalData(body);
    }

    [SqlFact]
    public async Task Secure_endpoint_blocks_User_A_from_reading_User_B_order_2002()
    {
        RequireSql();
        Assert.Equal(DemoCatalog.UserBId, await OwnerOfAsync(DemoCatalog.UserBPrimaryOrderId));

        using var client = CreateClient(DemoCatalog.UserAEmail);
        var body = await DemoHttp.GetAsync(client, "/api/secure/orders/2002", HttpStatusCode.NotFound);

        OrderAssertions.DoesNotLeakInternalData(body);
        Assert.DoesNotContain("ORD-2002", body, StringComparison.Ordinal);
    }

    [SqlFact]
    public async Task User_A_can_read_own_order_through_the_secure_endpoint()
    {
        RequireSql();

        using var client = CreateClient(DemoCatalog.UserAEmail);
        var body = await DemoHttp.GetAsync(client, "/api/secure/orders/2001", HttpStatusCode.OK);
        var order = JsonSerializer.Deserialize<OrderDto>(body, DemoJson.Options);

        Assert.NotNull(order);
        Assert.Equal(2001, order.Id);
        Assert.Equal("ORD-2001", order.OrderNumber);
        Assert.Equal(DemoCatalog.UserAId, await OwnerOfAsync(2001));
        OrderAssertions.DoesNotLeakInternalData(body);
    }

    [SqlFact]
    public async Task User_B_cannot_read_User_A_order_through_the_secure_endpoint()
    {
        RequireSql();
        Assert.Equal(DemoCatalog.UserAId, await OwnerOfAsync(2001));

        using var client = CreateClient(DemoCatalog.UserBEmail);
        var body = await DemoHttp.GetAsync(client, "/api/secure/orders/2001", HttpStatusCode.NotFound);

        Assert.DoesNotContain("ORD-2001", body, StringComparison.Ordinal);
        OrderAssertions.DoesNotLeakInternalData(body);
    }

    [SqlFact]
    public async Task Route_id_manipulation_does_not_bypass_ownership()
    {
        RequireSql();
        using var client = CreateClient(DemoCatalog.UserAEmail);

        Assert.Equal(HttpStatusCode.OK, await Status(client, "/api/secure/orders/2001"));
        Assert.Equal(HttpStatusCode.OK, await Status(client, "/api/secure/orders/2003"));
        Assert.Equal(HttpStatusCode.OK, await Status(client, "/api/secure/orders/2005"));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, "/api/secure/orders/2002"));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, "/api/secure/orders/2004"));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, "/api/secure/orders/1999"));
    }

    [SqlFact]
    public async Task Unauthenticated_callers_receive_401_on_protected_endpoints()
    {
        RequireSql();
        using var client = CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, "/api/insecure/orders/2002"));
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, "/api/secure/orders/2002"));
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, "/api/secure/orders/2002/policy"));
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, "/api/secure/my-orders"));
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, "/api/admin/orders/2002"));
    }

    private async Task<string> OwnerOfAsync(int orderId)
    {
        await using var db = Fixture.CreateDbContext();
        return await db.Orders.AsNoTracking()
            .Where(order => order.Id == orderId)
            .Select(order => order.CustomerUserId)
            .SingleAsync();
    }

    private static async Task<HttpStatusCode> Status(HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        return response.StatusCode;
    }
}
