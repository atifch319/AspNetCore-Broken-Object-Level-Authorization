using BolaDemo.Api.Authorization;
using BolaDemo.Api.Entities;

namespace BolaDemo.Api.Data;

/// <summary>
/// Deterministic demo identities and orders. Order 2002 belongs to User B.
/// </summary>
public static class DemoCatalog
{
    public const string InternalNoteMarker = "SENSITIVE-INTERNAL-NOTE-DO-NOT-LEAK";

    public const string UserAId = "user-a";
    public const string UserBId = "user-b";
    public const string AdminId = "admin";

    public const string UserAEmail = "user-a@example.com";
    public const string UserBEmail = "user-b@example.com";
    public const string AdminEmail = "admin@example.com";

    public const int UserAPrimaryOrderId = 2001;
    public const int UserBPrimaryOrderId = 2002;

    public static IReadOnlyList<string> AllowedEmails { get; } =
    [
        UserAEmail,
        UserBEmail,
        AdminEmail
    ];

    public static IReadOnlyList<AppUser> CreateUsers() =>
    [
        User(UserAId, UserAEmail, "User A", AppRoles.Customer),
        User(UserBId, UserBEmail, "User B", AppRoles.Customer),
        User(AdminId, AdminEmail, "Demo Admin", AppRoles.Admin)
    ];

    public static IReadOnlyList<Order> CreateOrders() =>
    [
        Order(2001, "ORD-2001", UserAId, OrderStatuses.Placed, 49.90m, On(2026, 1, 15)),
        Order(2002, "ORD-2002", UserBId, OrderStatuses.Shipped, 120.00m, On(2026, 2, 2)),
        Order(2003, "ORD-2003", UserAId, OrderStatuses.Delivered, 15.25m, On(2026, 3, 10)),
        Order(2004, "ORD-2004", UserBId, OrderStatuses.Placed, 88.40m, On(2026, 4, 4)),
        Order(2005, "ORD-2005", UserAId, OrderStatuses.Cancelled, 10.00m, On(2026, 5, 20))
    ];

    private static AppUser User(string id, string email, string displayName, string role) => new()
    {
        Id = id,
        Email = email,
        DisplayName = displayName,
        Role = role
    };

    private static Order Order(
        int id,
        string orderNumber,
        string customerUserId,
        string status,
        decimal totalAmount,
        DateTimeOffset createdAt) => new()
    {
        Id = id,
        OrderNumber = orderNumber,
        CustomerUserId = customerUserId,
        Status = status,
        TotalAmount = totalAmount,
        CreatedAt = createdAt,
        InternalNotes = $"{InternalNoteMarker} for order {id}."
    };

    private static DateTimeOffset On(int year, int month, int day) =>
        new(year, month, day, 12, 0, 0, TimeSpan.Zero);
}
