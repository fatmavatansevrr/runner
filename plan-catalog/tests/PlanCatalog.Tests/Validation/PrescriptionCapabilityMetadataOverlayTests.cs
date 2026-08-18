using System.Text.Json;
using Json.Schema;
using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Metadata;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Projection;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Schema;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>
/// Phase 10K-FREQ.6D.4C.2 — implements the architecture frozen by FREQ.6D.4C.1: (M4) removes the
/// semantic-axis-mismatched WorkoutDefinition.AllowedPrescriptionModes vs. profile IntensityMode
/// cross-check; (M3) introduces the narrow, additive-only WorkoutDefinitionCapabilityOverlay for
/// GOAL_PACE_TEN_K v2's genuinely-missing AllowedDistanceAccountingModes. No production profile
/// documents are authored here (FREQ.6D.4C.3's scope) — every profile below is a TEST fixture only.
/// </summary>
public sealed class PrescriptionCapabilityMetadataOverlayTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private static string SchemasDirectory() => Path.Combine(RepoRoot(), "schemas");
    private static string CatalogDirectory() => Path.Combine(RepoRoot(), "catalog");
    private static CatalogSourceSnapshot LoadRealSnapshot() => new FileSystemCatalogSourceRepository(CatalogDirectory()).LoadSnapshot();

    // ══════════════════════════════════════════════════════════════════
    // M4 isolation proof — MIXED no longer cross-compared with any typed mode (§21/§31 items 1-4).
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(PrescriptionIntensityMode.PaceBased)]
    [InlineData(PrescriptionIntensityMode.EffortBased)]
    [InlineData(PrescriptionIntensityMode.HeartRateBased)]
    public void RealMixedOnlyWorkout_WithValidTypedIntensity_NoLongerFailsSolelyOnAllowedPrescriptionModes(PrescriptionIntensityMode mode)
    {
        var snapshot = LoadRealSnapshot();
        var workout = snapshot.FindWorkout("THRESHOLD_TEMPO", 4)!; // real, immutable, AllowedPrescriptionModes = [MIXED]
        Assert.Equal([PrescriptionMode.Mixed], workout.AllowedPrescriptionModes);

        var profile = ContinuousProfile(workout, mode, DistanceAccountingMode.EstimatedSessionTotal);
        var result = WorkoutPrescriptionProfileValidator.Validate(profile, workout);

        Assert.DoesNotContain(result.Issues, i => i.Code == "PROFILE_INTENSITY_MODE_NOT_ALLOWED");
        // That error code is retired by this phase's own narrowing - confirm it can never fire again.
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void InvalidTypedIntensity_StillRejected_ViaProfileInternalValidation()
    {
        var snapshot = LoadRealSnapshot();
        var workout = snapshot.FindWorkout("THRESHOLD_TEMPO", 4)!;
        var badComponent = ContinuousComponent(1, WorkoutComponentType.WarmUp, PrescriptionIntensityMode.PaceBased) with
        {
            IntensityTarget = new PrescriptionIntensityTarget { Mode = (PrescriptionIntensityMode)999 }
        };
        var profile = ProfileWith(workout, [badComponent, ContinuousComponent(2, WorkoutComponentType.MainSet, PrescriptionIntensityMode.PaceBased), ContinuousComponent(3, WorkoutComponentType.CoolDown, PrescriptionIntensityMode.PaceBased)], DistanceAccountingMode.EstimatedSessionTotal);

        var result = WorkoutPrescriptionProfileValidator.Validate(profile, workout);

        Assert.Contains(result.Issues, i => i.Code == "PROFILE_INTENSITY_MODE_INVALID");
    }

    [Fact]
    public void MismatchedDescriptor_StillRejected_ViaProfileInternalValidation()
    {
        var snapshot = LoadRealSnapshot();
        var workout = snapshot.FindWorkout("THRESHOLD_TEMPO", 4)!;
        var badComponent = ContinuousComponent(1, WorkoutComponentType.WarmUp, PrescriptionIntensityMode.PaceBased) with
        {
            IntensityTarget = new PrescriptionIntensityTarget { Mode = PrescriptionIntensityMode.PaceBased, EffortDescriptorKey = "WRONG_FIELD_FOR_MODE" }
        };
        var profile = ProfileWith(workout, [badComponent, ContinuousComponent(2, WorkoutComponentType.MainSet, PrescriptionIntensityMode.PaceBased), ContinuousComponent(3, WorkoutComponentType.CoolDown, PrescriptionIntensityMode.PaceBased)], DistanceAccountingMode.EstimatedSessionTotal);

        var result = WorkoutPrescriptionProfileValidator.Validate(profile, workout);

        Assert.Contains(result.Issues, i => i.Code == "PROFILE_INTENSITY_MODE_DESCRIPTOR_MISMATCH");
    }

    [Fact]
    public void ComponentSkeletonMismatch_StillRejected_Unchanged()
    {
        var snapshot = LoadRealSnapshot();
        var workout = snapshot.FindWorkout("THRESHOLD_TEMPO", 4)!; // real skeleton: WARM_UP, MAIN_SET, COOL_DOWN
        var wrongSkeleton = ProfileWith(workout,
            [ContinuousComponent(1, WorkoutComponentType.WarmUp, PrescriptionIntensityMode.PaceBased)], // missing MAIN_SET/COOL_DOWN
            DistanceAccountingMode.EstimatedSessionTotal);

        var result = WorkoutPrescriptionProfileValidator.Validate(wrongSkeleton, workout);

        Assert.Contains(result.Issues, i => i.Code == "PROFILE_COMPONENT_SKELETON_MISMATCH");
    }

    [Fact]
    public void PhaseEligibility_RemainsOutsideThisValidatorsScope_NeverCheckedHereBeforeOrAfter()
    {
        // WorkoutPrescriptionProfileValidator has no phase parameter and never inspected EligiblePhases
        // before this phase, and still does not - phase-fit is expressed entirely by which
        // WorkoutDefinition VERSION a profile pins (e.g. THRESHOLD_TEMPO v5 vs v4), not by a check here.
        // This test documents that boundary rather than inventing new enforcement out of scope.
        var snapshot = LoadRealSnapshot();
        var v4NotFoundationEligible = snapshot.FindWorkout("THRESHOLD_TEMPO", 4)!;
        Assert.DoesNotContain(PhaseKey.Foundation, v4NotFoundationEligible.EligiblePhases);

        var profile = ContinuousProfile(v4NotFoundationEligible, PrescriptionIntensityMode.PaceBased, DistanceAccountingMode.EstimatedSessionTotal);
        var result = WorkoutPrescriptionProfileValidator.Validate(profile, v4NotFoundationEligible);

        Assert.True(result.IsValid); // Structurally/capability valid - phase suitability is a separate, upstream authoring decision (which version to pin), not this validator's concern.
    }

    // ══════════════════════════════════════════════════════════════════
    // M3 isolation proof (§22/§32 items 8-16).
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void GoalPaceTenKV2_NoOverlay_FailsClosed()
    {
        var snapshot = LoadRealSnapshot();
        var v2 = snapshot.FindWorkout("GOAL_PACE_TEN_K", 2)!;
        var profile = ContinuousProfile(v2, PrescriptionIntensityMode.PaceBased, DistanceAccountingMode.EstimatedSessionTotal);

        var result = WorkoutPrescriptionProfileValidator.Validate(profile, v2, capabilityOverlay: null);

        Assert.Contains(result.Issues, i => i.Code == "PROFILE_DISTANCE_ACCOUNTING_MODE_NOT_ALLOWED");
    }

    [Fact]
    public void GoalPaceTenKV2_WithExactApprovedOverlay_Passes()
    {
        var snapshot = LoadRealSnapshot();
        var v2 = snapshot.FindWorkout("GOAL_PACE_TEN_K", 2)!;
        var overlay = snapshot.FindCapabilityOverlay("GOAL_PACE_TEN_K", 2);
        Assert.NotNull(overlay);
        Assert.Equal([DistanceAccountingMode.EstimatedSessionTotal], overlay!.AllowedDistanceAccountingModes);

        var profile = ContinuousProfile(v2, PrescriptionIntensityMode.PaceBased, DistanceAccountingMode.EstimatedSessionTotal);
        var result = WorkoutPrescriptionProfileValidator.Validate(profile, v2, overlay);

        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void WrongVersionOverlay_DoesNotApplyToADifferentExactVersion()
    {
        var snapshot = LoadRealSnapshot();
        var v3 = snapshot.FindWorkout("GOAL_PACE_TEN_K", 3)!;
        var overlayForV2 = snapshot.FindCapabilityOverlay("GOAL_PACE_TEN_K", 2)!;

        // The exact lookup itself must not find a v2 overlay when asked for v3.
        Assert.Null(snapshot.FindCapabilityOverlay("GOAL_PACE_TEN_K", 3));

        // v3 already carries its own explicit metadata now (completed per FREQ.6D.4C.1 sequencing),
        // so it does not need the overlay - but prove the v2 overlay is never silently applied to it
        // by calling the validator with the (wrong) v2 overlay explicitly passed for v3.
        var profile = ContinuousProfile(v3, PrescriptionIntensityMode.PaceBased, DistanceAccountingMode.EstimatedSessionTotal);
        var resultWithWrongOverlay = WorkoutPrescriptionProfileValidator.Validate(profile, v3, overlayForV2);

        // v3 has explicit metadata AND we passed a (mismatched-target) overlay -> conflict, not silent misuse.
        Assert.Contains(resultWithWrongOverlay.Issues, i => i.Code == "PROFILE_CAPABILITY_OVERLAY_CONFLICTS_WITH_EXPLICIT_METADATA");
    }

    [Fact]
    public void WrongKeyOverlay_NeverResolvesForADifferentWorkoutKey()
    {
        var snapshot = LoadRealSnapshot();
        var fartlek = snapshot.FindWorkout("FARTLEK", 4)!;

        Assert.Null(snapshot.FindCapabilityOverlay("FARTLEK", 4)); // no overlay authored for FARTLEK at all

        var profile = RepeatedProfile(fartlek, PrescriptionIntensityMode.EffortBased);
        // FARTLEK v4 already has explicit AllowedDistanceAccountingModes, so this passes without any overlay -
        // proving explicit metadata continues to work standalone (§14/item 14 below covers this directly).
        var result = WorkoutPrescriptionProfileValidator.Validate(profile, fartlek, capabilityOverlay: null);
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void DuplicateOverlayTargetingSameExactWorkout_FailsGraphValidation()
    {
        var snapshot = LoadRealSnapshot();
        var real = snapshot.FindCapabilityOverlay("GOAL_PACE_TEN_K", 2)!;
        var duplicate = real with { Metadata = real.Metadata with { Key = "GOAL_PACE_TEN_K_V2_DUPLICATE_OVERLAY", ContentHash = null } };
        var snapshotWithDuplicate = snapshot with { CapabilityOverlays = [.. snapshot.CapabilityOverlays, duplicate] };

        var result = CatalogGraphValidator.Validate(snapshotWithDuplicate);

        Assert.Contains(result.Issues, i => i.Code == "GRAPH_CAPABILITY_OVERLAY_DUPLICATE_TARGET");
    }

    [Fact]
    public void OrphanOverlay_TargetingMissingWorkout_FailsGraphValidation()
    {
        var snapshot = LoadRealSnapshot();
        var orphan = new WorkoutDefinitionCapabilityOverlay
        {
            Metadata = new CatalogDocumentMetadata { DocumentType = DocumentTypes.WorkoutDefinitionCapabilityOverlay, SchemaVersion = 1, Key = "ORPHAN_OVERLAY_PROBE", Version = 1, Status = CatalogStatus.Draft },
            WorkoutDefinitionRef = new PlanCatalog.Contracts.References.VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = "DOES_NOT_EXIST", Version = 1 },
            AllowedDistanceAccountingModes = [DistanceAccountingMode.EstimatedSessionTotal],
        };
        var snapshotWithOrphan = snapshot with { CapabilityOverlays = [.. snapshot.CapabilityOverlays, orphan] };

        var result = CatalogGraphValidator.Validate(snapshotWithOrphan);

        Assert.Contains(result.Issues, i => i.Code == "GRAPH_CAPABILITY_OVERLAY_TARGET_NOT_FOUND");
    }

    [Fact]
    public void ExplicitMetadata_WorksWithoutOverlay()
    {
        var snapshot = LoadRealSnapshot();
        var fartlek = snapshot.FindWorkout("FARTLEK", 4)!;
        Assert.NotNull(fartlek.AllowedDistanceAccountingModes);

        var profile = RepeatedProfile(fartlek, PrescriptionIntensityMode.EffortBased);
        var result = WorkoutPrescriptionProfileValidator.Validate(profile, fartlek, capabilityOverlay: null);

        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void ExplicitMetadataPlusOverlay_RejectedAsConflict_NoSilentPrecedence()
    {
        var snapshot = LoadRealSnapshot();
        var fartlek = snapshot.FindWorkout("FARTLEK", 4)!; // has explicit AllowedDistanceAccountingModes
        var mismatchedOverlay = new WorkoutDefinitionCapabilityOverlay
        {
            Metadata = new CatalogDocumentMetadata { DocumentType = DocumentTypes.WorkoutDefinitionCapabilityOverlay, SchemaVersion = 1, Key = "FARTLEK_V4_PROBE_OVERLAY", Version = 1, Status = CatalogStatus.Draft },
            WorkoutDefinitionRef = new PlanCatalog.Contracts.References.VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = "FARTLEK", Version = 4 },
            AllowedDistanceAccountingModes = [DistanceAccountingMode.EstimatedSessionTotal],
        };

        // Profile-validation-level detection:
        var profile = RepeatedProfile(fartlek, PrescriptionIntensityMode.EffortBased);
        var profileResult = WorkoutPrescriptionProfileValidator.Validate(profile, fartlek, mismatchedOverlay);
        Assert.Contains(profileResult.Issues, i => i.Code == "PROFILE_CAPABILITY_OVERLAY_CONFLICTS_WITH_EXPLICIT_METADATA");

        // Graph-level detection, independent of any profile:
        var snapshotWithConflict = snapshot with { CapabilityOverlays = [.. snapshot.CapabilityOverlays, mismatchedOverlay] };
        var graphResult = CatalogGraphValidator.Validate(snapshotWithConflict);
        Assert.Contains(graphResult.Issues, i => i.Code == "GRAPH_CAPABILITY_OVERLAY_CONFLICTS_WITH_EXPLICIT_METADATA");
    }

    [Fact]
    public void UnsupportedAccountingMode_StillRejectedEvenWithOverlayPresent()
    {
        var snapshot = LoadRealSnapshot();
        var v2 = snapshot.FindWorkout("GOAL_PACE_TEN_K", 2)!;
        var overlay = snapshot.FindCapabilityOverlay("GOAL_PACE_TEN_K", 2)!;

        var profile = ContinuousProfile(v2, PrescriptionIntensityMode.PaceBased, DistanceAccountingMode.ExactSessionTotal); // overlay only allows Estimated
        var result = WorkoutPrescriptionProfileValidator.Validate(profile, v2, overlay);

        Assert.Contains(result.Issues, i => i.Code == "PROFILE_DISTANCE_ACCOUNTING_MODE_NOT_ALLOWED");
    }

    [Fact]
    public void Overlay_ValidatesAgainstItsRealSchema()
    {
        var validator = new JsonSchemaNetValidator(SchemasDirectory());
        var json = File.ReadAllText(Path.Combine(CatalogDirectory(), "capability-overlays", "goal-pace-ten-k-v2-distance-accounting-capability.v1.json"));

        var result = validator.Validate(DocumentTypes.WorkoutDefinitionCapabilityOverlay, json);

        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void Overlay_SerializationRoundTrips()
    {
        var options = new JsonSerializerOptions(CanonicalJsonOptions.Pretty);
        var json = File.ReadAllText(Path.Combine(CatalogDirectory(), "capability-overlays", "goal-pace-ten-k-v2-distance-accounting-capability.v1.json"));

        var deserialized = JsonSerializer.Deserialize<WorkoutDefinitionCapabilityOverlay>(json, options)!;

        Assert.Equal("GOAL_PACE_TEN_K_V2_DISTANCE_ACCOUNTING_CAPABILITY", deserialized.Metadata.Key);
        Assert.Equal("GOAL_PACE_TEN_K", deserialized.WorkoutDefinitionRef.Key);
        Assert.Equal(2, deserialized.WorkoutDefinitionRef.Version);
        Assert.Equal([DistanceAccountingMode.EstimatedSessionTotal], deserialized.AllowedDistanceAccountingModes);
    }

    [Fact]
    public void Overlay_ContentHashChangesWhenAllowedCapabilityChanges_NeverAffectsWorkoutHash()
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        var snapshot = LoadRealSnapshot();
        var stamped = CatalogStamper.StampAsPublished(serializer, hasher, snapshot);

        var overlay = stamped.CapabilityOverlays.Single(x => x.Metadata.Key == "GOAL_PACE_TEN_K_V2_DISTANCE_ACCOUNTING_CAPABILITY");
        var v2Workout = stamped.Workouts.Single(x => x.Metadata.Key == "GOAL_PACE_TEN_K" && x.Metadata.Version == 2);
        var originalWorkoutHash = v2Workout.Metadata.ContentHash;

        var changedOverlay = overlay with { AllowedDistanceAccountingModes = [DistanceAccountingMode.EmbeddedComponents] };
        var changedHash = CatalogDocumentHasher.ComputeHashExcludingField(serializer, hasher, changedOverlay, "metadata", "contentHash");

        Assert.NotEqual(overlay.Metadata.ContentHash, changedHash);
        Assert.Equal(originalWorkoutHash, v2Workout.Metadata.ContentHash); // unaffected by overlay content
    }

    // ══════════════════════════════════════════════════════════════════
    // Eight-slot representability + projection proof (§20/§33 items 19-26), using the exact FREQ.6D.4B matrix.
    // ══════════════════════════════════════════════════════════════════

    public static IEnumerable<object[]> EightSlots()
    {
        yield return ["FND-P", "AEROBIC_STRENGTH_CONTROLLED_INTRO", 3, PrescriptionIntensityMode.EffortBased, true, 6, 90];
        yield return ["FND-S", "THRESHOLD_TEMPO", 5, PrescriptionIntensityMode.EffortBased, false, 0, 0];
        yield return ["BLD-P", "THRESHOLD_TEMPO", 4, PrescriptionIntensityMode.PaceBased, false, 0, 0];
        yield return ["BLD-S", "FARTLEK", 4, PrescriptionIntensityMode.EffortBased, true, 10, 60];
        yield return ["RS-P", "GOAL_PACE_TEN_K", 2, PrescriptionIntensityMode.PaceBased, false, 0, 0];
        yield return ["RS-S", "THRESHOLD_TEMPO", 4, PrescriptionIntensityMode.PaceBased, false, 0, 0];
        yield return ["TAP-P", "GOAL_PACE_TEN_K", 3, PrescriptionIntensityMode.PaceBased, false, 0, 0];
        yield return ["TAP-S", "FARTLEK", 5, PrescriptionIntensityMode.EffortBased, true, 6, 100];
    }

    [Theory]
    [MemberData(nameof(EightSlots))]
    public void EightSlot_ExactFrozen4BFixture_ValidatesAgainstItsExactWorkoutVersion(
        string slot, string workoutKey, int workoutVersion, PrescriptionIntensityMode mode, bool repeated, int? reps, int? recoverySeconds)
    {
        var snapshot = LoadRealSnapshot();
        var workout = snapshot.FindWorkout(workoutKey, workoutVersion);
        Assert.NotNull(workout);
        var overlay = snapshot.FindCapabilityOverlay(workoutKey, workoutVersion);

        var profile = repeated
            ? RepeatedProfile(workout!, mode, reps!.Value, recoverySeconds!.Value)
            : ContinuousProfile(workout!, mode, DistanceAccountingMode.EstimatedSessionTotal);

        var result = WorkoutPrescriptionProfileValidator.Validate(profile, workout, overlay);

        Assert.True(result.IsValid, $"{slot} ({workoutKey} v{workoutVersion}): {string.Join("; ", result.Issues.Select(i => i.Message))}");
    }

    [Theory]
    [MemberData(nameof(EightSlots))]
    public void EightSlot_ExactFrozen4BFixture_ProjectsLosslessly(
        string slot, string workoutKey, int workoutVersion, PrescriptionIntensityMode mode, bool repeated, int? reps, int? recoverySeconds)
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        var stamped = CatalogStamper.StampAsPublished(serializer, hasher, LoadRealSnapshot());
        var workout = stamped.FindWorkout(workoutKey, workoutVersion)!;

        var profile = repeated
            ? RepeatedProfile(workout, mode, reps!.Value, recoverySeconds!.Value)
            : ContinuousProfile(workout, mode, DistanceAccountingMode.EstimatedSessionTotal);
        var stampedProfile = profile with { Metadata = profile.Metadata with { ContentHash = "test-hash-" + slot } };

        var executable = new WorkoutPrescriptionExecutionProjector().Project(stampedProfile, workout);

        Assert.Empty(PlanCatalog.Contracts.Prescriptions.ExecutableWorkoutPrescriptionValidator.Validate(executable));
        if (repeated)
        {
            var mainSet = executable.Components.Single(c => c.ComponentType == WorkoutComponentType.MainSet);
            Assert.Equal(reps, mainSet.RepetitionCount);
            Assert.Equal(recoverySeconds, mainSet.Recovery!.Value);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Historical immutability + new-DRAFT-version regression (§34/§35).
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void HistoricalDefinitions_RemainUnchanged()
    {
        var snapshot = LoadRealSnapshot();
        Assert.Equal(CatalogStatus.Validated, snapshot.FindWorkout("THRESHOLD_TEMPO", 4)!.Metadata.Status);
        Assert.Equal(CatalogStatus.Validated, snapshot.FindWorkout("FARTLEK", 4)!.Metadata.Status);
        Assert.Equal(CatalogStatus.Validated, snapshot.FindWorkout("GOAL_PACE_TEN_K", 2)!.Metadata.Status);
        Assert.DoesNotContain(PhaseKey.Foundation, snapshot.FindWorkout("THRESHOLD_TEMPO", 4)!.EligiblePhases);
        Assert.DoesNotContain(PhaseKey.Taper, snapshot.FindWorkout("FARTLEK", 4)!.EligiblePhases);
        Assert.Null(snapshot.FindWorkout("GOAL_PACE_TEN_K", 2)!.AllowedDistanceAccountingModes);
    }

    [Fact]
    public void FourNewWorkoutDefinitionVersions_RemainDraft()
    {
        var snapshot = LoadRealSnapshot();
        foreach (var (key, version) in new[] { ("AEROBIC_STRENGTH_CONTROLLED_INTRO", 3), ("THRESHOLD_TEMPO", 5), ("FARTLEK", 5), ("GOAL_PACE_TEN_K", 3) })
        {
            Assert.Equal(CatalogStatus.Draft, snapshot.FindWorkout(key, version)!.Metadata.Status);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════════════

    private static WorkoutPrescriptionProfile ContinuousProfile(WorkoutDefinition workout, PrescriptionIntensityMode mode, DistanceAccountingMode distanceMode) =>
        ProfileWith(workout, workout.Components!.Select(c => ContinuousComponent(c.SequenceOrder, c.ComponentType, mode)).ToList(), distanceMode);

    private static WorkoutPrescriptionProfile RepeatedProfile(WorkoutDefinition workout, PrescriptionIntensityMode mode, int repetitions = 4, int recoverySeconds = 60)
    {
        var components = workout.Components!.Select(c => c.ComponentType == WorkoutComponentType.MainSet
            ? new PrescriptionProfileComponent
            {
                SequenceOrder = c.SequenceOrder,
                ComponentType = c.ComponentType,
                StructureMode = PrescriptionStructureMode.Repeated,
                WorkQuantity = new PrescriptionWorkQuantity { DurationSeconds = 60, RepetitionCount = repetitions },
                RecoveryQuantity = new PrescriptionRecoveryQuantity { DurationSeconds = recoverySeconds, Mode = PrescriptionRecoveryMode.Jog },
                RecoveryPlacement = PrescriptionRecoveryPlacement.BetweenRepetitions,
                IntensityTarget = IntensityTarget(mode),
            }
            : ContinuousComponent(c.SequenceOrder, c.ComponentType, PrescriptionIntensityMode.EffortBased)).ToList();
        return ProfileWith(workout, components, DistanceAccountingMode.EstimatedSessionTotal);
    }

    private static PrescriptionProfileComponent ContinuousComponent(int sequenceOrder, WorkoutComponentType type, PrescriptionIntensityMode mode) => new()
    {
        SequenceOrder = sequenceOrder,
        ComponentType = type,
        StructureMode = PrescriptionStructureMode.Continuous,
        WorkQuantity = new PrescriptionWorkQuantity { DurationSeconds = 300 },
        IntensityTarget = IntensityTarget(mode),
    };

    private static PrescriptionIntensityTarget IntensityTarget(PrescriptionIntensityMode mode) => mode switch
    {
        PrescriptionIntensityMode.PaceBased => new PrescriptionIntensityTarget { Mode = mode, PaceDescriptorKey = "PROBE_PACE" },
        PrescriptionIntensityMode.HeartRateBased => new PrescriptionIntensityTarget { Mode = mode, HeartRateZoneKey = "PROBE_HR" },
        _ => new PrescriptionIntensityTarget { Mode = mode, EffortDescriptorKey = "PROBE_EFFORT" },
    };

    private static WorkoutPrescriptionProfile ProfileWith(WorkoutDefinition workout, IReadOnlyList<PrescriptionProfileComponent> components, DistanceAccountingMode distanceMode) => new()
    {
        Metadata = new CatalogDocumentMetadata
        {
            DocumentType = DocumentTypes.WorkoutPrescriptionProfile,
            SchemaVersion = 1,
            Key = $"{workout.Metadata.Key}_V{workout.Metadata.Version}_TEST_PROBE",
            Version = 1,
            Status = CatalogStatus.Draft,
        },
        WorkoutDefinitionRef = new PlanCatalog.Contracts.References.VersionedCatalogReference
        {
            DocumentType = DocumentTypes.WorkoutDefinition,
            Key = workout.Metadata.Key,
            Version = workout.Metadata.Version,
        },
        DoseCategory = PrescriptionDoseCategory.Primary,
        DistanceAccountingMode = distanceMode,
        Components = components,
    };
}
