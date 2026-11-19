using BolaDemo.Api.Authorization;
using BolaDemo.Api.Data;
using BolaDemo.Api.Entities;
using Microsoft.AspNetCore.Authorization;

namespace BolaDemo.Api.Tests.Authorization;

public sealed class ReadOrderAuthorizationHandlerTests
{
    private readonly ReadOrderAuthorizationHandler _handler = new();
    private readonly ReadOrderRequirement _requirement = new();

    [Fact]
    public async Task Owner_can_read_the_order()
    {
        var context = Context(userId: DemoCatalog.UserBId, role: AppRoles.Customer, ownerId: DemoCatalog.UserBId);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Admin_can_read_another_users_order()
    {
        var context = Context(userId: DemoCatalog.AdminId, role: AppRoles.Admin, ownerId: DemoCatalog.UserBId);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Another_customer_is_denied()
    {
        var context = Context(userId: DemoCatalog.UserAId, role: AppRoles.Customer, ownerId: DemoCatalog.UserBId);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task CustomerUserId_claim_without_NameIdentifier_does_not_grant_access()
    {
        var identity = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim("CustomerUserId", DemoCatalog.UserBId),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, AppRoles.Customer)
            ],
            authenticationType: "Test");
        var context = new AuthorizationHandlerContext(
            [_requirement],
            new System.Security.Claims.ClaimsPrincipal(identity),
            OrderOwnedBy(DemoCatalog.UserBId));

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Short_role_claim_is_not_treated_as_the_ASP_NET_Core_role()
    {
        var identity = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, DemoCatalog.UserAId),
                new System.Security.Claims.Claim("role", AppRoles.Admin)
            ],
            authenticationType: "Test");
        var context = new AuthorizationHandlerContext(
            [_requirement],
            new System.Security.Claims.ClaimsPrincipal(identity),
            OrderOwnedBy(DemoCatalog.UserBId));

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Unauthenticated_principal_is_denied()
    {
        var identity = new System.Security.Claims.ClaimsIdentity();
        var context = new AuthorizationHandlerContext(
            [_requirement],
            new System.Security.Claims.ClaimsPrincipal(identity),
            OrderOwnedBy(DemoCatalog.UserAId));

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private AuthorizationHandlerContext Context(string userId, string role, string ownerId)
    {
        var identity = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)
            ],
            authenticationType: "Test");

        return new AuthorizationHandlerContext(
            [_requirement],
            new System.Security.Claims.ClaimsPrincipal(identity),
            OrderOwnedBy(ownerId));
    }

    private static Order OrderOwnedBy(string ownerId) => new()
    {
        Id = DemoCatalog.UserBPrimaryOrderId,
        OrderNumber = "ORD-2002",
        CustomerUserId = ownerId,
        Status = OrderStatuses.Shipped,
        TotalAmount = 120.00m,
        CreatedAt = new DateTimeOffset(2026, 2, 2, 12, 0, 0, TimeSpan.Zero),
        InternalNotes = DemoCatalog.InternalNoteMarker
    };
}
