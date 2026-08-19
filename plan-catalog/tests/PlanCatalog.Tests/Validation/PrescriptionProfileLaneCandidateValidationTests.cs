using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Metadata;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>
/// Phase 10K-FREQ.6D.4D Split B — publish-time (source-integrity) validation for the new,
/// additive <c>Lanes</c>/<c>PrescriptionProfileCandidates</c> shape on
/// <see cref="WorkoutProgressionValidator"/>. Every lane-related check degenerates to a no-op
/// for the pre-existing single-lane fixture (see <see cref="CombinationFixture"/>'s own
/// unmodified <c>ValidFixture_Passes</c> coverage in <c>WorkoutProgressionValidatorTests</c>).
/// </summary>
public sealed class PrescriptionProfileLaneCandidateValidationTests
{
    private static CatalogDocumentMetadata Metadata(string type, string key, int version = 1) =>
        new() { DocumentType = type, SchemaVersion = 1, Key = key, Version = version, Status = CatalogStatus.Published };

    private static WorkoutPrescriptionProfile Profile(string key, PrescriptionDoseCategory dose, string workoutKey, int workoutVersion) => new()
    {
        Metadata = Metadata(DocumentTypes.WorkoutPrescriptionProfile, key),
        WorkoutDefinitionRef = new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = workoutKey, Version = workoutVersion },
        DoseCategory = dose,
        DistanceAccountingMode = DistanceAccountingMode.EstimatedSessionTotal,
        Components =
        [
            new PrescriptionProfileComponent
            {
                SequenceOrder = 1, ComponentType = WorkoutComponentType.MainSet, StructureMode = PrescriptionStructureMode.Continuous,
                WorkQuantity = new PrescriptionWorkQuantity { DurationSeconds = 1200 },
                IntensityTarget = new PrescriptionIntensityTarget { Mode = PrescriptionIntensityMode.EffortBased, EffortDescriptorKey = "THRESHOLD" }
            }
        ]
    };

    private static VersionedCatalogReference ProfileRef(WorkoutPrescriptionProfile p) =>
        new() { DocumentType = p.Metadata.DocumentType, Key = p.Metadata.Key, Version = p.Metadata.Version };

    private static WorkoutProgressionStageDefinition Stage(
        string key, int order, VersionedCatalogReference workoutRef, string? fallback = null, params VersionedCatalogReference[] profileCandidates) => new()
    {
        StageKey = key, RelativeOrder = order, WorkoutCandidates = [workoutRef],
        MinimumExposures = 1, MaximumExposures = 2,
        CompressionBehavior = StageCompressionBehavior.Compressible, ExtensionBehavior = StageExtensionBehavior.Extendable,
        Requires = [], FallbackStageKey = fallback,
        PrescriptionProfileCandidates = profileCandidates.Length == 0 ? null : profileCandidates,
    };

    private sealed record Fixture(CombinationFixture Base, WorkoutPrescriptionProfile Lane0Profile, WorkoutPrescriptionProfile Lane1Profile, VersionedCatalogReference ThresholdRef);

    private static Fixture BuildFixture()
    {
        var baseFixture = new CombinationFixture();
        var thresholdRef = new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = baseFixture.ThresholdWorkout.Metadata.Key, Version = baseFixture.ThresholdWorkout.Metadata.Version };
        var lane0Profile = Profile("FND_PRIMARY", PrescriptionDoseCategory.Primary, thresholdRef.Key, thresholdRef.Version);
        var lane1Profile = Profile("FND_SECONDARY_CONTROLLED", PrescriptionDoseCategory.SecondaryControlled, thresholdRef.Key, thresholdRef.Version);
        return new Fixture(baseFixture, lane0Profile, lane1Profile, thresholdRef);
    }

    private static (WorkoutProgressionDefinition Progression, CatalogSourceSnapshot Snapshot) BuildLaneProgression(
        Fixture fx, VersionedCatalogReference? lane0Candidate = null, VersionedCatalogReference? lane1Candidate = null, string? lane1Fallback = null, int? duplicateLaneOrdinal = null)
    {
        var lane0Stage = Stage("LANE0_STAGE", 1, fx.ThresholdRef, profileCandidates: lane0Candidate is null ? [] : [lane0Candidate]);
        var lane1Stage = Stage("LANE1_STAGE", 1, fx.ThresholdRef, fallback: lane1Fallback, profileCandidates: lane1Candidate is null ? [] : [lane1Candidate]);

        var lanes = new List<WorkoutProgressionLaneDefinition>
        {
            new() { LaneOrdinal = 0, Stages = [lane0Stage] },
            new() { LaneOrdinal = duplicateLaneOrdinal ?? 1, Stages = [lane1Stage] },
        };

        var progression = fx.Base.WorkoutProgression with
        {
            PhaseProgressions =
            [
                new PhaseWorkoutProgressionDefinition { PhaseKey = PhaseKey.Build, Stages = [], Lanes = lanes }
            ]
        };

        var levelModifier = fx.Base.LevelModifier with { EligibleWorkoutKeys = null, EligibleWorkouts = [fx.ThresholdRef] };

        var snapshot = new CatalogSnapshotBuilder()
            .With(fx.Base.MasterTemplate).With(fx.Base.Layout).With(levelModifier)
            .With(progression).With(fx.Base.ProgressionModifier)
            .With(fx.Base.EasyWorkout).With(fx.Base.LongRunWorkout).With(fx.Base.ThresholdWorkout)
            .With(fx.Base.Registry).With(fx.Base.PeakVolumeBandPolicy).With(fx.Base.RulePack)
            .With(fx.Lane0Profile).With(fx.Lane1Profile)
            .Build();

        return (progression, snapshot);
    }

    [Fact]
    public void LaneAuthoredProgression_MatchingDoseCategoryProfiles_Passes()
    {
        var fx = BuildFixture();
        var (progression, snapshot) = BuildLaneProgression(fx, ProfileRef(fx.Lane0Profile), ProfileRef(fx.Lane1Profile));

        var result = WorkoutProgressionValidator.Validate(progression, snapshot);

        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => $"{i.Code}: {i.Message}")));
    }

    [Fact]
    public void ZeroCandidates_LegacyStage_Passes()
    {
        var fx = BuildFixture();
        var (progression, snapshot) = BuildLaneProgression(fx, lane0Candidate: null, lane1Candidate: null);

        var result = WorkoutProgressionValidator.Validate(progression, snapshot);

        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Code)));
    }

    [Fact]
    public void MoreThanOneCandidate_FailsAmbiguous()
    {
        var fx = BuildFixture();
        var lane1Stage = Stage("LANE1_STAGE", 1, fx.ThresholdRef, profileCandidates: [ProfileRef(fx.Lane1Profile), ProfileRef(fx.Lane1Profile) with { Version = 2 }]);
        var progression = fx.Base.WorkoutProgression with
        {
            PhaseProgressions =
            [
                new PhaseWorkoutProgressionDefinition
                {
                    PhaseKey = PhaseKey.Build, Stages = [],
                    Lanes = [new() { LaneOrdinal = 0, Stages = [Stage("LANE0_STAGE", 1, fx.ThresholdRef)] }, new() { LaneOrdinal = 1, Stages = [lane1Stage] }],
                }
            ]
        };
        var levelModifier = fx.Base.LevelModifier with { EligibleWorkoutKeys = null, EligibleWorkouts = [fx.ThresholdRef] };
        var snapshot = new CatalogSnapshotBuilder()
            .With(fx.Base.MasterTemplate).With(fx.Base.Layout).With(levelModifier)
            .With(progression).With(fx.Base.ProgressionModifier)
            .With(fx.Base.EasyWorkout).With(fx.Base.LongRunWorkout).With(fx.Base.ThresholdWorkout)
            .With(fx.Base.Registry).With(fx.Base.PeakVolumeBandPolicy).With(fx.Base.RulePack)
            .With(fx.Lane1Profile).With(fx.Lane1Profile with { Metadata = fx.Lane1Profile.Metadata with { Version = 2 } })
            .Build();

        var result = WorkoutProgressionValidator.Validate(progression, snapshot);

        Assert.Contains(result.Issues, i => i.Code == "WP_PRESCRIPTION_PROFILE_CANDIDATE_AMBIGUOUS");
    }

    [Fact]
    public void MissingExactProfileVersion_Fails()
    {
        var fx = BuildFixture();
        var (progression, snapshot) = BuildLaneProgression(fx, ProfileRef(fx.Lane0Profile), ProfileRef(fx.Lane1Profile) with { Version = 99 });

        var result = WorkoutProgressionValidator.Validate(progression, snapshot);

        Assert.Contains(result.Issues, i => i.Code == "WP_PRESCRIPTION_PROFILE_CANDIDATE_MISSING");
    }

    [Fact]
    public void WrongDoseCategoryForLane_FailsLaneDoseMismatch()
    {
        var fx = BuildFixture();
        // Lane 1 requires SecondaryControlled but is given the Primary profile.
        var (progression, snapshot) = BuildLaneProgression(fx, ProfileRef(fx.Lane0Profile), ProfileRef(fx.Lane0Profile));

        var result = WorkoutProgressionValidator.Validate(progression, snapshot);

        Assert.Contains(result.Issues, i => i.Code == "PROFILE_LANE_DOSE_CATEGORY_MISMATCH");
    }

    [Fact]
    public void DuplicateLaneOrdinal_Fails()
    {
        var fx = BuildFixture();
        var (progression, snapshot) = BuildLaneProgression(fx, ProfileRef(fx.Lane0Profile), ProfileRef(fx.Lane1Profile), duplicateLaneOrdinal: 0);

        var result = WorkoutProgressionValidator.Validate(progression, snapshot);

        Assert.Contains(result.Issues, i => i.Code == "WP_DUPLICATE_LANE_ORDINAL");
    }

    [Fact]
    public void FallbackAcrossLanes_ScopedPerLane_FailsMissing()
    {
        var fx = BuildFixture();
        // Lane 1's stage falls back to "LANE0_STAGE", which only exists in lane 0 — must fail,
        // proving fallback resolution is scoped per-lane, not per-phase.
        var (progression, snapshot) = BuildLaneProgression(fx, ProfileRef(fx.Lane0Profile), ProfileRef(fx.Lane1Profile), lane1Fallback: "LANE0_STAGE");

        var result = WorkoutProgressionValidator.Validate(progression, snapshot);

        Assert.Contains(result.Issues, i => i.Code == "WP_FALLBACK_STAGE_MISSING");
    }
}
