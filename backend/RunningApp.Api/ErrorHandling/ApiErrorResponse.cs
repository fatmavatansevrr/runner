namespace RunningApp.Api.ErrorHandling;

/// <summary>
/// Standardized error envelope returned by every 4xx/5xx response.
/// Intentionally a fixed, separate shape from the (snake_case) success
/// DTOs — clients should be able to detect and handle errors generically
/// without knowing about every endpoint's success contract.
/// </summary>
public sealed class ApiErrorResponse
{
    public required string ErrorCode { get; init; }
    public required string Message { get; init; }
    public required string CorrelationId { get; init; }
}
