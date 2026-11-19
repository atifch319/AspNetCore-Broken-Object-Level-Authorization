namespace BolaDemo.Api.Entities;

public sealed class Order
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string CustomerUserId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Warehouse and support notes. API responses must use <c>OrderDto</c> so this field is never serialized.
    /// </summary>
    public string InternalNotes { get; set; } = string.Empty;

    public AppUser? Customer { get; set; }
}
