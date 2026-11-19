using BolaDemo.Api.Entities;

namespace BolaDemo.Api.Authorization;

/// <summary>
/// Central owner-or-admin rule for reading an order.
/// Succeeds for the customer who owns the order, and for an admin via an explicit role check.
/// Every other principal fails closed. Callers must pass the loaded <see cref="Order"/>.
/// </summary>
public sealed class ReadOrderAuthorizationHandler : AuthorizationHandler<ReadOrderRequirement, Order>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ReadOrderRequirement requirement,
        Order resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId)
            && string.Equals(userId, resource.CustomerUserId, StringComparison.Ordinal))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Intentional privileged override. Admin is a role on the principal, never a client-supplied owner id.
        if (context.User.IsInRole(AppRoles.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}
