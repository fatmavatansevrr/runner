using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;

internal static class TenKPreparationRunwayNumericPolicyFactory
{
    public const string PolicyKey = "TEN_K_PREPARATION_RUNWAY_NUMERIC_POLICY";
    public const int PolicyVersion = 1;
    public const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
    public const int CandidateVersion = 10;

    public static PreparationRunwayNumericPolicy Build() =>
        Build(VolumeSafetyPolicy.Default, V1MissingReadinessStartingVolumePolicy.MissingWeeklyVolumeDefaultKm,
            V1MissingReadinessStartingVolumePolicy.ExplicitZeroWeeklyVolumeDefaultKm);

    /// <summary>
    /// Phase 10K-FREQ.6D.10 — the Preparation Runway numeric materializer
    /// enforces a single approved PolicyKey/PolicyVersion identity
    /// (<see cref="PreparationRunwayNumericMaterializer"/>'s ValidateRequest),
    /// so this overload keeps that identity fixed and only swaps which
    /// already-approved constant set backs it: the FREQ.6C Intermediate×5D
    /// authority (missing=26.0km, explicit-zero=19.5km, 28%/36% long-run
    /// shares -- <see cref="VolumeSafetyPolicy.FiveDayIntermediate"/>) for an
    /// exact TEN_K/INTERMEDIATE/5D candidate, the untouched 4D defaults for
    /// every other candidate. Mirrors <see cref="CatalogVolumeAndLongRunPlanner"/>'s
    /// own exact-identity-only dispatch -- never a broad "DaysPerWeek >= 5"
    /// condition.
    /// </summary>
    public static PreparationRunwayNumericPolicy Build(PlanCatalogCandidateSummary candidate) =>
        (candidate.CanonicalDistanceFamily, candidate.Level, candidate.DaysPerWeek) switch
        {
            ("TEN_K", "INTERMEDIATE", 5) => Build(VolumeSafetyPolicy.FiveDayIntermediate,
                V1FiveDayIntermediateMissingReadinessStartingVolumePolicy.MissingWeeklyVolumeDefaultKm,
                V1FiveDayIntermediateMissingReadinessStartingVolumePolicy.ExplicitZeroWeeklyVolumeDefaultKm),
            // Phase 10K-FREQ.6D.26 -- FREQ.6D.23/6D.25-approved Intermediate x6D authority, same exact-identity-only dispatch pattern as 5D above.
            ("TEN_K", "INTERMEDIATE", 6) => Build(VolumeSafetyPolicy.SixDayIntermediate,
                V1SixDayIntermediateMissingReadinessStartingVolumePolicy.MissingWeeklyVolumeDefaultKm,
                V1SixDayIntermediateMissingReadinessStartingVolumePolicy.ExplicitZeroWeeklyVolumeDefaultKm),
            // Phase 10K-GEN.9 -- GEN.7/GEN.8-approved Advanced 3D/4D/5D/6D
            // authority, same exact-identity-only dispatch pattern. Advanced
            // never resolves a missing/zero starting-volume default (GEN.8:
            // both are PRODUCT_INELIGIBLE) -- the two default-km parameters
            // here are dead values for Advanced call sites (the Core-level
            // planner fails closed before this factory's Runway-numeric
            // fields are ever read for a missing/zero Advanced request), kept
            // only because this record requires them structurally.
            ("TEN_K", "ADVANCED", 3) => Build(VolumeSafetyPolicy.Advanced3D, 0d, 0d),
            ("TEN_K", "ADVANCED", 4) => Build(VolumeSafetyPolicy.Advanced4D, 0d, 0d),
            ("TEN_K", "ADVANCED", 5) => Build(VolumeSafetyPolicy.Advanced5D, 0d, 0d),
            ("TEN_K", "ADVANCED", 6) => Build(VolumeSafetyPolicy.Advanced6D, 0d, 0d),
            _ => Build(),
        };

