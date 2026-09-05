using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.6 — the input evidence for the first GE week's numeric baseline.
/// Mirrors the same evidence categories <c>CatalogVolumeAndLongRunPlanner</c>'s
/// own starting-volume/long-run-anchor decision already consumes
/// (<c>RecentWeeklyVolumeKm</c>/<c>RecentLongestRunKm</c>) -- this phase
/// reuses those field names and semantics verbatim rather than inventing new
/// evidence categories. <c>RecentRunsPerWeek</c> is accepted for provenance
/// symmetry with the existing baseline-input shape but is not itself
/// consumed by the GE volume/long-run formulas (matching the existing Core
/// planner, which also does not use it in the volume/long-run computation).
/// </summary>
internal sealed record LongHorizonGeEntryBaselineInput(
    double? RecentWeeklyVolumeKm,
    double? RecentLongestRunKm,
    int? RecentRunsPerWeek);

/// <summary>
/// Phase 10K-FREQ.6D.14 — GE entry-readiness product-ineligibility, typed
/// per the established <c>CatalogProductIneligibleException</c> convention
/// (<c>RuntimeCatalog.Prescription.Volume.CatalogVolumeExceptions</c>) so a
/// caller can distinguish "not eligible for this product" from a generic
/// numeric failure, without reusing that Core/Runway-scoped exception type
/// directly for a GE-scoped decision. Replaces GE's own prior bare
/// <see cref="InvalidOperationException"/> throw with the same fail-closed
/// behavior, now distinguishing missing (null) from explicit-zero (&lt;=0)
/// evidence per FREQ.6D.12 SS16/SS17/SS39 -- neither is a new rule, both were
/// already approved as PRODUCT_INELIGIBLE; only the typed shape is new.
/// </summary>
internal abstract class LongHorizonGeProductIneligibleException : InvalidOperationException
{
    public string Code { get; }

    protected LongHorizonGeProductIneligibleException(string code, string message) : base(message)
    {
        Code = code;
    }
}

internal sealed class LongHorizonGeMissingReadinessProductIneligibleException : LongHorizonGeProductIneligibleException
{
    public LongHorizonGeMissingReadinessProductIneligibleException()
        : base(
            "LONG_HORIZON_GE_MISSING_READINESS_PRODUCT_INELIGIBLE",
            "Intermediate LongHorizon General Endurance requires positive observed recent weekly volume evidence; missing readiness is not eligible for this product (FREQ.6D.12).")
    {
    }
}

internal sealed class LongHorizonGeExplicitZeroReadinessProductIneligibleException : LongHorizonGeProductIneligibleException
{
    public LongHorizonGeExplicitZeroReadinessProductIneligibleException()
        : base(
            "LONG_HORIZON_GE_EXPLICIT_ZERO_READINESS_PRODUCT_INELIGIBLE",
            "Intermediate LongHorizon General Endurance requires positive observed recent weekly volume evidence; explicit-zero readiness is not eligible for this product (FREQ.6D.12).")
    {
    }
}

