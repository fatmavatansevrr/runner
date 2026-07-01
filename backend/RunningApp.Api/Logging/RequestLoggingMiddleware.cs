using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using RunningApp.Application.Services;

namespace RunningApp.Api.Logging;

/// <summary>
/// Structured per-request log: correlation id, endpoint, status code,
/// execution time, and the resolved user id. Registered first in the
/// pipeline (before UseExceptionHandler) so the status code it logs is
/// always the final one — including for requests the exception handler
/// converts to an error response.
///
/// Deliberately logs only routing/timing metadata, never request or
/// response bodies, so no personal data ends up in logs.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUserAccessor currentUser)
    {
        var correlationId = CorrelationIdAccessor.GetOrCreate(context);
        context.Response.Headers[CorrelationIdAccessor.HeaderName] = correlationId;

        var stopwatch = Stopwatch.StartNew();
        await _next(context);
        stopwatch.Stop();

        string? userId;
        try { userId = currentUser.UserId; }
        catch { userId = null; }

        _logger.LogInformation(
            "[{CorrelationId}] {Method} {Path} -> {StatusCode} in {ElapsedMs}ms (user={UserId})",
            correlationId,
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            userId ?? "anonymous");
    }
}
