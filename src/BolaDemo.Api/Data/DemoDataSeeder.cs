namespace BolaDemo.Api.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var user in DemoCatalog.CreateUsers())
        {
            var existing = await db.Users
                .SingleOrDefaultAsync(candidate => candidate.Id == user.Id, cancellationToken);

            if (existing is null)
            {
                db.Users.Add(user);
                continue;
            }

            existing.Email = user.Email;
            existing.DisplayName = user.DisplayName;
            existing.Role = user.Role;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var order in DemoCatalog.CreateOrders())
        {
            var existing = await db.Orders
                .SingleOrDefaultAsync(candidate => candidate.Id == order.Id, cancellationToken);

            if (existing is null)
            {
                db.Orders.Add(order);
                continue;
            }

            existing.OrderNumber = order.OrderNumber;
            existing.CustomerUserId = order.CustomerUserId;
            existing.Status = order.Status;
            existing.TotalAmount = order.TotalAmount;
            existing.CreatedAt = order.CreatedAt;
            existing.InternalNotes = order.InternalNotes;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
