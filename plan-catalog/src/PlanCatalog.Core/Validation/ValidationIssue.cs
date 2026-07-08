namespace PlanCatalog.Core.Validation;

public sealed record ValidationIssue(
    string Code,
    ValidationSeverity Severity,
    string Message,
    string? JsonPath = null);
