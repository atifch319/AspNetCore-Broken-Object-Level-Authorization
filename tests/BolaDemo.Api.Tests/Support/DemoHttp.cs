using System.Text.Json;
using BolaDemo.Api.Authentication;
using BolaDemo.Api.Data;

namespace BolaDemo.Api.Tests.Support;

internal static class DemoJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

internal static class DemoHttp
{
    public static HttpClient Create(BolaApiFactory factory, string? email)
    {
        var client = factory.CreateClient();
        if (!string.IsNullOrWhiteSpace(email))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(DemoAuthenticationDefaults.HeaderName, email);
        }

        return client;
    }

    public static async Task<string> GetAsync(HttpClient client, string url, HttpStatusCode expected)
    {
        using var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == expected,
            $"GET {url} expected {(int)expected} {expected} but received {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
        return body;
    }
}

internal static class OrderAssertions
{
    public static void DoesNotLeakInternalData(string json)
    {
        Assert.DoesNotContain(DemoCatalog.InternalNoteMarker, json, StringComparison.Ordinal);
        Assert.DoesNotContain("internalNotes", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("customerUserId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InternalNotes", json, StringComparison.Ordinal);
    }
}