/// <summary>
/// One GE week's executed numeric result, prior to workout binding/calendar
/// assignment. Phase 10K-FREQ.6D.14 -- <see cref="EasySupportDistancesKm"/>
/// replaces the fixed First/Second pair with an ordered list sized to match
/// the source descriptor's own <c>EasySupportWorkouts.Count</c>; the two
/// named accessors below are back-compat only, matching the identical
/// precedent already established by <c>FourDaySessionDistanceAllocation</c>.
/// </summary>
internal sealed record LongHorizonGeWeekNumericResult(
    int WeekIndex,
    double TotalVolumeKm,
    double LongRunDistanceKm,
    double KeySessionDistanceKm,
    IReadOnlyList<double> EasySupportDistancesKm)
{
    /// <summary>Back-compat accessor for every pre-FREQ.6D.14 (2-EASY) consumer -- unchanged value when <see cref="EasySupportDistancesKm"/> has exactly 2 entries.</summary>
    public double FirstEasySupportDistanceKm => EasySupportDistancesKm[0];

    /// <summary>Back-compat accessor -- unchanged value when <see cref="EasySupportDistancesKm"/> has exactly 2 entries.</summary>
    public double SecondEasySupportDistanceKm => EasySupportDistancesKm[1];

    /// <summary>Explicit structural equality for the ordered-list member -- see the identical note on <see cref="Prescription.Session.FourDaySessionDistanceAllocation"/>.</summary>
    public bool Equals(LongHorizonGeWeekNumericResult? other) =>
        other is not null &&
        WeekIndex == other.WeekIndex &&
        TotalVolumeKm.Equals(other.TotalVolumeKm) &&
        LongRunDistanceKm.Equals(other.LongRunDistanceKm) &&
        KeySessionDistanceKm.Equals(other.KeySessionDistanceKm) &&
        EasySupportDistancesKm.SequenceEqual(other.EasySupportDistancesKm);

    public override int GetHashCode() =>
        HashCode.Combine(WeekIndex, TotalVolumeKm, LongRunDistanceKm, KeySessionDistanceKm,
            EasySupportDistancesKm.Aggregate(17, HashCode.Combine));
}

/// <summary>
/// Phase 4I.6 — executes the Phase 4I.2 (development progression) and Phase
/// 4I.2A (recovery) numeric policies against an ordered GE structural week
/// sequence (from <see cref="LongHorizonGeStructuralSelector"/>). Reuses
/// <see cref="VolumeSafetyPolicy.Default"/> and <see cref="FourDaySessionDistanceAllocationPolicy"/>
/// verbatim -- introduces zero new numeric constants. Deterministic, pure,
/// no I/O.
/// </summary>
internal static class LongHorizonGeNumericExecutor
{
    public const string PolicyId = "TD-LONG-HORIZON-GE-SAFETY-001";
    public const string RecoveryPolicyId = "TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001";
    public const string PolicyVersion = VolumeSafetyPolicy.PolicyVersion;

    /// <summary>Phase 4I.2A: fixed recovery-week total-volume reduction ratio (0.85 = 15% reduction of the previous non-cutback development peak). Not sourced from <see cref="VolumeSafetyPolicy"/> because it is a GE-specific policy (TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001), not a generic volume-safety constant.</summary>
    public const double RecoveryVolumeRatio = 0.85d;

    /// <summary>Phase 4I.2A: minimum observable total-volume reduction after rounding.</summary>
    public const double MinimumRecoveryReductionKm = 0.5d;

