namespace BolaDemo.Api.Tests.Support;

/// <summary>
/// Runs the test when SQL Server is reachable. Skips it when the server is not configured
/// or cannot be contacted. The skip is decided once, during discovery, without EF Core InMemory.
/// </summary>
public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (!SqlServerAvailability.IsAvailable)
        {
            Skip = SqlServerAvailability.Reason;
        }
    }
}

internal static class SqlServerAvailability
{
    private static readonly Lazy<(bool Available, string Reason)> Probe = new(ProbeOnce);

    public static bool IsAvailable => Probe.Value.Available;

    public static string Reason => Probe.Value.Reason;

    private static (bool Available, string Reason) ProbeOnce()
    {
        if (!SqlConnectionSettings.TryResolveMasterConnection(out var connectionString, out var reason))
        {
            return (false, reason);
        }

        try
        {
            SqlDatabaseLifetime.ProbeAsync(connectionString).GetAwaiter().GetResult();
            return (true, string.Empty);
        }
        catch (Exception exception)
        {
            return (false, "SQL Server is not reachable. EF Core InMemory is not used. " + Sanitize(exception.Message));
        }
    }

    private static string Sanitize(string message)
    {
        var passwordIndex = message.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
        if (passwordIndex >= 0)
        {
            message = message[..passwordIndex] + "Password=(redacted)";
        }

        return message.Length <= 400 ? message : message[..400];
    }
}
