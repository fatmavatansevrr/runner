namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.6A — the bounded GE→Runway transition state. Derived exclusively
/// from the already-validated <see cref="LongHorizonGeWeekNumericResult"/>
/// sequence (Phase 4I.6) -- never recalculates GE progression itself. Carries
/// only what the Runway entry-context adapter needs, not the full GE
/// schedule.
/// </summary>
internal sealed record LongHorizonGeExitState(
    int FinalGeWeekIndex,
    double FinalWeeklyVolumeKm,
    double FinalLongRunKm,
    double PreviousNonCutbackPeakVolumeKm,
    LongHorizonGeStageFamily FinalStageFamily,
    bool FinalWeekWasRecovery,
    bool FinalWeekWasTerminalAlignment,
    ReadinessProfile ReadinessProfile,
    string NumericPolicyId,
    string NumericPolicyVersion)
{
    public static LongHorizonGeExitState From(
        IReadOnlyList<LongHorizonGeWeekDescriptor> descriptors,
        IReadOnlyList<LongHorizonGeWeekNumericResult> numeric,
        ReadinessProfile profile)
    {
        if (descriptors is null || descriptors.Count == 0)
            throw new ArgumentException("descriptors must be a non-empty GE sequence.", nameof(descriptors));
        if (numeric is null || numeric.Count != descriptors.Count)
            throw new ArgumentException("numeric must have exactly one entry per GE descriptor.", nameof(numeric));

        var finalDescriptor = descriptors[^1];
        var finalNumeric = numeric[^1];

        var priorPeak = 0d;
        for (var i = 0; i < numeric.Count; i++)
        {
            if (!descriptors[i].IsRecoveryWeek)
                priorPeak = Math.Max(priorPeak, numeric[i].TotalVolumeKm);
        }

        return new LongHorizonGeExitState(
            FinalGeWeekIndex: finalDescriptor.WeekIndex,
            FinalWeeklyVolumeKm: finalNumeric.TotalVolumeKm,
            FinalLongRunKm: finalNumeric.LongRunDistanceKm,
            PreviousNonCutbackPeakVolumeKm: priorPeak,
            FinalStageFamily: finalDescriptor.StageFamily,
            FinalWeekWasRecovery: finalDescriptor.IsRecoveryWeek,
            FinalWeekWasTerminalAlignment: finalDescriptor.IsTerminalAlignment,
            ReadinessProfile: profile,
            NumericPolicyId: LongHorizonGeNumericExecutor.PolicyId,
            NumericPolicyVersion: LongHorizonGeNumericExecutor.PolicyVersion);
    }
}