    /// <param name="weeks">The ordered GE descriptor sequence from <see cref="LongHorizonGeStructuralSelector"/>.</param>
    /// <param name="baseline">Entry evidence. Missing (null) and explicit-zero (&lt;=0) are distinguished and both fail closed as typed <see cref="LongHorizonGeProductIneligibleException"/> -- reused verbatim, not merely re-thrown generically (Phase 10K-FREQ.6D.14, FREQ.6D.12 SS16/SS17/SS39).</param>
    /// <param name="policy">
    /// Phase 10K-FREQ.6D.14 -- the exact <see cref="VolumeSafetyPolicy"/>
    /// instance whose growth ratios and long-run shares govern this GE run.
    /// Defaults to <see cref="VolumeSafetyPolicy.Default"/>, reproducing
    /// every pre-FREQ.6D.14 (4D) caller byte-for-byte, including the
    /// pre-existing absence of any weekly-volume target cap.
    /// </param>
    /// <param name="applyTargetCap">
    /// Phase 10K-FREQ.6D.14 -- when true, clamps each non-recovery week's
    /// total volume to <c>policy.ResolvedPeakReference.Value</c> (the
    /// already-approved FREQ.6C/FREQ.6D.10 44.5km reference for
    /// <see cref="VolumeSafetyPolicy.FiveDayIntermediate"/> -- no new
    /// numeric constant), producing a deterministic plateau once the cap is
    /// reached rather than continued unbounded growth. Defaults to false so
    /// every pre-FREQ.6D.14 (4D) caller is byte-for-byte unchanged; must be
    /// explicitly opted into by a 5D-aware caller.
    /// </param>
    public static IReadOnlyList<LongHorizonGeWeekNumericResult> Execute(
        IReadOnlyList<LongHorizonGeWeekDescriptor> weeks, LongHorizonGeEntryBaselineInput baseline,
        VolumeSafetyPolicy? policy = null, bool applyTargetCap = false)
    {
        if (weeks is null || weeks.Count == 0)
            throw new ArgumentException("weeks must be a non-empty ordered GE descriptor sequence.", nameof(weeks));
        if (baseline.RecentWeeklyVolumeKm is null)
            throw new LongHorizonGeMissingReadinessProductIneligibleException();
        if (baseline.RecentWeeklyVolumeKm.Value <= 0)
            throw new LongHorizonGeExplicitZeroReadinessProductIneligibleException();

        var effectivePolicy = policy ?? VolumeSafetyPolicy.Default;
        var results = new List<LongHorizonGeWeekNumericResult>(weeks.Count);
        double? previousWeekVolume = null;
        double? previousLongRun = null;
        var priorPeakVolume = baseline.RecentWeeklyVolumeKm.Value;
        var previousWasRecovery = false;

        foreach (var week in weeks)
        {
            double totalVolume;
            if (previousWeekVolume is null)
            {
                // First GE week: the entry baseline itself, unprogressed.
                totalVolume = Round(baseline.RecentWeeklyVolumeKm.Value, effectivePolicy);
            }
            else if (week.IsRecoveryWeek)
            {
                totalVolume = Round(priorPeakVolume * RecoveryVolumeRatio, effectivePolicy);
                var reduction = Round(priorPeakVolume - totalVolume, effectivePolicy);
                if (reduction < MinimumRecoveryReductionKm)
                {
                    totalVolume = Round(priorPeakVolume - MinimumRecoveryReductionKm, effectivePolicy);
                }
                if (totalVolume >= priorPeakVolume)
                {
                    throw new InvalidOperationException(
                        $"GE week {week.WeekIndex}: recovery volume ({totalVolume}km) did not reduce below the prior peak ({priorPeakVolume}km).");
                }
            }
            else if (previousWasRecovery)
            {
                // Phase 4I.2A postCutbackProgressionBaseline=PREVIOUS_NON_CUTBACK_VOLUME:
                // progress from the prior development PEAK, never from the recovery week's reduced value.
                totalVolume = ApplyDevelopmentProgressionCap(priorPeakVolume, effectivePolicy);
            }
            else
            {
                totalVolume = ApplyDevelopmentProgressionCap(previousWeekVolume.Value, effectivePolicy);
            }

            if (applyTargetCap && !week.IsRecoveryWeek && totalVolume > effectivePolicy.ResolvedPeakReference.Value)
                totalVolume = Round(effectivePolicy.ResolvedPeakReference.Value, effectivePolicy);

            var longRun = week.IsRecoveryWeek
                ? ResolveRecoveryLongRun(totalVolume, previousLongRun!.Value, effectivePolicy)
                : ResolveDevelopmentLongRun(totalVolume, previousWeekVolume is null ? baseline.RecentLongestRunKm : null, effectivePolicy);

            // Phase 10K-GEN.32 (GEN.31 §1/§3.4 item 2) -- generalized from the
            // fixed keySessionCount:1 (byte-identical for every pre-GEN.32
            // 4D/5D/6D week, all of which have week.HasKeySession==true via
            // the descriptor's own default) to week.HasKeySession's own
            // 0/1 value, admitting 2D's Pattern-B week (HasKeySession=false)
            // with zero new allocation logic -- FourDaySessionDistanceAllocationPolicy.Allocate
            // already supports keySessionCount==0 (Phase 10K-GEN.20).
            var distribution = FourDaySessionDistanceAllocationPolicy.Allocate(
                totalVolume, longRun,
                keySessionCount: week.HasKeySession ? 1 : 0,
                easySupportCount: week.EasySupportWorkouts.Count);

            var keySessionDistanceKm = distribution.KeySessionDistancesKm.Count > 0
                ? distribution.KeySessionDistanceKm
                : 0d;

            results.Add(new LongHorizonGeWeekNumericResult(
                week.WeekIndex, totalVolume, longRun,
                keySessionDistanceKm, distribution.EasySupportDistancesKm));

            if (!week.IsRecoveryWeek)
                priorPeakVolume = Math.Max(priorPeakVolume, totalVolume);

            previousWeekVolume = totalVolume;
            previousLongRun = longRun;
            previousWasRecovery = week.IsRecoveryWeek;
        }

        return results;
    }

