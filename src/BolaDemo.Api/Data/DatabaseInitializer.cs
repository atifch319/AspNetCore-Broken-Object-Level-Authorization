namespace BolaDemo.Api.Data;

public static class DatabaseInitializer
{
    public static async Task MigrateAndSeedAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        logger.LogInformation("Applying EF Core migrations.");
        await db.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Seeding demo users and orders.");
        await DemoDataSeeder.SeedAsync(db, cancellationToken);

        var users = await db.Users.CountAsync(cancellationToken);
        var orders = await db.Orders.CountAsync(cancellationToken);
        logger.LogInformation(
            "Demo database ready. Users: {UserCount}. Orders: {OrderCount}.",
            users,
            orders);
    }
}
