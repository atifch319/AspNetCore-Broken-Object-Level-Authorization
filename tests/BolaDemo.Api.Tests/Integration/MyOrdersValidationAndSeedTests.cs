using BolaDemo.Api.Authentication;
using BolaDemo.Api.Contracts;
using BolaDemo.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BolaDemo.Api.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class MyOrdersValidationAndSeedTests : SqlIntegrationTest
{
    public MyOrdersValidationAndSeedTests(SqlServerCollectionFixture fixture)
        : base(fixture)
    {
    }

    [SqlFact]
    public async Task My_orders_returns_only_the_current_users_records()
    {
        RequireSql();
        using var client = CreateClient(DemoCatalog.UserAEmail);
        var body = await DemoHttp.GetAsync(client, "/api/secure/my-orders?page=1&pageSize=2", HttpStatusCode.OK);
        var page = JsonSerializer.Deserialize<PagedOrdersResponse>(body, DemoJson.Options);

        Assert.NotNull(page);
        Assert.Equal(1, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal([2001, 2003], page.Items.Select(item => item.Id).ToArray());
        OrderAssertions.DoesNotLeakInternalData(body);

        var second = JsonSerializer.Deserialize<PagedOrdersResponse>(
            await DemoHttp.GetAsync(client, "/api/secure/my-orders?page=2&pageSize=2", HttpStatusCode.OK),
            DemoJson.Options);
        Assert.NotNull(second);
        Assert.Equal([2005], second.Items.Select(item => item.Id).ToArray());

        using var userB = CreateClient(DemoCatalog.UserBEmail);
        var userBPage = JsonSerializer.Deserialize<PagedOrdersResponse>(
            await DemoHttp.GetAsync(userB, "/api/secure/my-orders", HttpStatusCode.OK),
            DemoJson.Options);
        Assert.NotNull(userBPage);
        Assert.Equal([2002, 2004], userBPage.Items.Select(item => item.Id).ToArray());
    }

    [SqlFact]
    public async Task Client_supplied_owner_ids_cannot_change_caller_scope()
    {
        RequireSql();
        using var client = CreateClient(DemoCatalog.UserAEmail);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Customer-User-Id", DemoCatalog.UserBId);

        var body = await DemoHttp.GetAsync(
            client,
            "/api/secure/my-orders?customerUserId=user-b&ownerId=admin",
            HttpStatusCode.OK);
        var page = JsonSerializer.Deserialize<PagedOrdersResponse>(body, DemoJson.Options);

        Assert.NotNull(page);
        Assert.Equal([2001, 2003, 2005], page.Items.Select(item => item.Id).ToArray());
        Assert.DoesNotContain(2002, page.Items.Select(item => item.Id));

        var denied = await DemoHttp.GetAsync(
            client,
            "/api/secure/orders/2002?customerUserId=user-a",
            HttpStatusCode.NotFound);
        Assert.DoesNotContain("ORD-2002", denied, StringComparison.Ordinal);

        client.DefaultRequestHeaders.Remove(DemoAuthenticationDefaults.HeaderName);
        client.DefaultRequestHeaders.TryAddWithoutValidation(DemoAuthenticationDefaults.HeaderName, DemoCatalog.UserBId);
        await DemoHttp.GetAsync(client, "/api/secure/my-orders", HttpStatusCode.Unauthorized);
    }

    [SqlFact]
    public async Task Dto_output_does_not_contain_internal_notes()
    {
        RequireSql();
        using var userA = CreateClient(DemoCatalog.UserAEmail);
        using var admin = CreateClient(DemoCatalog.AdminEmail);

        string[] bodies =
        [
            await DemoHttp.GetAsync(userA, "/api/insecure/orders/2002", HttpStatusCode.OK),
            await DemoHttp.GetAsync(userA, "/api/secure/orders/2001", HttpStatusCode.OK),
            await DemoHttp.GetAsync(userA, "/api/secure/orders/2001/policy", HttpStatusCode.OK),
            await DemoHttp.GetAsync(userA, "/api/secure/my-orders", HttpStatusCode.OK),
            await DemoHttp.GetAsync(admin, "/api/admin/orders/2002", HttpStatusCode.OK)
        ];

        foreach (var body in bodies)
        {
            OrderAssertions.DoesNotLeakInternalData(body);
        }

        using var document = JsonDocument.Parse(bodies[1]);
        var names = document.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray();
        Assert.Equal(["createdAt", "id", "orderNumber", "status", "totalAmount"], names);
    }

    [SqlFact]
    public async Task Invalid_ids_and_error_responses_are_consistent()
    {
        RequireSql();
        using var client = CreateClient(DemoCatalog.UserAEmail);

        foreach (var route in new[] { "/api/secure/orders", "/api/insecure/orders", "/api/secure/orders" })
        {
            await DemoHttp.GetAsync(client, $"{route}/0", HttpStatusCode.BadRequest);
            await DemoHttp.GetAsync(client, $"{route}/-1", HttpStatusCode.BadRequest);
            await DemoHttp.GetAsync(client, $"{route}/abc", HttpStatusCode.NotFound);
            await DemoHttp.GetAsync(client, $"{route}/9999", HttpStatusCode.NotFound);
        }

        await DemoHttp.GetAsync(client, "/api/secure/orders/0/policy", HttpStatusCode.BadRequest);
        await DemoHttp.GetAsync(client, "/api/secure/orders/abc/policy", HttpStatusCode.NotFound);
        await DemoHttp.GetAsync(client, "/api/secure/my-orders?page=0", HttpStatusCode.BadRequest);
        await DemoHttp.GetAsync(client, "/api/secure/my-orders?pageSize=101", HttpStatusCode.BadRequest);
        await DemoHttp.GetAsync(client, "/api/secure/my-orders?page=abc", HttpStatusCode.BadRequest);

        using var admin = CreateClient(DemoCatalog.AdminEmail);
        await DemoHttp.GetAsync(admin, "/api/admin/orders/0", HttpStatusCode.BadRequest);
        await DemoHttp.GetAsync(admin, "/api/admin/orders/abc", HttpStatusCode.NotFound);
        await DemoHttp.GetAsync(admin, "/api/admin/orders/9999", HttpStatusCode.NotFound);
    }

    [SqlFact]
    public async Task Seeding_twice_does_not_duplicate_rows()
    {
        RequireSql();
        await using var db = Fixture.CreateDbContext();

        await DemoDataSeeder.SeedAsync(db);
        await DemoDataSeeder.SeedAsync(db);

        Assert.Equal(3, await db.Users.CountAsync());
        Assert.Equal(5, await db.Orders.CountAsync());
        Assert.Equal(
            DemoCatalog.UserBId,
            await db.Orders.Where(order => order.Id == 2002).Select(order => order.CustomerUserId).SingleAsync());
    }
}
