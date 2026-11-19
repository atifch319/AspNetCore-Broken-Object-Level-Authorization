using BolaDemo.Api.Entities;

namespace BolaDemo.Api.Contracts;

/// <summary>
/// Public order shape. Internal notes and the owner id are intentionally absent.
/// A class keeps the EF Core projection on a parameterless constructor.
/// </summary>
public sealed class OrderDto
{
    public required int Id { get; init; }

    public required string OrderNumber { get; init; }

    public required string Status { get; init; }

    public required decimal TotalAmount { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public static OrderDto FromEntity(Order order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        Status = order.Status,
        TotalAmount = order.TotalAmount,
        CreatedAt = order.CreatedAt
    };
}
