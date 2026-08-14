namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.3 — fail-fast invariant checks for an already-constructed
/// <see cref="LongHorizonCompositionDecision"/>. Not database validation;
/// exists to catch an internally inconsistent decision (a defect in
/// <see cref="LongHorizonCompositionResolver"/> or a hand-constructed test
/// fixture) before any future consumer trusts the decision's fields.
/// </summary>
internal static class LongHorizonCompositionValidator
{
    public static LongHorizonCompositionValidationResult Validate(LongHorizonCompositionDecision decision)
    {
        var findings = new List<string>();

        if (decision.Eligibility == LongHorizonEligibility.SupportedLongHorizon)
        {
            if (decision.AvailableFullWeeks is < 21 or > 52)
                findings.Add($"SupportedLongHorizon decision has AvailableFullWeeks={decision.AvailableFullWeeks}, expected 21..52.");
            if (decision.GeneralEnduranceWeeks is null or < 1 or > 32)
                findings.Add($"SupportedLongHorizon decision has GeneralEnduranceWeeks={decision.GeneralEnduranceWeeks}, expected 1..32.");
            if (decision.PreparationRunwayWeeks != 8)
                findings.Add($"SupportedLongHorizon decision has PreparationRunwayWeeks={decision.PreparationRunwayWeeks}, expected exactly 8.");
            if (decision.CoreWeeks != 12)
                findings.Add($"SupportedLongHorizon decision has CoreWeeks={decision.CoreWeeks}, expected exactly 12.");
            if (decision.GeneralEnduranceWeeks is int ge && decision.PreparationRunwayWeeks is int rw && decision.CoreWeeks is int cw
                && ge + rw + cw != decision.AvailableFullWeeks)
                findings.Add($"SupportedLongHorizon sum invariant violated: {ge}+{rw}+{cw} != {decision.AvailableFullWeeks}.");

            var expectedClassification = decision.GeneralEnduranceWeeks is >= 1 and <= 3
                ? GeneralEnduranceDurationClassification.ShortExtension
                : GeneralEnduranceDurationClassification.FullPhase;
            if (decision.GeneralEnduranceClassification != expectedClassification)
                findings.Add($"GeneralEnduranceClassification={decision.GeneralEnduranceClassification} does not match GE={decision.GeneralEnduranceWeeks} (expected {expectedClassification}).");

            if (decision.ReadinessProfile is null)
                findings.Add("SupportedLongHorizon decision is missing a ReadinessProfile.");
            if (string.IsNullOrWhiteSpace(decision.PolicyId) || string.IsNullOrWhiteSpace(decision.PolicyVersion))
                findings.Add("SupportedLongHorizon decision is missing policy provenance (PolicyId/PolicyVersion).");
        }
        else
        {
            // GeneralEnduranceWeeks is exclusively a Long-Horizon (21-52)
            // concept -- never legitimate for any other path.
            if (decision.GeneralEnduranceWeeks is not null)
                findings.Add("Non-SupportedLongHorizon decision emits GeneralEnduranceWeeks as if eligible.");

            // Truly unsupported paths (below-minimum/invalid, and 53+) must
            // carry no allocation at all. CoreOnly and PreparationRunwayAndCore
            // legitimately carry their own non-null Core/Runway values --
            // that is a different horizon path's valid allocation, not a
            // misleading Long-Horizon claim.
            var isTrulyUnsupportedPath = decision.HorizonPath is
                LongHorizonPath.InsufficientOrExistingUnsupportedPath or
                LongHorizonPath.UnsupportedAboveSupportedWindow;
            if (isTrulyUnsupportedPath &&
                (decision.PreparationRunwayWeeks is not null || decision.CoreWeeks is not null))
                findings.Add("Unsupported decision emits allocation values as if eligible.");

            if (decision.Eligibility == LongHorizonEligibility.UnsupportedAboveAnnualWindow)
            {
                if (decision.ReasonCode != LongHorizonCompositionResolver.ReasonPlanHorizonExceedsSupportedWindow)
                    findings.Add($"UnsupportedAboveAnnualWindow decision has ReasonCode={decision.ReasonCode}, expected {LongHorizonCompositionResolver.ReasonPlanHorizonExceedsSupportedWindow}.");
            }
        }

        return findings.Count == 0
            ? LongHorizonCompositionValidationResult.Valid()
            : LongHorizonCompositionValidationResult.Invalid(findings);
    }
}

internal sealed record LongHorizonCompositionValidationResult(bool IsValid, IReadOnlyList<string> Findings)
{
    public static LongHorizonCompositionValidationResult Valid() => new(true, []);
    public static LongHorizonCompositionValidationResult Invalid(IEnumerable<string> findings) => new(false, findings.ToList());
}
