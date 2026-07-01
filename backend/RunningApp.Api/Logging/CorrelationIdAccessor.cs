using Microsoft.AspNetCore.Http;

namespace RunningApp.Api.Logging;

/// <summary>
/// Resolves a single correlation id per request — reused from the
/// X-Correlation-Id request header when the caller supplies one (useful for
/// tracing a request across mobile -> API -> logs), generated otherwise.
/// Cached on HttpContext.Items so every middleware/handler that asks for it
/// within the same request gets the same value.
/// </summary>
public static class CorrelationIdAccessor
{
    public const string HeaderName = "X-Correlation-Id";
    private const string ItemKey = "CorrelationId";

    public static string GetOrCreate(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var existing) && existing is string cached)
        {
            return cached;
        }

        var headerValue = context.Request.Headers[HeaderName].ToString();
        var correlationId = string.IsNullOrWhiteSpace(headerValue)
            ? Guid.NewGuid().ToString("n")
            : headerValue;

        context.Items[ItemKey] = correlationId;
        return correlationId;
    }
}
