using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using RunningApp.Api.Logging;
using RunningApp.Application.Exceptions;
// UnauthorizedAppException, NotFoundAppException, ConflictAppException are all in RunningApp.Application.Exceptions

namespace RunningApp.Api.ErrorHandling;

/// <summary>
/// Maps known application exceptions to a standardized JSON error envelope
/// (see <see cref="ApiErrorResponse"/>) instead of letting unhandled
/// exceptions surface as raw 500s with stack traces.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, errorCode) = exception switch
        {
            UnauthorizedAppException => (StatusCodes.Status401Unauthorized,  "UNAUTHORIZED"),
            NotFoundAppException     => (StatusCodes.Status404NotFound,      "NOT_FOUND"),
            ConflictAppException     => (StatusCodes.Status409Conflict,      "CONFLICT"),
            ArgumentException        => (StatusCodes.Status400BadRequest,    "VALIDATION_ERROR"),
            _                        => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR"),
        };

        var correlationId = CorrelationIdAccessor.GetOrCreate(httpContext);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "[{CorrelationId}] Unhandled exception while processing {Path}", correlationId, httpContext.Request.Path);
        }

        var response = new ApiErrorResponse
        {
            ErrorCode = errorCode,
            Message = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message,
            CorrelationId = correlationId,
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, ResponseJsonOptions, cancellationToken);

        return true;
    }
}