    /// <summary>Phase 4I.2 (generalized Phase 10K-FREQ.6D.14 to accept any <see cref="VolumeSafetyPolicy"/>, byte-identical for <see cref="VolumeSafetyPolicy.Default"/>): preferred ratio increase, absolute cap (whichever is smaller), never exceeding the hard ceiling. Rounded via the policy's own rounding convention.</summary>
    private static double ApplyDevelopmentProgressionCap(double reference, VolumeSafetyPolicy policy)
    {
        var preferredTarget = reference * (1 + policy.PreferredMaxWeeklyIncreaseRatio);
        var absoluteCapped = reference + policy.AbsoluteWeeklyIncrementCapKm;
        var candidate = Round(Math.Min(preferredTarget, absoluteCapped), policy);

        var hardCap = Round(reference * (1 + policy.HardMaxWeeklyIncreaseRatio), policy);
        if (candidate > hardCap)
            candidate = hardCap;

        return candidate;
    }

    /// <summary>Development-week long run: SelectionShare target, clamped to [PreferredMinimumShare, min(PreferredMaximumShare, HardCapShare)] -- the exact rule <c>CatalogVolumeAndLongRunPlanner.BuildLongRunPlan</c> already uses (reused, not reimplemented independently). The very first GE week additionally reconciles against RecentLongestRunKm when present, matching that same existing rule.</summary>
    private static double ResolveDevelopmentLongRun(double weeklyVolumeKm, double? recentLongestRunKm, VolumeSafetyPolicy policy)
    {
        var target = Round(weeklyVolumeKm * policy.LongRunSelectionShare, policy);
        var lower = Round(weeklyVolumeKm * policy.LongRunPreferredMinimumShare, policy);
        var upper = Round(weeklyVolumeKm * Math.Min(policy.LongRunPreferredMaximumShare, policy.LongRunHardCapShare), policy);

        var unclamped = target;
        if (recentLongestRunKm is > 0)
            unclamped = Math.Min(recentLongestRunKm.Value, target);

        return Round(Math.Clamp(unclamped, lower, upper), policy);
    }

    /// <summary>Phase 4I.2A: RecoveryWeekLongRunKm = Round(RecoveryWeekTotalVolumeKm x LongRunSelectionShare) -- reusing the existing share unmodified.</summary>
    private static double ResolveRecoveryLongRun(double recoveryTotalVolumeKm, double previousLongRunKm, VolumeSafetyPolicy policy)
    {
        var longRun = Round(recoveryTotalVolumeKm * policy.LongRunSelectionShare, policy);
        if (longRun >= previousLongRunKm)
        {
            throw new InvalidOperationException(
                $"Recovery long run ({longRun}km) did not reduce below the previous development long run ({previousLongRunKm}km).");
        }
        return longRun;
    }

    private static double Round(double value, VolumeSafetyPolicy policy) =>
        Math.Round(value / policy.RoundingIncrementKm, MidpointRounding.AwayFromZero) * policy.RoundingIncrementKm;
}
