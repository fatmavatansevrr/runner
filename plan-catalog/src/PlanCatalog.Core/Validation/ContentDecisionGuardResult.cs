namespace PlanCatalog.Core.Validation;

/// <summary>
/// One field-level blocking domain-content decision, nested inside a <see cref="ContentDecisionGuardError"/>.
/// Sourced 1:1 from a <see cref="Audit.DomainContentDecision"/> entry — never re-derived or summarized.
/// </summary>
public sealed record ProductionReadinessBlockingDecision(
    string EntryId,
    string FieldPath,
    string Classification,
    string Reason);

/// <summary>
/// One Production-readiness content-decision-guard error — see artifacts/audits/production-readiness-error-contract-audit.md
/// for the canonical contract this type implements. Grouped by artifact identity
/// (DocumentType, Key, Version): one error per blocking artifact, carrying every one of that
/// artifact's blocking field-level decisions in <see cref="BlockingDecisions"/> (never just a
/// concatenated message string).
/// </summary>
public sealed record ContentDecisionGuardError(
    string ErrorCode,
    ValidationSeverity Severity,
    string DocumentType,
    string Key,
    int Version,
    IReadOnlyList<ProductionReadinessBlockingDecision> BlockingDecisions);

/// <summary>
/// Aggregate, structured result of <see cref="PublishReadinessValidator.ValidateContentDecisionsDetailed"/>.
/// <see cref="BlockingArtifactCount"/> and <see cref="BlockingDecisionCount"/> are catalog-content
/// properties of the scanned bundle closure — they do not vary with channel or
/// --allow-unconfirmed-content; only <see cref="Errors"/>' codes/severities vary with those inputs.
/// The two counts must never be compared to each other directly (an artifact with 5 blocking fields
/// contributes 5 to the decision count but 1 to the artifact count) — see
/// <see cref="Audit.BlockerScopeMeasurement"/> for the same invariant applied catalog-wide.
/// </summary>
public sealed record ContentDecisionGuardResult(
    int BlockingArtifactCount,
    int BlockingDecisionCount,
    IReadOnlyList<ContentDecisionGuardError> Errors)
{
    public bool IsValid => Errors.All(e => e.Severity != ValidationSeverity.Error);

    /// <summary>Backward-compatible projection for callers that only understand the generic <see cref="ValidationResult"/> shape.</summary>
    public ValidationResult ToValidationResult() => new(Errors.Select(e =>
    {
        var fieldPaths = string.Join(", ", e.BlockingDecisions.Select(d => d.FieldPath));
        return new ValidationIssue(
            e.ErrorCode, e.Severity,
            $"'{e.DocumentType}/{e.Key}' v{e.Version} contains {e.BlockingDecisions.Count} PLACEHOLDER_UNCONFIRMED field-level decision(s) ({fieldPaths}).",
            "$");
    }).ToList());
}
