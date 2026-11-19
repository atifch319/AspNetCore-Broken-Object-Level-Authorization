using BolaDemo.Api.Contracts;
using BolaDemo.Api.Data;
using BolaDemo.Api.Entities;

namespace BolaDemo.Api.Tests.Contracts;

public sealed class OrderDtoTests
{
    [Fact]
    public void Public_shape_excludes_internal_notes_and_owner_id()
    {
        var names = typeof(OrderDto).GetProperties().Select(property => property.Name).OrderBy(name => name).ToArray();

        Assert.Equal(
            ["CreatedAt", "Id", "OrderNumber", "Status", "TotalAmount"],
            names);
    }

    [Fact]
    public void FromEntity_serializes_without_internal_notes()
    {
        var dto = OrderDto.FromEntity(new Order
        {
            Id = DemoCatalog.UserBPrimaryOrderId,
            OrderNumber = "ORD-2002",
            CustomerUserId = DemoCatalog.UserBId,
            Status = OrderStatuses.Shipped,
            TotalAmount = 120.00m,
            CreatedAt = new DateTimeOffset(2026, 2, 2, 12, 0, 0, TimeSpan.Zero),
            InternalNotes = DemoCatalog.InternalNoteMarker + " hidden"
        });

        var json = JsonSerializer.Serialize(dto, DemoJson.Options);
        OrderAssertions.DoesNotLeakInternalData(json);
        Assert.Contains("ORD-2002", json, StringComparison.Ordinal);
    }
}
