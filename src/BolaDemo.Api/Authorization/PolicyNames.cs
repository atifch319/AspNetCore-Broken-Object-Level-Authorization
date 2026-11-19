namespace BolaDemo.Api.Authorization;

public static class PolicyNames
{
    /// <summary>
    /// Resource-based read policy. Invoke it with the loaded <see cref="Entities.Order"/>,
    /// not as a bare endpoint attribute.
    /// </summary>
    public const string CanReadOrder = "CanReadOrder";
}
