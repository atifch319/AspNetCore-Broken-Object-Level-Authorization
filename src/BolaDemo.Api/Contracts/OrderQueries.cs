using BolaDemo.Api.Entities;

namespace BolaDemo.Api.Contracts;

public static class OrderQueries
{
    /// <summary>
    /// Projects the public columns only, so internal notes are not selected.
    /// </summary>
    public static IQueryable<OrderDto> SelectDto(this IQueryable<Order> orders) =>
        orders.Select(order => new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt
        });
}
