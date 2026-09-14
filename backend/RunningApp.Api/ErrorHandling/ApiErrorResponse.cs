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

    /// <summary>
    /// HM.18 Blocker 1 -- advisory-only field, populated only for
    /// PLAN_HORIZON_START_TOO_EARLY. Never present (serialized) for any other
    /// error code, so it adds zero shape delta to every pre-existing error
    /// response. Never used to auto-mutate a request's StartDate; advisory
    /// metadata for the client only.
    /// </summary>
    public DateOnly? RecommendedStartDate { get; init; }
}
