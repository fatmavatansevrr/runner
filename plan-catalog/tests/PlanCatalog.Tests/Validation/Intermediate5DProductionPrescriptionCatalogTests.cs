using Json.Schema;
using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Metadata;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Schema;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>
/// Phase 10K-FREQ.6D.4C — catalog tests for the four approved WorkoutDefinition
/// versioned eligibility extensions (all real, authored, DRAFT this phase),
/// and the real, exact catalog-representability contradiction discovered while
/// attempting to author the eight approved production WorkoutPrescriptionProfile
/// documents. No profile documents were authored into the real catalog this
/// phase - see FREQ6D4C's report for the full analysis. Every assertion below
/// runs against the REAL repository catalog (not synthetic fixtures) except
/// where a test explicitly isolates one field via `with` to prove the
/// contradiction is precisely scoped to that field and nothing else.
///
/// UPDATED IN FREQ.6D.4C.2: both root causes documented below were resolved by
/// that phase (M4: removed the invalid PROFILE_INTENSITY_MODE_NOT_ALLOWED
/// cross-check; M3: additive-only WorkoutDefinitionCapabilityOverlay for the
/// genuine missing-metadata gap, plus direct completion of GOAL_PACE_TEN_K v3
/// since it was still DRAFT/non-historical). The tests in sections 4-6 below
/// are updated to assert the fixed, current behavior instead of the
/// now-resolved blocker. See PrescriptionCapabilityMetadataOverlayTests.cs for
/// the full new-architecture test matrix (M4/M3 isolation, 8-slot
/// representability + lossless-projection proof, regression coverage).
/// </summary>
public sealed class Intermediate5DProductionPrescriptionCatalogTests
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

    private static readonly (string Key, int Version)[] NewWorkoutVersions =
    [
        ("AEROBIC_STRENGTH_CONTROLLED_INTRO", 3),
        ("THRESHOLD_TEMPO", 5),
        ("FARTLEK", 5),
        ("GOAL_PACE_TEN_K", 3),
    ];

    private static PlanCatalog.Core.Catalog.CatalogSourceSnapshot LoadRealSnapshot() =>
        new FileSystemCatalogSourceRepository(CatalogDirectory()).LoadSnapshot();

    // ══════════════════════════════════════════════════════════════════
    // 1. Discovery: exactly the 4 new WorkoutDefinition versions exist.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void FourNewWorkoutDefinitionVersions_AreDiscoveredByTheRealLoader()
    {
        var snapshot = LoadRealSnapshot();

        foreach (var (key, version) in NewWorkoutVersions)
        {
            Assert.NotNull(snapshot.FindWorkout(key, version));
        }
    }

    [Fact]
    public void RealCatalog_HasNoPrescriptionProfilesAuthoredYet()
    {
        // Confirms the honest state of this phase's real output: zero profile
        // documents were authored, because all eight would fail real domain
        // validation - see the two contradiction-proof tests below.
        var snapshot = LoadRealSnapshot();
        Assert.Empty(snapshot.PrescriptionProfiles);
    }

    // ══════════════════════════════════════════════════════════════════
    // 2. Schema validation of the 4 new WorkoutDefinition versions + full non-regression.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void NewWorkoutDefinitionVersions_ValidateAgainstTheCanonicalSchema()
    {
        var validator = new JsonSchemaNetValidator(SchemasDirectory());
        foreach (var (key, version) in NewWorkoutVersions)
        {
            var file = Path.Combine(CatalogDirectory(), "workouts", $"{ToKebab(key)}.v{version}.json");
            var json = File.ReadAllText(file);
            var result = validator.Validate(DocumentTypes.WorkoutDefinition, json);
            Assert.True(result.IsValid, $"{file}: {string.Join("; ", result.Issues.Select(i => i.Message))}");
        }
    }

    [Fact]
    public void AllExistingWorkoutDefinitions_StillValidateAgainstTheSchema()
    {
        var validator = new JsonSchemaNetValidator(SchemasDirectory());
        foreach (var file in Directory.GetFiles(Path.Combine(CatalogDirectory(), "workouts"), "*.json"))
        {
            var json = File.ReadAllText(file);
            var result = validator.Validate(DocumentTypes.WorkoutDefinition, json);
            Assert.True(result.IsValid, $"{Path.GetFileName(file)}: {string.Join("; ", result.Issues.Select(i => i.Message))}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 3. Eligibility-only diff invariant + historical immutability (all 4 extensions).
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void AerobicStrengthControlledIntro_V3_OnlyAddsFoundationEligibility()
    {
        var snapshot = LoadRealSnapshot();
        var v2 = snapshot.FindWorkout("AEROBIC_STRENGTH_CONTROLLED_INTRO", 2)!;
        var v3 = snapshot.FindWorkout("AEROBIC_STRENGTH_CONTROLLED_INTRO", 3)!;

        Assert.Equal(v2.Family, v3.Family);
        Assert.Equal(v2.AllowedPrescriptionModes, v3.AllowedPrescriptionModes);
        Assert.Equal(v2.AllowedDistanceAccountingModes, v3.AllowedDistanceAccountingModes);
        AssertSameComponentSkeleton(v2.Components!, v3.Components!);
        Assert.Equal(v2.EligiblePhases.Append(PhaseKey.Foundation).OrderBy(x => x), v3.EligiblePhases.OrderBy(x => x));
        Assert.DoesNotContain(PhaseKey.Foundation, v2.EligiblePhases);
    }

    [Fact]
    public void ThresholdTempo_V5_OnlyAddsFoundationEligibility()
    {
        var snapshot = LoadRealSnapshot();
        var v4 = snapshot.FindWorkout("THRESHOLD_TEMPO", 4)!;
        var v5 = snapshot.FindWorkout("THRESHOLD_TEMPO", 5)!;

        Assert.Equal(v4.Family, v5.Family);
        Assert.Equal(v4.AllowedPrescriptionModes, v5.AllowedPrescriptionModes);
        Assert.Equal(v4.AllowedDistanceAccountingModes, v5.AllowedDistanceAccountingModes);
        AssertSameComponentSkeleton(v4.Components!, v5.Components!);
        Assert.Equal(v4.EligiblePhases.Append(PhaseKey.Foundation).OrderBy(x => x), v5.EligiblePhases.OrderBy(x => x));
        Assert.DoesNotContain(PhaseKey.Foundation, v4.EligiblePhases);
    }

    [Fact]
    public void Fartlek_V5_OnlyAddsTaperEligibility()
    {
        var snapshot = LoadRealSnapshot();
        var v4 = snapshot.FindWorkout("FARTLEK", 4)!;
        var v5 = snapshot.FindWorkout("FARTLEK", 5)!;

        Assert.Equal(v4.Family, v5.Family);
        Assert.Equal(v4.AllowedPrescriptionModes, v5.AllowedPrescriptionModes);
        Assert.Equal(v4.AllowedDistanceAccountingModes, v5.AllowedDistanceAccountingModes);
        AssertSameComponentSkeleton(v4.Components!, v5.Components!);
        Assert.Equal(v4.EligiblePhases.Append(PhaseKey.Taper).OrderBy(x => x), v5.EligiblePhases.OrderBy(x => x));
        Assert.DoesNotContain(PhaseKey.Taper, v4.EligiblePhases);
    }

    [Fact]
    public void GoalPaceTenK_V3_AddsTaperEligibilityAndDistanceAccountingCapability()
    {
        // UPDATED IN FREQ.6D.4C.2: v3 was still DRAFT (non-historical), so root
        // cause 1's missing-metadata gap was resolved for v3 by direct
        // completion (adding allowedDistanceAccountingModes) rather than via
        // the overlay mechanism reserved for immutable historical versions
        // (that's v2's fix - see GoalPaceTenK_MissingAllowedDistanceAccountingModes below).
        // v3 therefore legitimately differs from v2 by both eligiblePhases and
        // allowedDistanceAccountingModes; everything else remains identical.
        var snapshot = LoadRealSnapshot();
        var v2 = snapshot.FindWorkout("GOAL_PACE_TEN_K", 2)!;
        var v3 = snapshot.FindWorkout("GOAL_PACE_TEN_K", 3)!;

        Assert.Equal(v2.Family, v3.Family);
        Assert.Equal(v2.ComplexityTier, v3.ComplexityTier);
        Assert.Equal(v2.AllowedPrescriptionModes, v3.AllowedPrescriptionModes);
        AssertSameComponentSkeleton(v2.Components!, v3.Components!);
        Assert.Equal(v2.EligiblePhases.Append(PhaseKey.Taper).OrderBy(x => x), v3.EligiblePhases.OrderBy(x => x));
        Assert.DoesNotContain(PhaseKey.Taper, v2.EligiblePhases);

        Assert.Null(v2.AllowedDistanceAccountingModes);
        Assert.Equal([DistanceAccountingMode.EstimatedSessionTotal], v3.AllowedDistanceAccountingModes);
    }

    private static void AssertSameComponentSkeleton(IReadOnlyList<WorkoutComponentDefinition> parent, IReadOnlyList<WorkoutComponentDefinition> child)
    {
        Assert.Equal(parent.Count, child.Count);
        for (var i = 0; i < parent.Count; i++)
        {
            Assert.Equal(parent[i].SequenceOrder, child[i].SequenceOrder);
            Assert.Equal(parent[i].ComponentType, child[i].ComponentType);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 4. Root cause 1 (AllowedDistanceAccountingModes never populated on
    //    GOAL_PACE_TEN_K v2, an immutable historical version): RESOLVED in
    //    FREQ.6D.4C.2 via the additive-only WorkoutDefinitionCapabilityOverlay
    //    (M3), never by mutating v2 itself. Without the overlay, v2 still
    //    fails closed exactly as before - the overlay is what makes RS-P
    //    representable, not a change to v2's own metadata.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void GoalPaceTenK_V2_MissingAllowedDistanceAccountingModes_StillBlocksWithoutOverlay_ButResolvedByCapabilityOverlay()
    {
        var snapshot = LoadRealSnapshot();
        var v2 = snapshot.FindWorkout("GOAL_PACE_TEN_K", 2)!; // RS-P's approved reference

        Assert.Null(v2.AllowedDistanceAccountingModes);

        // Without the overlay: still fails closed, exactly as FREQ.6D.4C found.
        foreach (DistanceAccountingMode mode in Enum.GetValues<DistanceAccountingMode>())
        {
            var candidate = MinimalCandidateProfile(v2, mode, PrescriptionIntensityMode.PaceBased);
            var result = WorkoutPrescriptionProfileValidator.Validate(candidate, v2);
            Assert.Contains(result.Issues, i => i.Code == "PROFILE_DISTANCE_ACCOUNTING_MODE_NOT_ALLOWED");
        }

        // With the real, authored FREQ.6D.4C.2 overlay: the approved
        // EstimatedSessionTotal mode is now representable; other modes remain blocked.
        var overlay = snapshot.FindCapabilityOverlay("GOAL_PACE_TEN_K", 2);
        Assert.NotNull(overlay);
        var supported = MinimalCandidateProfile(v2, DistanceAccountingMode.EstimatedSessionTotal, PrescriptionIntensityMode.PaceBased);
        var supportedResult = WorkoutPrescriptionProfileValidator.Validate(supported, v2, overlay);
        Assert.DoesNotContain(supportedResult.Issues, i => i.Code == "PROFILE_DISTANCE_ACCOUNTING_MODE_NOT_ALLOWED");

        var unsupported = MinimalCandidateProfile(v2, DistanceAccountingMode.ExactSessionTotal, PrescriptionIntensityMode.PaceBased);
        var unsupportedResult = WorkoutPrescriptionProfileValidator.Validate(unsupported, v2, overlay);
        Assert.Contains(unsupportedResult.Issues, i => i.Code == "PROFILE_DISTANCE_ACCOUNTING_MODE_NOT_ALLOWED");
    }

    [Fact]
    public void GoalPaceTenK_V3_NoLongerNeedsOverlay_DirectlyCompletedSinceStillDraft()
    {
        var snapshot = LoadRealSnapshot();
        var v3 = snapshot.FindWorkout("GOAL_PACE_TEN_K", 3)!; // TAP-P's approved reference

        Assert.Equal([DistanceAccountingMode.EstimatedSessionTotal], v3.AllowedDistanceAccountingModes);
        Assert.Null(snapshot.FindCapabilityOverlay("GOAL_PACE_TEN_K", 3));

        var candidate = MinimalCandidateProfile(v3, DistanceAccountingMode.EstimatedSessionTotal, PrescriptionIntensityMode.PaceBased);
        var result = WorkoutPrescriptionProfileValidator.Validate(candidate, v3);
        Assert.DoesNotContain(result.Issues, i => i.Code == "PROFILE_DISTANCE_ACCOUNTING_MODE_NOT_ALLOWED");
    }

    // ══════════════════════════════════════════════════════════════════
    // 5. Root cause 2 (AEROBIC_STRENGTH_CONTROLLED_INTRO, THRESHOLD_TEMPO and
    //    FARTLEK all declare allowedPrescriptionModes=["MIXED"] only - the
    //    legacy session-level dosage descriptor - which was being invalidly
    //    cross-checked against the unrelated, per-component typed
    //    PrescriptionIntensityMode axis): RESOLVED in FREQ.6D.4C.2 (M4) by
    //    removing the PROFILE_INTENSITY_MODE_NOT_ALLOWED cross-check entirely,
    //    since AllowedPrescriptionModes was never meant to gate typed intensity
    //    targeting (confirmed via Golden Fixture v3 and the legacy backend
    //    runtime's own first-class handling of MIXED). The internal,
    //    profile-only checks (PROFILE_INTENSITY_MODE_INVALID,
    //    PROFILE_INTENSITY_MODE_DESCRIPTOR_MISMATCH) remain the sufficient
    //    safety invariant and still apply.
    // ══════════════════════════════════════════════════════════════════

    public static IEnumerable<object[]> MixedOnlyWorkouts()
    {
        yield return ["AEROBIC_STRENGTH_CONTROLLED_INTRO", 3];  // FND-P's approved reference (new)
        yield return ["THRESHOLD_TEMPO", 5];                     // FND-S's approved reference (new)
        yield return ["THRESHOLD_TEMPO", 4];                     // BLD-P/RS-S's approved reference (existing, immutable)
        yield return ["FARTLEK", 4];                              // BLD-S's approved reference (existing, immutable)
        yield return ["FARTLEK", 5];                              // TAP-S's approved reference (new)
    }

    [Theory]
    [MemberData(nameof(MixedOnlyWorkouts))]
    public void MixedOnlyAllowedPrescriptionModes_NoLongerBlocksTypedIntensityModes(string key, int version)
    {
        var snapshot = LoadRealSnapshot();
        var workout = snapshot.FindWorkout(key, version)!;

        Assert.Equal([PrescriptionMode.Mixed], workout.AllowedPrescriptionModes);

        foreach (PrescriptionIntensityMode mode in Enum.GetValues<PrescriptionIntensityMode>())
        {
            var candidate = MinimalCandidateProfile(workout, DistanceAccountingMode.EstimatedSessionTotal, mode);
            var result = WorkoutPrescriptionProfileValidator.Validate(candidate, workout);
            Assert.DoesNotContain(result.Issues, i => i.Code == "PROFILE_INTENSITY_MODE_NOT_ALLOWED");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 6. Proof: the FREQ.6D.4B exact FND-P prescription content (structure,
    //    quantities, recovery, dose category) validates and projects
    //    losslessly against the REAL, unmodified v3 workout - no hypothetical
    //    `with`-isolated field fix is needed anymore, since FREQ.6D.4C.2 (M4)
    //    resolved root cause 2 for real. This directly confirms full
    //    representability of the approved policy under the current architecture.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void TheFrozenFndPPrescription_ValidatesAndProjectsLosslesslyAgainstTheRealWorkout()
    {
        var snapshot = LoadRealSnapshot();
        var realWorkout = snapshot.FindWorkout("AEROBIC_STRENGTH_CONTROLLED_INTRO", 3)!;

        var fndP = new WorkoutPrescriptionProfile
        {
            Metadata = new CatalogDocumentMetadata
            {
                DocumentType = DocumentTypes.WorkoutPrescriptionProfile,
                SchemaVersion = 1,
                Key = "AEROBIC_STRENGTH_CONTROLLED_INTRO_FOUNDATION_PRIMARY",
                Version = 1,
                Status = CatalogStatus.Draft,
            },
            WorkoutDefinitionRef = new PlanCatalog.Contracts.References.VersionedCatalogReference
            {
                DocumentType = DocumentTypes.WorkoutDefinition,
                Key = realWorkout.Metadata.Key,
                Version = realWorkout.Metadata.Version,
            },
            DoseCategory = PrescriptionDoseCategory.Primary,
            DistanceAccountingMode = DistanceAccountingMode.EstimatedSessionTotal,
            Components =
            [
                new PrescriptionProfileComponent
                {
                    SequenceOrder = 1, ComponentType = WorkoutComponentType.WarmUp, StructureMode = PrescriptionStructureMode.Continuous,
                    WorkQuantity = new PrescriptionWorkQuantity { DurationSeconds = 300 },
                    IntensityTarget = new PrescriptionIntensityTarget { Mode = PrescriptionIntensityMode.EffortBased, EffortDescriptorKey = "EASY" },
                },
                new PrescriptionProfileComponent
                {
                    SequenceOrder = 2, ComponentType = WorkoutComponentType.MainSet, StructureMode = PrescriptionStructureMode.Repeated,
                    WorkQuantity = new PrescriptionWorkQuantity { DurationSeconds = 30, RepetitionCount = 6 },
                    RecoveryQuantity = new PrescriptionRecoveryQuantity { DurationSeconds = 90, Mode = PrescriptionRecoveryMode.Jog },
                    RecoveryPlacement = PrescriptionRecoveryPlacement.BetweenRepetitions,
                    IntensityTarget = new PrescriptionIntensityTarget { Mode = PrescriptionIntensityMode.EffortBased, EffortDescriptorKey = "CONTROLLED_AEROBIC_POWER_INTRO" },
                },
                new PrescriptionProfileComponent
                {
                    SequenceOrder = 3, ComponentType = WorkoutComponentType.CoolDown, StructureMode = PrescriptionStructureMode.Continuous,
                    WorkQuantity = new PrescriptionWorkQuantity { DurationSeconds = 300 },
                    IntensityTarget = new PrescriptionIntensityTarget { Mode = PrescriptionIntensityMode.EffortBased, EffortDescriptorKey = "EASY" },
                },
            ],
        };

        // Against the REAL, unmodified v3: the frozen FREQ.6D.4B content validates cleanly.
        var result = WorkoutPrescriptionProfileValidator.Validate(fndP, realWorkout);
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Message)));

        // ...and projects losslessly, including the exact derived RecoveryCount (6 reps, Between -> 5).
        var stampedProfile = fndP with { Metadata = fndP.Metadata with { ContentHash = "test-hash-profile" } };
        var stampedWorkout = realWorkout with { Metadata = realWorkout.Metadata with { ContentHash = "test-hash-workout" } };
        var executable = new PlanCatalog.Infrastructure.Projection.WorkoutPrescriptionExecutionProjector().Project(stampedProfile, stampedWorkout);
        var mainSet = executable.Components.Single(c => c.ComponentType == WorkoutComponentType.MainSet);
        Assert.Equal(6, mainSet.RepetitionCount);
        Assert.Equal(90, mainSet.Recovery!.Value);
        Assert.Equal(5, mainSet.Recovery.RecoveryCount);
        Assert.Equal(5, PrescriptionRecoveryCardinality.Derive(6, PrescriptionRecoveryPlacement.BetweenRepetitions));
    }

    private static WorkoutPrescriptionProfile MinimalCandidateProfile(WorkoutDefinition workout, DistanceAccountingMode distanceMode, PrescriptionIntensityMode intensityMode) => new()
    {
        Metadata = new CatalogDocumentMetadata
        {
            DocumentType = DocumentTypes.WorkoutPrescriptionProfile,
            SchemaVersion = 1,
            Key = $"{workout.Metadata.Key}_CANDIDATE_PROBE",
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
        Components = workout.Components!.Select(c => new PrescriptionProfileComponent
        {
            SequenceOrder = c.SequenceOrder,
            ComponentType = c.ComponentType,
            StructureMode = PrescriptionStructureMode.Continuous,
            WorkQuantity = new PrescriptionWorkQuantity { DurationSeconds = 600 },
            IntensityTarget = intensityMode switch
            {
                PrescriptionIntensityMode.PaceBased => new PrescriptionIntensityTarget { Mode = intensityMode, PaceDescriptorKey = "PROBE" },
                PrescriptionIntensityMode.HeartRateBased => new PrescriptionIntensityTarget { Mode = intensityMode, HeartRateZoneKey = "PROBE" },
                _ => new PrescriptionIntensityTarget { Mode = intensityMode, EffortDescriptorKey = "PROBE" },
            },
        }).ToList(),
    };

    private static string ToKebab(string upperSnakeKey) => upperSnakeKey.ToLowerInvariant().Replace('_', '-');
}
