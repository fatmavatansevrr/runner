using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;

internal static class TenKPreparationRunwayNumericPolicyFactory
{
    public const string PolicyKey = "TEN_K_PREPARATION_RUNWAY_NUMERIC_POLICY";
    public const int PolicyVersion = 1;
    public const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
    public const int CandidateVersion = 10;

    public static PreparationRunwayNumericPolicy Build()
    {
        var core = VolumeSafetyPolicy.Default;
        return new PreparationRunwayNumericPolicy(
            PolicyKey,
            PolicyVersion,
            V1MissingReadinessStartingVolumePolicy.MissingWeeklyVolumeDefaultKm,
            V1MissingReadinessStartingVolumePolicy.ExplicitZeroWeeklyVolumeDefaultKm,
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
                [nameof(V1MissingReadinessStartingVolumePolicy.MissingWeeklyVolumeDefaultKm)] = PreparationRunwayNumericRuleClassification.ProductDefault,
                [nameof(V1MissingReadinessStartingVolumePolicy.ExplicitZeroWeeklyVolumeDefaultKm)] = PreparationRunwayNumericRuleClassification.ProductDefault,
                ["FourDaySlotDistribution"] = PreparationRunwayNumericRuleClassification.ReusedCoreBehavior,
                ["CoreContinuityTolerance"] = PreparationRunwayNumericRuleClassification.DirectCanonicalRule,
            });
    }
}
