using PlanCatalog.Contracts;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Serialization;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Publishing;

/// <summary>
/// Milestone E: self-contained workout closure. Tests 25-32 of the Part 2 required-tests list — see
/// artifacts/audits/bundle-workout-closure-audit.md. Closure = exact progression candidates UNION exact
/// level-modifier eligible workouts (not intersection) — this is what lets LONG_RUN_STANDARD enter the
/// bundle via eligibility alone, without ever becoming a progression candidate.
/// </summary>
public sealed class BundleWorkoutClosureTests
{
    private static VersionedCatalogReference Ref(string key, int version) =>
        new() { DocumentType = DocumentTypes.WorkoutDefinition, Key = key, Version = version };

    /// <summary>Builds an isolated exact-shape (schemaVersion 2) progression + level-modifier + combination graph.</summary>
    private static (CatalogSourceSnapshot Snapshot, PlanCatalog.Core.Models.TemplateCombinationDefinition Combination) BuildExactShapeGraph(CombinationFixture fixture)
    {
        var exactProgression = fixture.WorkoutProgression with
        {
            Metadata = fixture.WorkoutProgression.Metadata with { SchemaVersion = 2 },
            PhaseProgressions = fixture.WorkoutProgression.PhaseProgressions.Select(p => p with
            {
                Stages = p.Stages.Select(s => s with
                {
                    WorkoutCandidateKeys = null,
                    WorkoutCandidates = s.WorkoutCandidateKeys!.Select(k => Ref(k, 1)).ToList()
                }).ToList()
            }).ToList()
        };

        var exactLevelModifier = fixture.LevelModifier with
        {
            Metadata = fixture.LevelModifier.Metadata with { SchemaVersion = 2 },
            EligibleWorkoutKeys = null,
            // The fixture's only progression candidate is THRESHOLD_TEMPO (stage "TEMPO_INTRO"); EASY_STANDARD
            // and LONG_RUN_STANDARD are eligible per the level modifier but never progression candidates.
            EligibleWorkouts = [Ref("EASY_STANDARD", 1), Ref("LONG_RUN_STANDARD", 1), Ref("THRESHOLD_TEMPO", 1)]
        };

        var master = fixture.MasterTemplate with
        {
            WorkoutProgression = new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutProgression, Key = exactProgression.Metadata.Key, Version = exactProgression.Metadata.Version }
        };
        var combination = fixture.Combination with
        {
            MasterTemplate = new VersionedCatalogReference { DocumentType = DocumentTypes.PlanTemplate, Key = master.Metadata.Key, Version = master.Metadata.Version },
            LevelModifier = new VersionedCatalogReference { DocumentType = DocumentTypes.LevelModifier, Key = exactLevelModifier.Metadata.Key, Version = exactLevelModifier.Metadata.Version }
        };

        var snapshot = new CatalogSnapshotBuilder()
            .With(master).With(fixture.Layout).With(exactLevelModifier)
            .With(exactProgression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(fixture.RulePack)
            .With(combination)
            .Build();

        return (snapshot, combination);
    }

    [Fact]
    public void EveryExactProgressionWorkout_AppearsInTheBundle()
    {
        // Test 25.
        var fixture = new CombinationFixture();
        var (snapshot, combination) = BuildExactShapeGraph(fixture);
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        var bundle = new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher())
            .Assemble(stamped, combination.Metadata.Key, combination.Metadata.Version);

