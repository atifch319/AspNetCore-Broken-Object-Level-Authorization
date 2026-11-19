using BolaDemo.Api.Data;

namespace BolaDemo.Api.Authentication;

public static class DemoIdentities
{
    public static bool IsAllowedEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256)
        {
            return false;
        }

        var normalized = email.Trim().ToLowerInvariant();
        return DemoCatalog.AllowedEmails.Contains(normalized, StringComparer.Ordinal);
    }
}
