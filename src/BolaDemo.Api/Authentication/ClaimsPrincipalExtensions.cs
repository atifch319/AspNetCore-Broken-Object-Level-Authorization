namespace BolaDemo.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static string? TryGetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