    private static PreparationRunwayNumericPolicy Build(VolumeSafetyPolicy core, double missingWeeklyVolumeDefaultKm, double explicitZeroWeeklyVolumeDefaultKm)
    {
        // Phase 10K-FREQ.6D.10: LongRunShareTolerance is a ratio-scale
        // epsilon for the long-run-share validation band (distinct from
        // ContinuityToleranceKm, a km-scale epsilon for exact sum-
        // reconciliation checks -- the two were previously conflated).
        // Default/ThreeDayIntermediate/BeginnerFourDay keep the exact prior
        // numeric value (byte-identical behavior; a governance test asserts
        // one of them still rejects a real violation at this original tight
        // margin). FiveDayIntermediate alone needs a wider tolerance,
        // because FREQ.6C approved exactly two 5D long-run figures (28%
        // selection, 36% hard cap) with no separate preferred-minimum, so
        // its floor sits exactly at the selection share with zero nominal
        // gap -- real weeks can legitimately land up to roughly one
        // rounding increment away from that exact target, since weekly
        // volume and long run are each independently rounded per the
        // approved "round_nearest_0.5km_after_each_week_value_then_validate"
        // rule. Deriving the tolerance from the policy's own already-
        // approved RoundingIncrementKm and GoldenFixtureStartingVolumeKm
        // (never a new invented number) absorbs exactly that drift.
        // Phase 10K-FREQ.6D.26 -- SixDayIntermediate has the identical
        // zero-nominal-gap shape FiveDayIntermediate does (28% selection with
        // no separate preferred-minimum), so it needs the same derived
        // tolerance for the same reason.
        // Phase 10K-GEN.9 -- Advanced5D/6D have the identical zero-nominal-gap
        // shape (LongRunSelectionShare == LongRunPreferredMinimumShare ==
        // 0.28, GEN.7 §27) FiveDayIntermediate/SixDayIntermediate already
        // do, so they need the same derived tolerance for the same reason.
        var longRunShareTolerance = ReferenceEquals(core, VolumeSafetyPolicy.FiveDayIntermediate) || ReferenceEquals(core, VolumeSafetyPolicy.SixDayIntermediate)
            || ReferenceEquals(core, VolumeSafetyPolicy.Advanced5D) || ReferenceEquals(core, VolumeSafetyPolicy.Advanced6D)
            ? core.RoundingIncrementKm / core.GoldenFixtureStartingVolumeKm
            : V1FourDaySessionVolumeAllocationPolicy.ToleranceKm;
        return new PreparationRunwayNumericPolicy(
            PolicyKey,
            PolicyVersion,
            missingWeeklyVolumeDefaultKm,
            explicitZeroWeeklyVolumeDefaultKm,
            core.PreferredMaxWeeklyIncreaseRatio,
            core.HardMaxWeeklyIncreaseRatio,
            core.AbsoluteWeeklyIncrementCapKm,
            core.LongRunPreferredMinimumShare,
            core.LongRunPreferredMaximumShare,
            core.LongRunSelectionShare,
            core.LongRunHardCapShare,
            core.RoundingIncrementKm,
            ContinuityToleranceKm: V1FourDaySessionVolumeAllocationPolicy.ToleranceKm,
            core.RoundingRule,
            new Dictionary<string, PreparationRunwayNumericRuleClassification>
            {
                [nameof(core.PreferredMaxWeeklyIncreaseRatio)] = PreparationRunwayNumericRuleClassification.DirectCanonicalRule,
                [nameof(core.HardMaxWeeklyIncreaseRatio)] = PreparationRunwayNumericRuleClassification.DirectCanonicalRule,
                [nameof(core.AbsoluteWeeklyIncrementCapKm)] = PreparationRunwayNumericRuleClassification.DirectCanonicalRule,
                [nameof(core.LongRunPreferredMinimumShare)] = PreparationRunwayNumericRuleClassification.EvidenceInformedProductDefault,
                [nameof(core.LongRunPreferredMaximumShare)] = PreparationRunwayNumericRuleClassification.EvidenceInformedProductDefault,
                [nameof(core.LongRunSelectionShare)] = PreparationRunwayNumericRuleClassification.EvidenceInformedProductDefault,
                [nameof(core.LongRunHardCapShare)] = PreparationRunwayNumericRuleClassification.EvidenceInformedProductDefault,
                [nameof(core.RoundingIncrementKm)] = PreparationRunwayNumericRuleClassification.ReusedCoreBehavior,
                ["MissingWeeklyVolumeDefaultKm"] = PreparationRunwayNumericRuleClassification.ProductDefault,
                ["ExplicitZeroWeeklyVolumeDefaultKm"] = PreparationRunwayNumericRuleClassification.ProductDefault,
                ["FourDaySlotDistribution"] = PreparationRunwayNumericRuleClassification.ReusedCoreBehavior,
                ["CoreContinuityTolerance"] = PreparationRunwayNumericRuleClassification.DirectCanonicalRule,
            },
            LongRunShareTolerance: longRunShareTolerance);
    }
}
