namespace PlanCatalog.Core.Validation;

public sealed record ValidationResult(IReadOnlyList<ValidationIssue> Issues)
{
    public bool IsValid => Issues.All(x => x.Severity != ValidationSeverity.Error);

    public static ValidationResult Empty { get; } = new(Array.Empty<ValidationIssue>());

    public static ValidationResult Combine(params ValidationResult[] results) =>
        new(results.SelectMany(r => r.Issues).ToList());

    public static ValidationResult Combine(IEnumerable<ValidationResult> results) =>
        new(results.SelectMany(r => r.Issues).ToList());
}
