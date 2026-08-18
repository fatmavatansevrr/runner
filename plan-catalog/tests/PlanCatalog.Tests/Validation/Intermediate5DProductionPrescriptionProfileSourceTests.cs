using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Projection;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>
/// Phase 10K-FREQ.6D.4C.3 — the eight REAL production WorkoutPrescriptionProfile catalog source
/// documents (catalog/prescription-profiles/*.json), materializing the exact FREQ.6D.4B / 4B.3
/// athlete-facing policy against the corrected FARTLEK v5 R1 architecture (FREQ.6D.4B.2/4B.4). All
/// assertions below run against the real repository catalog, never synthetic fixtures. These
/// profiles are NOT wired into any real 5D progression/combination bundle - existence alone must not
/// change legacy 3D/4D/Beginner behavior (see LegacyCatalog_ExecutionPrescriptionsRemainNull below).
/// </summary>
public sealed class Intermediate5DProductionPrescriptionProfileSourceTests
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

    private static string CatalogDirectory() => Path.Combine(RepoRoot(), "catalog");

    private static CatalogSourceSnapshot LoadRealSnapshot() =>
        new FileSystemCatalogSourceRepository(CatalogDirectory()).LoadSnapshot();

    private static CatalogSourceSnapshot LoadStampedSnapshot()
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        return CatalogStamper.StampAsPublished(serializer, hasher, LoadRealSnapshot());
    }

    public static IEnumerable<object[]> EightSlots()
    {
        // slot, profileKey, workoutKey, workoutVersion, dose, repeated, mainSeconds, reps, recoverySeconds, recoveryMode, intensityMode, descriptor, recoveryCount
        yield return ["FND-P", "INTERMEDIATE_5D_FOUNDATION_PRIMARY", "AEROBIC_STRENGTH_CONTROLLED_INTRO", 3, PrescriptionDoseCategory.Primary, true, 30, 6, 90, PrescriptionRecoveryMode.Jog, PrescriptionIntensityMode.EffortBased, "CONTROLLED_AEROBIC_POWER_INTRO", 5];
        yield return ["FND-S", "INTERMEDIATE_5D_FOUNDATION_SECONDARY_CONTROLLED", "THRESHOLD_TEMPO", 5, PrescriptionDoseCategory.SecondaryControlled, false, 1200, 0, 0, PrescriptionRecoveryMode.Jog, PrescriptionIntensityMode.EffortBased, "CONTROLLED_THRESHOLD_INTRO", 0];
        yield return ["BLD-P", "INTERMEDIATE_5D_BUILD_PRIMARY", "THRESHOLD_TEMPO", 4, PrescriptionDoseCategory.Primary, false, 2400, 0, 0, PrescriptionRecoveryMode.Jog, PrescriptionIntensityMode.PaceBased, "THRESHOLD_PACE", 0];
        yield return ["BLD-S", "INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED", "FARTLEK", 5, PrescriptionDoseCategory.SecondaryControlled, true, 60, 10, 60, PrescriptionRecoveryMode.Jog, PrescriptionIntensityMode.EffortBased, "SURGE_FASTER_THAN_5K_EFFORT", 9];
        yield return ["RS-P", "INTERMEDIATE_5D_RACE_SPECIFIC_PRIMARY", "GOAL_PACE_TEN_K", 2, PrescriptionDoseCategory.Primary, false, 1200, 0, 0, PrescriptionRecoveryMode.Jog, PrescriptionIntensityMode.PaceBased, "GOAL_PACE_TEN_K", 0];
        yield return ["RS-S", "INTERMEDIATE_5D_RACE_SPECIFIC_SECONDARY_CONTROLLED", "THRESHOLD_TEMPO", 4, PrescriptionDoseCategory.SecondaryControlled, false, 1500, 0, 0, PrescriptionRecoveryMode.Jog, PrescriptionIntensityMode.PaceBased, "THRESHOLD_SUPPORT_PACE", 0];
        yield return ["TAP-P", "INTERMEDIATE_5D_TAPER_PRIMARY", "GOAL_PACE_TEN_K", 3, PrescriptionDoseCategory.Primary, false, 600, 0, 0, PrescriptionRecoveryMode.Jog, PrescriptionIntensityMode.PaceBased, "GOAL_PACE_TEN_K", 0];
        yield return ["TAP-S", "INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED", "FARTLEK", 5, PrescriptionDoseCategory.SecondaryControlled, true, 20, 6, 100, PrescriptionRecoveryMode.Walk, PrescriptionIntensityMode.EffortBased, "CONTROLLED_STRIDES_SHARPENING", 5];
    }

    // ══════════════════════════════════════════════════════════════════
    // 1-9: real production profile source count/identity discovery.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void RealCatalog_HasExactlyEightProductionPrescriptionProfiles()
    {
        var snapshot = LoadRealSnapshot();
        Assert.Equal(8, snapshot.PrescriptionProfiles.Count);
    }

    [Fact]
    public void AllEightProfileIdentities_AreDistinct()
    {
        var snapshot = LoadRealSnapshot();
        var identities = snapshot.PrescriptionProfiles.Select(p => (p.Metadata.Key, p.Metadata.Version)).ToList();
        Assert.Equal(identities.Count, identities.Distinct().Count());
        Assert.Equal(8, identities.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(EightSlots))]
    public void EachSlot_LoadsWithExactWorkoutReferenceAndDoseCategory(
        string slot, string profileKey, string workoutKey, int workoutVersion, PrescriptionDoseCategory dose,
        bool repeated, int mainSeconds, int reps, int recoverySeconds, PrescriptionRecoveryMode recoveryMode,
        PrescriptionIntensityMode mode, string descriptor, int recoveryCount)
    {
        _ = (slot, repeated, mainSeconds, reps, recoverySeconds, recoveryMode, mode, descriptor, recoveryCount);
        var snapshot = LoadRealSnapshot();
        var profile = snapshot.FindPrescriptionProfile(profileKey, 1);
        Assert.NotNull(profile);
        Assert.Equal(workoutKey, profile!.WorkoutDefinitionRef.Key);
        Assert.Equal(workoutVersion, profile.WorkoutDefinitionRef.Version);
        Assert.Equal(dose, profile.DoseCategory);
        Assert.Equal(DistanceAccountingMode.EstimatedSessionTotal, profile.DistanceAccountingMode);
    }

    // ══════════════════════════════════════════════════════════════════
    // 10-13: shared WARM_UP/COOL_DOWN + no unauthorized structural RECOVERY.
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(EightSlots))]
    public void EachSlot_HasExactSharedWarmUpAndCoolDown_AndNoStructuralRecovery(
        string slot, string profileKey, string workoutKey, int workoutVersion, PrescriptionDoseCategory dose,
        bool repeated, int mainSeconds, int reps, int recoverySeconds, PrescriptionRecoveryMode recoveryMode,
        PrescriptionIntensityMode mode, string descriptor, int recoveryCount)
    {
        _ = (slot, workoutKey, workoutVersion, dose, repeated, mainSeconds, reps, recoverySeconds, recoveryMode, mode, descriptor, recoveryCount);
        var snapshot = LoadRealSnapshot();
        var profile = snapshot.FindPrescriptionProfile(profileKey, 1)!;

        Assert.DoesNotContain(profile.Components, c => c.ComponentType == WorkoutComponentType.Recovery);
        Assert.Equal(3, profile.Components.Count);

        var warmUp = profile.Components.Single(c => c.ComponentType == WorkoutComponentType.WarmUp);
        Assert.Equal(PrescriptionStructureMode.Continuous, warmUp.StructureMode);
        Assert.Equal(600, warmUp.WorkQuantity!.DurationSeconds);
        Assert.Equal(PrescriptionIntensityMode.EffortBased, warmUp.IntensityTarget.Mode);
        Assert.Equal("EASY", warmUp.IntensityTarget.EffortDescriptorKey);
        Assert.Null(warmUp.RecoveryQuantity);
        Assert.Null(warmUp.RecoveryPlacement);

        var coolDown = profile.Components.Single(c => c.ComponentType == WorkoutComponentType.CoolDown);
        Assert.Equal(PrescriptionStructureMode.Continuous, coolDown.StructureMode);
        Assert.Equal(300, coolDown.WorkQuantity!.DurationSeconds);
        Assert.Equal(PrescriptionIntensityMode.EffortBased, coolDown.IntensityTarget.Mode);
        Assert.Equal("EASY", coolDown.IntensityTarget.EffortDescriptorKey);
        Assert.Null(coolDown.RecoveryQuantity);
        Assert.Null(coolDown.RecoveryPlacement);
    }

    // ══════════════════════════════════════════════════════════════════
    // 14-19: MAIN_SET structure/recovery-placement/main-set field fidelity.
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(EightSlots))]
    public void EachSlot_MainSetMatchesExactFrozenAuthority(
        string slot, string profileKey, string workoutKey, int workoutVersion, PrescriptionDoseCategory dose,
        bool repeated, int mainSeconds, int reps, int recoverySeconds, PrescriptionRecoveryMode recoveryMode,
        PrescriptionIntensityMode mode, string descriptor, int recoveryCount)
    {
        _ = (slot, workoutKey, workoutVersion, dose);
        var snapshot = LoadRealSnapshot();
        var profile = snapshot.FindPrescriptionProfile(profileKey, 1)!;
        var main = profile.Components.Single(c => c.ComponentType == WorkoutComponentType.MainSet);

        Assert.Equal(mode, main.IntensityTarget.Mode);
        var descriptorValue = mode switch
        {
            PrescriptionIntensityMode.PaceBased => main.IntensityTarget.PaceDescriptorKey,
            _ => main.IntensityTarget.EffortDescriptorKey,
        };
        Assert.Equal(descriptor, descriptorValue);

        if (repeated)
        {
            Assert.Equal(PrescriptionStructureMode.Repeated, main.StructureMode);
            Assert.Equal(mainSeconds, main.WorkQuantity!.DurationSeconds);
            Assert.Equal(reps, main.WorkQuantity.RepetitionCount);
            Assert.NotNull(main.RecoveryQuantity);
            Assert.Equal(recoverySeconds, main.RecoveryQuantity!.DurationSeconds);
            Assert.Equal(recoveryMode, main.RecoveryQuantity.Mode);
            Assert.Equal(PrescriptionRecoveryPlacement.BetweenRepetitions, main.RecoveryPlacement);
            Assert.Equal(recoveryCount, PrescriptionRecoveryCardinality.Derive(reps, PrescriptionRecoveryPlacement.BetweenRepetitions));
        }
        else
        {
            Assert.Equal(PrescriptionStructureMode.Continuous, main.StructureMode);
            Assert.Equal(mainSeconds, main.WorkQuantity!.DurationSeconds);
            Assert.Null(main.WorkQuantity.RepetitionCount);
            Assert.Null(main.RecoveryQuantity);
            Assert.Null(main.RecoveryPlacement);
        }
    }

    [Fact]
    public void SourceProfiles_NeverAuthorRawRecoveryCount()
    {
        // Schema/model has no RecoveryCount field at all on the source component - RecoveryCount is
        // exclusively derived downstream by PrescriptionRecoveryCardinality.Derive. This test proves
        // by construction (compile-time absence + real-source round trip) that no source document
        // could have authored it.
        var snapshot = LoadRealSnapshot();
        foreach (var profile in snapshot.PrescriptionProfiles)
        {
            foreach (var component in profile.Components.Where(c => c.StructureMode == PrescriptionStructureMode.Repeated))
            {
                Assert.NotNull(component.WorkQuantity!.RepetitionCount);
                Assert.NotNull(component.RecoveryQuantity);
                Assert.NotNull(component.RecoveryPlacement);
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 20: RS-P capability-overlay-backed validation (GOAL_PACE_TEN_K v2).
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void RsP_ValidatesOnlyThroughTheRealCapabilityOverlay_NoOverlayFailsClosed()
    {
        var snapshot = LoadRealSnapshot();
        var profile = snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_RACE_SPECIFIC_PRIMARY", 1)!;
        var workout = snapshot.FindWorkout("GOAL_PACE_TEN_K", 2)!;
        Assert.Null(workout.AllowedDistanceAccountingModes);

        var withoutOverlay = WorkoutPrescriptionProfileValidator.Validate(profile, workout);
        Assert.Contains(withoutOverlay.Issues, i => i.Code == "PROFILE_DISTANCE_ACCOUNTING_MODE_NOT_ALLOWED");

        var overlay = snapshot.FindCapabilityOverlay("GOAL_PACE_TEN_K", 2);
        Assert.NotNull(overlay);
        var withOverlay = WorkoutPrescriptionProfileValidator.Validate(profile, workout, overlay);
        Assert.True(withOverlay.IsValid, string.Join("; ", withOverlay.Issues.Select(i => i.Message)));
    }

    // ══════════════════════════════════════════════════════════════════
    // 21: simultaneous real references to mixed exact WorkoutDefinition versions.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void MixedExactWorkoutVersions_AreSimultaneouslyReferencedWithoutAutoUpgrade()
    {
        var snapshot = LoadRealSnapshot();

        Assert.Equal(4, snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_BUILD_PRIMARY", 1)!.WorkoutDefinitionRef.Version);
        Assert.Equal(4, snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_RACE_SPECIFIC_SECONDARY_CONTROLLED", 1)!.WorkoutDefinitionRef.Version);
        Assert.Equal(5, snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_FOUNDATION_SECONDARY_CONTROLLED", 1)!.WorkoutDefinitionRef.Version);
        Assert.NotNull(snapshot.FindWorkout("THRESHOLD_TEMPO", 4));
        Assert.NotNull(snapshot.FindWorkout("THRESHOLD_TEMPO", 5));

        Assert.Equal(5, snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED", 1)!.WorkoutDefinitionRef.Version);
        Assert.Equal(5, snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED", 1)!.WorkoutDefinitionRef.Version);
        Assert.NotNull(snapshot.FindWorkout("FARTLEK", 4));
        Assert.NotNull(snapshot.FindWorkout("FARTLEK", 5));
        Assert.DoesNotContain(snapshot.PrescriptionProfiles, p => p.WorkoutDefinitionRef.Key == "FARTLEK" && p.WorkoutDefinitionRef.Version == 4);

        Assert.Equal(2, snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_RACE_SPECIFIC_PRIMARY", 1)!.WorkoutDefinitionRef.Version);
        Assert.Equal(3, snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_TAPER_PRIMARY", 1)!.WorkoutDefinitionRef.Version);
        Assert.NotNull(snapshot.FindWorkout("GOAL_PACE_TEN_K", 2));
        Assert.NotNull(snapshot.FindWorkout("GOAL_PACE_TEN_K", 3));
    }

    // ══════════════════════════════════════════════════════════════════
    // 22: lane-dose representability (no lane selection implementation).
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(EightSlots))]
    public void EachSlot_SatisfiesItsFrozenLaneDoseMapping(
        string slot, string profileKey, string workoutKey, int workoutVersion, PrescriptionDoseCategory dose,
        bool repeated, int mainSeconds, int reps, int recoverySeconds, PrescriptionRecoveryMode recoveryMode,
        PrescriptionIntensityMode mode, string descriptor, int recoveryCount)
    {
        _ = (slot, workoutKey, workoutVersion, repeated, mainSeconds, reps, recoverySeconds, recoveryMode, mode, descriptor, recoveryCount);
        var snapshot = LoadRealSnapshot();
        var profile = snapshot.FindPrescriptionProfile(profileKey, 1)!;
        var expectedLane = dose == PrescriptionDoseCategory.Primary ? 0 : 1;
        var matching = PrescriptionProfileLaneDoseValidator.Validate(expectedLane, profile);
        Assert.True(matching.IsValid, string.Join("; ", matching.Issues.Select(i => i.Message)));

        var otherLane = expectedLane == 0 ? 1 : 0;
        var mismatched = PrescriptionProfileLaneDoseValidator.Validate(otherLane, profile);
        Assert.Contains(mismatched.Issues, i => i.Code == "PROFILE_LANE_DOSE_CATEGORY_MISMATCH");
    }

    // ══════════════════════════════════════════════════════════════════
    // 23/26: 8/8 real catalog capacity READY.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void RealCatalogCapacityMatrix_AllEightSlotsReady()
    {
        var snapshot = LoadRealSnapshot();
        foreach (var (slot, profileKey) in new[]
        {
            ("FND-P", "INTERMEDIATE_5D_FOUNDATION_PRIMARY"), ("FND-S", "INTERMEDIATE_5D_FOUNDATION_SECONDARY_CONTROLLED"),
            ("BLD-P", "INTERMEDIATE_5D_BUILD_PRIMARY"), ("BLD-S", "INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED"),
            ("RS-P", "INTERMEDIATE_5D_RACE_SPECIFIC_PRIMARY"), ("RS-S", "INTERMEDIATE_5D_RACE_SPECIFIC_SECONDARY_CONTROLLED"),
            ("TAP-P", "INTERMEDIATE_5D_TAPER_PRIMARY"), ("TAP-S", "INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED"),
        })
        {
            var profile = snapshot.FindPrescriptionProfile(profileKey, 1);
            Assert.True(profile is not null, $"{slot}: profile source missing");
            var workout = snapshot.FindWorkout(profile!.WorkoutDefinitionRef.Key, profile.WorkoutDefinitionRef.Version);
            var overlay = snapshot.FindCapabilityOverlay(profile.WorkoutDefinitionRef.Key, profile.WorkoutDefinitionRef.Version);
            var result = WorkoutPrescriptionProfileValidator.Validate(profile, workout, overlay);
            Assert.True(result.IsValid, $"{slot}: {string.Join("; ", result.Issues.Select(i => i.Message))}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 24-31/33: real execution projection, boundary validation, hash/provenance.
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(EightSlots))]
    public void EachSlot_ProjectsLosslesslyAndPassesBoundaryValidation(
        string slot, string profileKey, string workoutKey, int workoutVersion, PrescriptionDoseCategory dose,
        bool repeated, int mainSeconds, int reps, int recoverySeconds, PrescriptionRecoveryMode recoveryMode,
        PrescriptionIntensityMode mode, string descriptor, int recoveryCount)
    {
        _ = (slot, dose, mainSeconds);
        var stamped = LoadStampedSnapshot();
        var profile = stamped.FindPrescriptionProfile(profileKey, 1)!;
        var workout = stamped.FindWorkout(workoutKey, workoutVersion)!;
        Assert.NotNull(profile.Metadata.ContentHash);
        Assert.NotNull(workout.Metadata.ContentHash);

        var executable = new WorkoutPrescriptionExecutionProjector().Project(profile, workout);

        Assert.Equal(1, executable.ContractSchemaVersion);
        Assert.Equal(profileKey, executable.SourceProfile.Key);
        Assert.Equal(1, executable.SourceProfile.Version);
        Assert.Equal(workoutKey, executable.SourceWorkout.Key);
        Assert.Equal(workoutVersion, executable.SourceWorkout.Version);
        Assert.Equal(3, executable.Components.Count);
        Assert.DoesNotContain(executable.Components, c => c.ComponentType == WorkoutComponentType.Recovery);

        var main = executable.Components.Single(c => c.ComponentType == WorkoutComponentType.MainSet);
        Assert.Equal((ExecutableIntensityMode)(int)mode, main.Intensity.Mode);
        Assert.Equal(descriptor, main.Intensity.DescriptorKey);
        if (repeated)
        {
            Assert.Equal(ExecutablePrescriptionStructureMode.Repeated, main.StructureMode);
            Assert.Equal(reps, main.RepetitionCount);
            Assert.NotNull(main.Recovery);
            Assert.Equal(recoverySeconds, main.Recovery!.Value);
            Assert.Equal((ExecutableRecoveryMode)(int)recoveryMode, main.Recovery.Mode);
            Assert.Equal(ExecutableRecoveryPlacement.BetweenRepetitions, main.Recovery.Placement);
            Assert.Equal(recoveryCount, main.Recovery.RecoveryCount);
        }
        else
        {
            Assert.Equal(ExecutablePrescriptionStructureMode.Continuous, main.StructureMode);
            Assert.Null(main.RepetitionCount);
            Assert.Null(main.Recovery);
        }

        Assert.Empty(PlanCatalog.Contracts.Prescriptions.ExecutableWorkoutPrescriptionValidator.Validate(executable));
    }

    [Fact]
    public void BldS_ProjectsExactlyThreeComponents_WithNineRecoveries_NoStructuralRecovery()
    {
        var stamped = LoadStampedSnapshot();
        var profile = stamped.FindPrescriptionProfile("INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED", 1)!;
        var workout = stamped.FindWorkout("FARTLEK", 5)!;
        var executable = new WorkoutPrescriptionExecutionProjector().Project(profile, workout);

        Assert.Equal(
            [WorkoutComponentType.WarmUp, WorkoutComponentType.MainSet, WorkoutComponentType.CoolDown],
            executable.Components.Select(c => c.ComponentType).ToList());
        var main = executable.Components.Single(c => c.ComponentType == WorkoutComponentType.MainSet);
        Assert.Equal(10, main.RepetitionCount);
        Assert.Equal(ExecutableRecoveryMode.Jog, main.Recovery!.Mode);
        Assert.Equal(ExecutableRecoveryPlacement.BetweenRepetitions, main.Recovery.Placement);
        Assert.Equal(9, main.Recovery.RecoveryCount);
    }

    [Fact]
    public void TapS_ProjectsExactlyThreeComponents_WithFiveRecoveries_NoStructuralRecovery()
    {
        var stamped = LoadStampedSnapshot();
        var profile = stamped.FindPrescriptionProfile("INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED", 1)!;
        var workout = stamped.FindWorkout("FARTLEK", 5)!;
        var executable = new WorkoutPrescriptionExecutionProjector().Project(profile, workout);

        Assert.Equal(
            [WorkoutComponentType.WarmUp, WorkoutComponentType.MainSet, WorkoutComponentType.CoolDown],
            executable.Components.Select(c => c.ComponentType).ToList());
        var main = executable.Components.Single(c => c.ComponentType == WorkoutComponentType.MainSet);
        Assert.Equal(6, main.RepetitionCount);
        Assert.Equal(ExecutableRecoveryMode.Walk, main.Recovery!.Mode);
        Assert.Equal(ExecutableRecoveryPlacement.BetweenRepetitions, main.Recovery.Placement);
        Assert.Equal(5, main.Recovery.RecoveryCount);
    }

    [Fact]
    public void FndP_ProjectsExactRecoveryCountOfFive()
    {
        var stamped = LoadStampedSnapshot();
        var profile = stamped.FindPrescriptionProfile("INTERMEDIATE_5D_FOUNDATION_PRIMARY", 1)!;
        var workout = stamped.FindWorkout("AEROBIC_STRENGTH_CONTROLLED_INTRO", 3)!;
        var executable = new WorkoutPrescriptionExecutionProjector().Project(profile, workout);
        var main = executable.Components.Single(c => c.ComponentType == WorkoutComponentType.MainSet);
        Assert.Equal(6, main.RepetitionCount);
        Assert.Equal(5, main.Recovery!.RecoveryCount);
    }

    [Fact]
    public void ProfileContentHashes_AreCanonicalAndChangeWithLoadBearingContent()
    {
        var stamped = LoadStampedSnapshot();
        foreach (var profile in stamped.PrescriptionProfiles)
        {
            Assert.False(string.IsNullOrWhiteSpace(profile.Metadata.ContentHash));
        }

        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        var original = LoadRealSnapshot();
        var target = original.PrescriptionProfiles.Single(p => p.Metadata.Key == "INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED");
        var mutated = target with
        {
            Components = [.. target.Components.Where(c => c.ComponentType != WorkoutComponentType.MainSet),
                target.Components.Single(c => c.ComponentType == WorkoutComponentType.MainSet) with
                {
                    WorkQuantity = target.Components.Single(c => c.ComponentType == WorkoutComponentType.MainSet).WorkQuantity! with { RepetitionCount = 11 }
                }]
        };
        var mutatedSnapshot = original with
        {
            PrescriptionProfiles = [.. original.PrescriptionProfiles.Where(p => p.Metadata.Key != target.Metadata.Key), mutated]
        };
        var stampedOriginal = CatalogStamper.StampAsPublished(serializer, hasher, original);
        var stampedMutated = CatalogStamper.StampAsPublished(serializer, hasher, mutatedSnapshot);
        var originalHash = stampedOriginal.FindPrescriptionProfile("INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED", 1)!.Metadata.ContentHash;
        var mutatedHash = stampedMutated.FindPrescriptionProfile("INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED", 1)!.Metadata.ContentHash;
        Assert.NotEqual(originalHash, mutatedHash);
    }

    // ══════════════════════════════════════════════════════════════════
    // 25/34: real graph validation, no duplicate profile identities.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void RealCatalog_PassesFullGraphValidation()
    {
        var snapshot = LoadRealSnapshot();
        var result = CatalogGraphValidator.Validate(snapshot);
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void DuplicateProfileKeyVersion_IsRejectedByGraphValidation()
    {
        var snapshot = LoadRealSnapshot();
        var duplicated = snapshot with
        {
            PrescriptionProfiles = [.. snapshot.PrescriptionProfiles, snapshot.PrescriptionProfiles[0]]
        };
        var result = CatalogGraphValidator.Validate(duplicated);
        Assert.Contains(result.Issues, i => i.Code.Contains("DUPLICATE", StringComparison.Ordinal));
    }

    // ══════════════════════════════════════════════════════════════════
    // 35: legacy bundle zero-delta - profiles exist but nothing auto-wires them.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void LegacyCatalog_ExecutionPrescriptionsRemainNull_DespiteRealProfilesNowExisting()
    {
        var stamped = LoadStampedSnapshot();
        Assert.Equal(8, stamped.PrescriptionProfiles.Count);

        var assembler = new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher());
        var legacy = assembler.Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 10);
        var explicitEmpty = assembler.Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 10, []);

        Assert.Null(legacy.ExecutionPrescriptions);
        Assert.Null(explicitEmpty.ExecutionPrescriptions);
    }

    // ══════════════════════════════════════════════════════════════════
    // 36: historical FARTLEK v4 remains byte-for-byte the immutable four-row skeleton.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void HistoricalFartlekV4_RemainsFourRowAndUnreferencedByAnyProductionProfile()
    {
        var snapshot = LoadRealSnapshot();
        var v4 = snapshot.FindWorkout("FARTLEK", 4)!;
        Assert.Equal(CatalogStatus.Validated, v4.Metadata.Status);
        Assert.Equal(
            [WorkoutComponentType.WarmUp, WorkoutComponentType.MainSet, WorkoutComponentType.Recovery, WorkoutComponentType.CoolDown],
            v4.Components!.Select(c => c.ComponentType).ToList());
        Assert.DoesNotContain(snapshot.PrescriptionProfiles, p => p.WorkoutDefinitionRef.Key == "FARTLEK" && p.WorkoutDefinitionRef.Version == 4);
    }

    // ══════════════════════════════════════════════════════════════════
    // Negative source tests (§44).
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void WrongWorkoutVersionReference_FailsValidation()
    {
        // BLD-P (3-component profile) checked against the historical 4-component FARTLEK v4 skeleton
        // (WARM_UP, MAIN_SET, RECOVERY, COOL_DOWN) - an exact-version substitution that must fail on
        // skeleton shape alone, independent of the (deliberately removed in FREQ.6D.4C.2) intensity-mode
        // cross-check.
        var snapshot = LoadRealSnapshot();
        var profile = snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_BUILD_PRIMARY", 1)!;
        var wrongVersionWorkout = snapshot.FindWorkout("FARTLEK", 4);
        var result = WorkoutPrescriptionProfileValidator.Validate(profile, wrongVersionWorkout);
        Assert.Contains(result.Issues, i => i.Code == "PROFILE_COMPONENT_SKELETON_MISMATCH");
    }

    [Fact]
    public void MissingReferencedDefinition_FailsValidation()
    {
        var snapshot = LoadRealSnapshot();
        var profile = snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_RACE_SPECIFIC_PRIMARY", 1)!;
        var result = WorkoutPrescriptionProfileValidator.Validate(profile, workout: null);
        Assert.Contains(result.Issues, i => i.Code == "PROFILE_WORKOUT_REFERENCE_NOT_FOUND");
    }

    [Fact]
    public void ProfileSkeletonMismatch_FailsValidation()
    {
        var snapshot = LoadRealSnapshot();
        var profile = snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED", 1)!;
        var mismatched = profile with { Components = profile.Components.Take(2).ToList() };
        var workout = snapshot.FindWorkout("FARTLEK", 5);
        var result = WorkoutPrescriptionProfileValidator.Validate(mismatched, workout);
        Assert.Contains(result.Issues, i => i.Code == "PROFILE_COMPONENT_SKELETON_MISMATCH");
    }

    [Fact]
    public void ReintroducedStructuralRecoveryOnCorrectedV5Profile_FailsSkeletonMatch()
    {
        var snapshot = LoadRealSnapshot();
        var profile = snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED", 1)!;
        var withStructuralRecovery = profile with
        {
            Components =
            [
                .. profile.Components,
                new PrescriptionProfileComponent
                {
                    SequenceOrder = 4,
                    ComponentType = WorkoutComponentType.Recovery,
                    StructureMode = PrescriptionStructureMode.Continuous,
                    WorkQuantity = new PrescriptionWorkQuantity { DurationSeconds = 60 },
                    IntensityTarget = new PrescriptionIntensityTarget { Mode = PrescriptionIntensityMode.EffortBased, EffortDescriptorKey = "EASY" },
                },
            ]
        };
        var workout = snapshot.FindWorkout("FARTLEK", 5);
        var result = WorkoutPrescriptionProfileValidator.Validate(withStructuralRecovery, workout);
        Assert.Contains(result.Issues, i => i.Code == "PROFILE_COMPONENT_SKELETON_MISMATCH");
    }

    [Fact]
    public void ContinuousSupportComponentWithRecovery_FailsValidation()
    {
        var snapshot = LoadRealSnapshot();
        var profile = snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_FOUNDATION_PRIMARY", 1)!;
        var warmUp = profile.Components.Single(c => c.ComponentType == WorkoutComponentType.WarmUp);
        var invalidWarmUp = warmUp with
        {
            RecoveryQuantity = new PrescriptionRecoveryQuantity { DurationSeconds = 30, Mode = PrescriptionRecoveryMode.Jog }
        };
        var mutated = profile with { Components = [invalidWarmUp, .. profile.Components.Skip(1)] };
        var workout = snapshot.FindWorkout("AEROBIC_STRENGTH_CONTROLLED_INTRO", 3);
        var result = WorkoutPrescriptionProfileValidator.Validate(mutated, workout);
        Assert.Contains(result.Issues, i => i.Code == "PROFILE_CONTINUOUS_RECOVERY_FORBIDDEN");
    }

    [Fact]
    public void RepeatedMainSetMissingRecoveryPlacement_FailsValidation()
    {
        var snapshot = LoadRealSnapshot();
        var profile = snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED", 1)!;
        var main = profile.Components.Single(c => c.ComponentType == WorkoutComponentType.MainSet);
        var invalidMain = main with { RecoveryPlacement = null };
        var mutated = profile with { Components = [profile.Components[0], invalidMain, profile.Components[2]] };
        var workout = snapshot.FindWorkout("FARTLEK", 5);
        var result = WorkoutPrescriptionProfileValidator.Validate(mutated, workout);
        Assert.Contains(result.Issues, i => i.Code == "PROFILE_REPEATED_RECOVERY_PLACEMENT_REQUIRED");
    }

    [Fact]
    public void InvalidEasyTypedDescriptor_FailsValidation()
    {
        var snapshot = LoadRealSnapshot();
        var profile = snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_FOUNDATION_PRIMARY", 1)!;
        var warmUp = profile.Components.Single(c => c.ComponentType == WorkoutComponentType.WarmUp);
        var invalidWarmUp = warmUp with { IntensityTarget = new PrescriptionIntensityTarget { Mode = PrescriptionIntensityMode.EffortBased } };
        var mutated = profile with { Components = [invalidWarmUp, .. profile.Components.Skip(1)] };
        var workout = snapshot.FindWorkout("AEROBIC_STRENGTH_CONTROLLED_INTRO", 3);
        var result = WorkoutPrescriptionProfileValidator.Validate(mutated, workout);
        Assert.Contains(result.Issues, i => i.Code == "PROFILE_INTENSITY_MODE_DESCRIPTOR_MISMATCH" || i.Code == "PROFILE_INTENSITY_DESCRIPTOR_INVALID");
    }

    [Fact]
    public void InvalidMainSetIntensity_FailsValidation()
    {
        var snapshot = LoadRealSnapshot();
        var profile = snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_BUILD_PRIMARY", 1)!;
        var main = profile.Components.Single(c => c.ComponentType == WorkoutComponentType.MainSet);
        var invalidMain = main with { IntensityTarget = new PrescriptionIntensityTarget { Mode = PrescriptionIntensityMode.PaceBased, PaceDescriptorKey = "" } };
        var mutated = profile with { Components = [profile.Components[0], invalidMain, profile.Components[2]] };
        var workout = snapshot.FindWorkout("THRESHOLD_TEMPO", 4);
        var result = WorkoutPrescriptionProfileValidator.Validate(mutated, workout);
        Assert.Contains(result.Issues, i => i.Code == "PROFILE_INTENSITY_DESCRIPTOR_INVALID");
    }

    [Fact]
    public void UnsupportedAccountingMode_FailsValidation()
    {
        var snapshot = LoadRealSnapshot();
        var profile = snapshot.FindPrescriptionProfile("INTERMEDIATE_5D_RACE_SPECIFIC_PRIMARY", 1)!;
        var mutated = profile with { DistanceAccountingMode = DistanceAccountingMode.ExactSessionTotal };
        var workout = snapshot.FindWorkout("GOAL_PACE_TEN_K", 2);
        var overlay = snapshot.FindCapabilityOverlay("GOAL_PACE_TEN_K", 2);
        var result = WorkoutPrescriptionProfileValidator.Validate(mutated, workout, overlay);
        Assert.Contains(result.Issues, i => i.Code == "PROFILE_DISTANCE_ACCOUNTING_MODE_NOT_ALLOWED");
    }

    [Fact]
    public void DuplicateProfileKeyVersion_IsRejectedByGraphValidation_ExactSameDocumentTwice()
    {
        var snapshot = LoadRealSnapshot();
        var doubled = snapshot with { PrescriptionProfiles = [.. snapshot.PrescriptionProfiles, snapshot.PrescriptionProfiles[3]] };
        var result = CatalogGraphValidator.Validate(doubled);
        Assert.False(result.IsValid);
    }
}