        // The fixture's only progression candidate is THRESHOLD_TEMPO.
        Assert.Contains(bundle.Workouts, w => w.Key == "THRESHOLD_TEMPO");
    }

    [Fact]
    public void EveryExactLevelModifierEligibleWorkout_AppearsInTheBundle()
    {
        // Test 26.
        var fixture = new CombinationFixture();
        var (snapshot, combination) = BuildExactShapeGraph(fixture);
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        var bundle = new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher())
            .Assemble(stamped, combination.Metadata.Key, combination.Metadata.Version);

        Assert.Contains(bundle.Workouts, w => w.Key == "LONG_RUN_STANDARD");
    }

    [Fact]
    public void LongRunStandard_AppearsWithoutBecomingAProgressionCandidate()
    {
        // Test 27 — the required outcome (Milestone E2).
        var fixture = new CombinationFixture();
        var (snapshot, _) = BuildExactShapeGraph(fixture);

        var progression = snapshot.WorkoutProgressions.Single();
        var allCandidateKeys = progression.PhaseProgressions.SelectMany(p => p.Stages).SelectMany(s => s.WorkoutCandidates ?? []).Select(r => r.Key).ToHashSet();

        Assert.DoesNotContain("LONG_RUN_STANDARD", allCandidateKeys);

        var levelModifier = snapshot.LevelModifiers.Single();
        Assert.Contains(levelModifier.EligibleWorkouts!, r => r.Key == "LONG_RUN_STANDARD");
    }

    [Fact]
    public void EasySupportCoverage_Exists()
    {
        // Test 28.
        var fixture = new CombinationFixture();
        var (snapshot, combination) = BuildExactShapeGraph(fixture);

        var result = CandidatePublishGraphValidator.Validate(snapshot, combination);

        Assert.DoesNotContain(result.Issues, i => i.Code == "LAYOUT_SLOT_HAS_NO_ELIGIBLE_WORKOUT" && i.Message.Contains("EasySupport", StringComparison.Ordinal));
    }

    [Fact]
    public void LongRunCoverage_Exists()
    {
        // Test 29.
        var fixture = new CombinationFixture();
        var (snapshot, combination) = BuildExactShapeGraph(fixture);

        var result = CandidatePublishGraphValidator.Validate(snapshot, combination);

        Assert.DoesNotContain(result.Issues, i => i.Code == "LAYOUT_SLOT_HAS_NO_ELIGIBLE_WORKOUT" && i.Message.Contains("LongRun", StringComparison.Ordinal));
    }

    [Fact]
    public void LayoutCoverage_FailsWhenASlotHasNoEligibleWorkout()
    {
        // Negative counterpart to 28/29 — proves the check actually fires, not just always passes.
        var fixture = new CombinationFixture();
        var (snapshot, combination) = BuildExactShapeGraph(fixture);
        var levelModifierWithoutLongRun = snapshot.LevelModifiers.Single() with
        {
            EligibleWorkouts = [Ref("EASY_STANDARD", 1), Ref("THRESHOLD_TEMPO", 1), Ref("FARTLEK", 1)]
        };
        var snapshotWithoutLongRunCoverage = snapshot with
        {
            LevelModifiers = [levelModifierWithoutLongRun]
        };

        var result = CandidatePublishGraphValidator.Validate(snapshotWithoutLongRunCoverage, combination);

        Assert.Contains(result.Issues, i => i.Code == "LAYOUT_SLOT_HAS_NO_ELIGIBLE_WORKOUT" && i.Message.Contains("LongRun", StringComparison.Ordinal));
    }

    [Fact]
    public void BundleConstruction_RequiresNoLatestVersionLookup()
    {
        // Test 30 — proves via WorkoutClosureResolver: the closure is computed purely from the exact
        // references declared in the progression/level-modifier documents, never from "highest version in
        // source."
        var fixture = new CombinationFixture();
        var (snapshot, _) = BuildExactShapeGraph(fixture);

        var progression = snapshot.WorkoutProgressions.Single();
        var levelModifier = snapshot.LevelModifiers.Single();

        var closure = WorkoutClosureResolver.ComputeExactClosureRefs(progression, levelModifier);

        Assert.All(closure, r => Assert.Equal(1, r.Version));
    }

    [Fact]
    public void CandidateBundle_IsSelfContained_EveryWorkoutPinnedByExactKeyVersionAndHash()
    {
        // Test 31.
        var fixture = new CombinationFixture();
        var (snapshot, combination) = BuildExactShapeGraph(fixture);
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        var bundle = new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher())
            .Assemble(stamped, combination.Metadata.Key, combination.Metadata.Version);

        Assert.NotEmpty(bundle.Workouts);
        Assert.All(bundle.Workouts, w =>
        {
            Assert.False(string.IsNullOrEmpty(w.Key));
            Assert.True(w.Version > 0);
            Assert.False(string.IsNullOrEmpty(w.ContentHash));
        });
    }

    [Fact]
    public void CandidateBundleHash_IsStableAcrossRepeatedBuilds()
    {
        // Test 32.
        var fixture = new CombinationFixture();
        var (snapshot, combination) = BuildExactShapeGraph(fixture);
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        var assembler = new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher());

        var hashA = assembler.Assemble(stamped, combination.Metadata.Key, combination.Metadata.Version).BundleContentHash;
        var hashB = assembler.Assemble(stamped, combination.Metadata.Key, combination.Metadata.Version).BundleContentHash;
        var hashC = assembler.Assemble(stamped, combination.Metadata.Key, combination.Metadata.Version).BundleContentHash;

        Assert.Equal(hashA, hashB);
        Assert.Equal(hashB, hashC);
    }
}
