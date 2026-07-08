
namespace PlanCatalog.Core.Validation;

/// <summary>Thrown when a publish workflow step fails validation; carries the full structured result.</summary>
public sealed class CatalogValidationException(string stage, ValidationResult result, ContentDecisionGuardResult? contentDecisionDetail = null)
    : Exception($"{stage} failed with {result.Issues.Count(i => i.Severity == ValidationSeverity.Error)} error(s): " +
                 string.Join(" | ", result.Issues.Where(i => i.Severity == ValidationSeverity.Error).Select(i => $"{i.Code}: {i.Message}")))
{
    public string Stage { get; } = stage;
    public ValidationResult Result { get; } = result;

    /// <summary>
    /// Populated only when <see cref="Stage"/> is "Content decision guard" — the structured,
    /// artifact-grouped-with-nested-decisions form of the same failure carried in <see cref="Result"/>.
    /// See artifacts/audits/production-readiness-error-contract-audit.md.
    /// </summary>
    public ContentDecisionGuardResult? ContentDecisionDetail { get; } = contentDecisionDetail;
}
