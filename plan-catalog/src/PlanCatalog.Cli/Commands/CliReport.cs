namespace PlanCatalog.Cli.Commands;

internal sealed record CliReport
{
    public required bool Success { get; init; }
    public required string Command { get; init; }
    public IReadOnlyList<CliReportIssue> Issues { get; init; } = [];
    public object? Data { get; init; }
}

internal sealed record CliReportIssue(string Code, string Severity, string Message, string? JsonPath);
