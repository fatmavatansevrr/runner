using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Ports;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>
/// Phase 10K-FREQ.6D.4C.5 — implements the FREQ.6D.4C.4-approved lifecycle containment architecture:
/// WorkoutDefinition.EligibleForLegacyDefaultResolution is a narrow, additive, default-preserving
/// filter on CatalogSourceSnapshot.FindWorkout(string, IRetirementLedger?)'s bare-key candidate set
/// only. It never affects exact (key, version) lookup, combination activation, publisher eligibility,
/// or phase eligibility. Absent/null means eligible (true) - identical to every artifact's behavior
/// before this field existed.
/// </summary>
public sealed class LegacyResolverEligibilityContainmentTests
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

    private static WorkoutDefinition Workout(string key, int version, CatalogStatus status, bool? legacyEligible) => new()
    {
        Metadata = Meta.Of(DocumentTypes.WorkoutDefinition, key, version, status),
        Family = WorkoutFamily.Quality,
        EligiblePhases = [PhaseKey.Build],
        AllowedPrescriptionModes = [PrescriptionMode.Mixed],
        EligibleForLegacyDefaultResolution = legacyEligible,
    };

    private static CatalogSourceSnapshot EmptySnapshotWith(params WorkoutDefinition[] workouts) => new()
    {
        PlanTemplates = [],
        RunLayouts = [],
        LevelModifiers = [],
        WorkoutProgressions = [],
        ProgressionModifiers = [],
        Workouts = workouts,
        RuntimeConditionValueRegistries = [],
        PeakVolumeBandPolicies = [],
        RulePacks = [],
        Combinations = [],
    };

    // ══════════════════════════════════════════════════════════════════
    // 1-3: absent/explicit-true/explicit-false semantics (synthetic, isolated).
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void FieldAbsent_PreservesHistoricalLegacyEligibility()
    {
        var v1 = Workout("SYN_KEY", 1, CatalogStatus.Validated, legacyEligible: null);
        var snapshot = EmptySnapshotWith(v1);

        var resolved = snapshot.FindWorkout("SYN_KEY");

        Assert.NotNull(resolved);
        Assert.Equal(1, resolved!.Metadata.Version);
    }

    [Fact]
    public void ExplicitTrue_BehavesIdenticallyToAbsent()
    {
        var v1 = Workout("SYN_KEY", 1, CatalogStatus.Validated, legacyEligible: true);
        var snapshot = EmptySnapshotWith(v1);

        var resolved = snapshot.FindWorkout("SYN_KEY");

        Assert.NotNull(resolved);
        Assert.Equal(1, resolved!.Metadata.Version);
    }

    [Fact]
    public void ExplicitFalse_ExcludesFromBareKeySelection()
    {
        var v1 = Workout("SYN_KEY", 1, CatalogStatus.Validated, legacyEligible: null);
        var v2 = Workout("SYN_KEY", 2, CatalogStatus.Validated, legacyEligible: false);
        var snapshot = EmptySnapshotWith(v1, v2);

        // Higher version (2) is legacy-ineligible: the eligible v1 must still win, not null.
        var resolved = snapshot.FindWorkout("SYN_KEY");

        Assert.NotNull(resolved);
        Assert.Equal(1, resolved!.Metadata.Version);
    }

    // ══════════════════════════════════════════════════════════════════
    // 4-5: exact lookup ignores the flag; bare-key lookup respects it.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void ExactLookup_IgnoresLegacyEligibilityFlag()
    {
        var v2 = Workout("SYN_KEY", 2, CatalogStatus.Validated, legacyEligible: false);
        var snapshot = EmptySnapshotWith(v2);

        var exact = snapshot.FindWorkout("SYN_KEY", 2);

        Assert.NotNull(exact);
        Assert.Equal(2, exact!.Metadata.Version);
    }

    [Fact]
    public void BareKeyLookup_RespectsLegacyEligibilityFlag()
    {
        var v1 = Workout("SYN_KEY", 1, CatalogStatus.Validated, legacyEligible: null);
        var v2 = Workout("SYN_KEY", 2, CatalogStatus.Validated, legacyEligible: false);
        var snapshot = EmptySnapshotWith(v1, v2);

        Assert.Equal(2, snapshot.FindWorkout("SYN_KEY", 2)!.Metadata.Version);
        Assert.Equal(1, snapshot.FindWorkout("SYN_KEY")!.Metadata.Version);
    }

    // ══════════════════════════════════════════════════════════════════
    // 6-8: status rules (RETIRED/DRAFT) remain stronger than/independent of the new flag.
    // ══════════════════════════════════════════════════════════════════

    private sealed class FakeRetirementLedger(params (string DocumentType, string Key, int Version)[] retired) : IRetirementLedger
    {
        public bool IsRetired(string documentType, string key, int version) => retired.Contains((documentType, key, version));
    }

    [Fact]
    public void Retired_RemainsExcludedRegardlessOfEligibilityTrue()
    {
        var v1 = Workout("SYN_KEY", 1, CatalogStatus.Validated, legacyEligible: null);
        var v2 = Workout("SYN_KEY", 2, CatalogStatus.Validated, legacyEligible: true);
        var snapshot = EmptySnapshotWith(v1, v2);
        var ledger = new FakeRetirementLedger((DocumentTypes.WorkoutDefinition, "SYN_KEY", 2));

        var resolved = snapshot.FindWorkout("SYN_KEY", ledger);

        Assert.NotNull(resolved);
        Assert.Equal(1, resolved!.Metadata.Version);
    }

    [Fact]
    public void Draft_RemainsExcludedRegardlessOfEligibilityTrue()
    {
        var v1 = Workout("SYN_KEY", 1, CatalogStatus.Validated, legacyEligible: null);
        var v2 = Workout("SYN_KEY", 2, CatalogStatus.Draft, legacyEligible: true);
        var snapshot = EmptySnapshotWith(v1, v2);

        var resolved = snapshot.FindWorkout("SYN_KEY");

        Assert.NotNull(resolved);
        Assert.Equal(1, resolved!.Metadata.Version);
    }

    [Fact]
    public void ExplicitFalse_DoesNotChangeStatusOrRetirementFiltering()
    {
        // The flag is a candidate-set filter only - it composes with, never replaces, existing rules.
        var draftIneligible = Workout("SYN_KEY", 3, CatalogStatus.Draft, legacyEligible: false);
        var snapshot = EmptySnapshotWith(draftIneligible);

        Assert.Null(snapshot.FindWorkout("SYN_KEY"));
        Assert.NotNull(snapshot.FindWorkout("SYN_KEY", 3));
    }

    // ══════════════════════════════════════════════════════════════════
    // 9-12: real exact lookup succeeds for all four newly-VALIDATED versions.
    // ══════════════════════════════════════════════════════════════════

    public static IEnumerable<object[]> FourPromotedVersions() =>
    [
        ["AEROBIC_STRENGTH_CONTROLLED_INTRO", 3],
        ["THRESHOLD_TEMPO", 5],
        ["FARTLEK", 5],
        ["GOAL_PACE_TEN_K", 3],
    ];

    [Theory]
    [MemberData(nameof(FourPromotedVersions))]
    public void FourPromotedVersions_ExactLookupSucceeds(string key, int version)
    {
        var snapshot = LoadRealSnapshot();

        var workout = snapshot.FindWorkout(key, version);

        Assert.NotNull(workout);
        Assert.Equal(CatalogStatus.Validated, workout!.Metadata.Status);
    }

    // ══════════════════════════════════════════════════════════════════
    // 13-14: all four excluded from real bare-key default selection; prior default still wins.
    // ══════════════════════════════════════════════════════════════════

    public static IEnumerable<object[]> BareKeyExpectations() =>
    [
        ["THRESHOLD_TEMPO", 4],
        ["FARTLEK", 4],
        ["GOAL_PACE_TEN_K", 2],
    ];

    [Theory]
    [MemberData(nameof(BareKeyExpectations))]
    public void FourPromotedVersions_ExcludedFromRealBareKeyDefaultSelection_PriorDefaultStillWins(string key, int expectedPriorDefaultVersion)
    {
        var snapshot = LoadRealSnapshot();

        var resolved = snapshot.FindWorkout(key);

        Assert.NotNull(resolved);
        Assert.Equal(expectedPriorDefaultVersion, resolved!.Metadata.Version);
    }

    [Fact]
    public void AerobicStrengthControlledIntro_HasZeroLegacyEligibleCandidates_UnchangedByPromotion()
    {
        // v1/v2 are both DRAFT and v3 is legacy-ineligible: the bare key correctly has zero
        // legacy-eligible candidates, exactly as before this phase (it was never referenced by any
        // progression/level-modifier bare-key path to begin with - zero real exposure either way).
        var snapshot = LoadRealSnapshot();

        var resolved = snapshot.FindWorkout("AEROBIC_STRENGTH_CONTROLLED_INTRO");

        Assert.Null(resolved);
    }

    // ══════════════════════════════════════════════════════════════════
    // 15: live Intermediate×4D exact-reference resolution is unchanged.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void LiveIntermediate4D_ExactVersionsUnchangedAfterPromotion()
    {
        var snapshot = LoadRealSnapshot();
        var stamped = Infrastructure.Publishing.CatalogStamper.StampAsPublished(
            new Infrastructure.Serialization.SystemTextJsonCanonicalSerializer(),
            new Infrastructure.Hashing.Sha256ContentHasher(),
            snapshot);
        var assembler = new Infrastructure.Publishing.CatalogBundleAssembler(
            new Infrastructure.Serialization.SystemTextJsonCanonicalSerializer(),
            new Infrastructure.Hashing.Sha256ContentHasher());

        // TEN_K__4D__INTERMEDIATE v4 (the real, live combination) resolves via WorkoutProgression v2's
        // own exact pins (EASY_STANDARD/FARTLEK/LONG_RUN_STANDARD/THRESHOLD_TEMPO v2, GOAL_PACE_TEN_K
        // v1) - exact lookup, never the legacy bare-key resolver, so it is unaffected by construction.
        // None of the four newly-promoted (key, version) pairs may appear in its resolved closure.
        var bundle = assembler.Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 4);

        Assert.NotEmpty(bundle.Workouts);
        Assert.DoesNotContain(bundle.Workouts, w => w.Key == "AEROBIC_STRENGTH_CONTROLLED_INTRO" && w.Version == 3);
        Assert.DoesNotContain(bundle.Workouts, w => w.Key == "THRESHOLD_TEMPO" && w.Version == 5);
        Assert.DoesNotContain(bundle.Workouts, w => w.Key == "FARTLEK" && w.Version == 5);
        Assert.DoesNotContain(bundle.Workouts, w => w.Key == "GOAL_PACE_TEN_K" && w.Version == 3);
        Assert.All(bundle.Workouts.Where(w => w.Key != "GOAL_PACE_TEN_K"), w => Assert.Equal(2, w.Version));
        Assert.Contains(bundle.Workouts, w => w.Key == "GOAL_PACE_TEN_K" && w.Version == 1);
    }

    // ══════════════════════════════════════════════════════════════════
    // 16-18: historical v1-v3 resolution unchanged (re-run of the exact regression-catching tests).
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void HistoricalCombinations_ResolutionUnchangedAfterPromotion(int version)
    {
        var snapshot = LoadRealSnapshot();
        var stamped = Infrastructure.Publishing.CatalogStamper.StampAsPublished(
            new Infrastructure.Serialization.SystemTextJsonCanonicalSerializer(),
            new Infrastructure.Hashing.Sha256ContentHasher(),
            snapshot);
        var assembler = new Infrastructure.Publishing.CatalogBundleAssembler(
            new Infrastructure.Serialization.SystemTextJsonCanonicalSerializer(),
            new Infrastructure.Hashing.Sha256ContentHasher());

        var bundle = assembler.Assemble(stamped, "TEN_K__4D__INTERMEDIATE", version);

        Assert.NotEmpty(bundle.Workouts);
        Assert.All(bundle.Workouts.Where(w => w.Key != "GOAL_PACE_TEN_K"), w => Assert.Equal(4, w.Version));
        Assert.DoesNotContain(bundle.Workouts, w => w.Key == "FARTLEK" && w.Version == 5);
        Assert.DoesNotContain(bundle.Workouts, w => w.Key == "THRESHOLD_TEMPO" && w.Version == 5);
        Assert.DoesNotContain(bundle.Workouts, w => w.Key == "GOAL_PACE_TEN_K" && w.Version == 3);
    }

    // ══════════════════════════════════════════════════════════════════
    // 21/29: publisher accepts the four now-VALIDATED exact artifacts (no lifecycle rejection).
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(FourPromotedVersions))]
    public void PromotedVersions_SurviveExcludeDraftArtifacts_ViaFullSnapshotStamp(string key, int version)
    {
        var snapshot = LoadRealSnapshot();
        var stamped = Infrastructure.Publishing.CatalogStamper.StampAsPublished(
            new Infrastructure.Serialization.SystemTextJsonCanonicalSerializer(),
            new Infrastructure.Hashing.Sha256ContentHasher(),
            snapshot);

        // Stamping alone (not full ExcludeDraftArtifacts, which only CatalogPublisher.BuildRelease
        // applies) preserves every non-Draft workout, including the four now-VALIDATED versions -
        // proving they are no longer publication-blocked purely for being DRAFT. CatalogStamper maps
        // any non-Draft status to Published (its own release-stamping semantics), confirming these
        // four survived the Draft-exclusion boundary.
        var stampedWorkout = stamped.FindWorkout(key, version);
        Assert.NotNull(stampedWorkout);
        Assert.Equal(CatalogStatus.Published, stampedWorkout!.Metadata.Status);
        Assert.NotNull(stampedWorkout.Metadata.ContentHash);
    }

    // ══════════════════════════════════════════════════════════════════
    // 22: historical canonical source/hash behavior unchanged for a pre-existing artifact (no field).
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void HistoricalWorkoutV1_ContentHashUnchangedByNewField()
    {
        var snapshot = LoadRealSnapshot();
        var v1 = snapshot.Workouts.Single(w => w.Metadata.Key == "FARTLEK" && w.Metadata.Version == 1);
        Assert.Null(v1.EligibleForLegacyDefaultResolution);

        var serializer = new Infrastructure.Serialization.SystemTextJsonCanonicalSerializer();
        var hasher = new Infrastructure.Hashing.Sha256ContentHasher();
        var hash = Infrastructure.Hashing.CatalogDocumentHasher.ComputeHashExcludingField(serializer, hasher, v1, "contentHash");

        Assert.Equal("8652ed9aa01a0909ab1efffdacf1e029a164bd5784b505351b7296d6a5f89482", hash);
    }
}
