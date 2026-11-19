namespace BolaDemo.Api.Hosting;

/// <summary>
/// Loads a small allow-list of keys from a repo-root .env file in Development only.
/// Existing process environment values win. The file is never read in Testing or Production.
/// </summary>
public static class LocalEnvFile
{
    private static readonly string[] AllowedKeys =
    [
        "MSSQL_SA_PASSWORD",
        "BOLA_DEMO_CONNECTION_STRING",
        "ConnectionStrings__BolaDemo"
    ];

    public static void LoadInto(WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
        {
            return;
        }

        var path = FindEnvFile(builder.Environment.ContentRootPath);
        if (path is null)
        {
            return;
        }

        foreach (var (key, value) in ReadAllowedEntries(path))
        {
            var configurationKey = key == "ConnectionStrings__BolaDemo"
                ? "ConnectionStrings:BolaDemo"
                : key;

            if (!string.IsNullOrEmpty(builder.Configuration[configurationKey]))
            {
                continue;
            }

            builder.Configuration[configurationKey] = value;
        }
    }

    private static string? FindEnvFile(string start)
    {
        var directory = new DirectoryInfo(start);
        for (var depth = 0; depth < 6 && directory is not null; depth++, directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<(string Key, string Value)> ReadAllowedEntries(string path)
    {
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line["export ".Length..].Trim();
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            if (!AllowedKeys.Contains(key, StringComparer.Ordinal))
            {
                continue;
            }

            var value = line[(separator + 1)..].Trim().Trim('"');
            if (value.Length > 0)
            {
                yield return (key, value);
            }
        }
    }
}
