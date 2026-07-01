using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Thin JsonNode-based helpers for asserting on the wire contract (snake_case
/// JSON keys) directly, instead of depending on either side's C# DTOs.
/// </summary>
public static class HttpJsonExtensions
{
    public static async Task<JsonNode> GetJsonAsync(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var node = await response.Content.ReadFromJsonAsync<JsonNode>();
        return node ?? throw new InvalidOperationException($"Empty JSON body from GET {url}");
    }

    public static async Task<JsonNode> PostJsonAsync(this HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        var node = await response.Content.ReadFromJsonAsync<JsonNode>();
        return node ?? throw new InvalidOperationException($"Empty JSON body from POST {url}");
    }

    public static async Task<HttpResponseMessage> PostRawAsync(this HttpClient client, string url, object? body = null)
    {
        return body is null
            ? await client.PostAsync(url, content: null)
            : await client.PostAsJsonAsync(url, body);
    }
}
