using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Golden;

/// <summary>
/// TEN_K_MASTER TAPER phase eligible-workout-family correction: Golden Fixture v3 Week 12 contains a
/// QUALITY activation workout (RACE_PACE_REPEATS) and a RACE workout (RACE_DAY) both scheduled in the
/// TAPER phase, so TAPER must permit EASY, LONG_RUN, QUALITY, and RACE. This does not introduce
/// WorkoutFamily.Taper — TAPER remains exclusively a PhaseKey.
/// </summary>
public sealed class TaperPhaseFamilyEligibilityTests
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

    private static PlanTemplateDefinition LoadTenKMasterV2() =>
        new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog"))
            .LoadSnapshot()
            .PlanTemplates
            .Single(t => t.Metadata.Key == "TEN_K_MASTER" && t.Metadata.Version == 2);

    private static PhaseDefinition TaperPhase() => LoadTenKMasterV2().Phases.Single(p => p.PhaseKey == PhaseKey.Taper);

    [Fact]
    public void TenKMasterV2_TaperPhase_AllowsEasy()
    {
        Assert.Contains(WorkoutFamily.Easy, TaperPhase().EligibleWorkoutFamilies);
    }

    [Fact]
    public void TenKMasterV2_TaperPhase_AllowsLongRun()
    {
        Assert.Contains(WorkoutFamily.LongRun, TaperPhase().EligibleWorkoutFamilies);
    }

    [Fact]
    public void TenKMasterV2_TaperPhase_AllowsQuality()
    {
        Assert.Contains(WorkoutFamily.Quality, TaperPhase().EligibleWorkoutFamilies);
    }

    [Fact]
    public void TenKMasterV2_TaperPhase_AllowsRace()
    {
        Assert.Contains(WorkoutFamily.Race, TaperPhase().EligibleWorkoutFamilies);
    }

    [Fact]
    public void TenKMasterV2_TaperPhase_EligibleFamilySet_IsExactlyTheFourConfirmedValues()
    {
        var families = TaperPhase().EligibleWorkoutFamilies;
        Assert.Equal(4, families.Count);
        Assert.Equal(
            new HashSet<WorkoutFamily> { WorkoutFamily.Easy, WorkoutFamily.LongRun, WorkoutFamily.Quality, WorkoutFamily.Race },
            new HashSet<WorkoutFamily>(families));
    }

    [Fact]
    public void PhaseKeyTaper_IsNotAWorkoutFamilyValue()
    {
        // TAPER must remain exclusively a PhaseKey — this is a compile-time guarantee (WorkoutFamily has
        // no "Taper" member at all) reasserted here so a future edit cannot silently reintroduce it.
        var workoutFamilyNames = Enum.GetNames<WorkoutFamily>();
        Assert.DoesNotContain(workoutFamilyNames, n => n.Equals("Taper", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(Enum.GetNames<PhaseKey>(), n => n == "Taper");
    }

    [Fact]
    public void QualityCandidateWorkout_ValidatesSuccessfullyInTaperStage()
    {
        var fixture = new CombinationFixture();
        var taperProgression = fixture.WorkoutProgression with
        {
            PhaseProgressions =
            [
                new PhaseWorkoutProgressionDefinition
                {
                    PhaseKey = PhaseKey.Taper,
                    Stages =
                    [
                        new WorkoutProgressionStageDefinition
                        {
                            StageKey = "TAPER_QUALITY_ACTIVATION",
                            RelativeOrder = 1,
                            WorkoutCandidateKeys = [fixture.ThresholdWorkout.Metadata.Key], // QUALITY family
                            MinimumExposures = 1,
                            MaximumExposures = 1,
                            CompressionBehavior = StageCompressionBehavior.Protected,
                            ExtensionBehavior = StageExtensionBehavior.FixedExposure,
                            Requires = []
                        }
                    ]
                }
            ]
        };

        var masterWithConfirmedTaper = fixture.MasterTemplate with
        {
            Phases = fixture.MasterTemplate.Phases.Select(p => p.PhaseKey == PhaseKey.Taper
                ? p with { EligibleWorkoutFamilies = [WorkoutFamily.Easy, WorkoutFamily.LongRun, WorkoutFamily.Quality, WorkoutFamily.Race] }
                : p).ToList()
        };

        var snapshot = new CatalogSnapshotBuilder()
            .With(masterWithConfirmedTaper).With(fixture.Layout).With(fixture.LevelModifier)
            .With(taperProgression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(fixture.RulePack)
            .Build();

        var result = WorkoutProgressionValidator.Validate(taperProgression, snapshot);

        Assert.DoesNotContain(result.Issues, i => i.Code == "WP_CANDIDATE_FAMILY_NOT_ELIGIBLE_FOR_PHASE");
    }

    [Fact]
    public void RaceCandidateWorkout_ValidatesSuccessfullyInTaperStage()
    {
        var fixture = new CombinationFixture();
        var raceWorkout = fixture.ThresholdWorkout with
        {
            Metadata = fixture.ThresholdWorkout.Metadata with { Key = "RACE_DAY_TEST" },
            Family = WorkoutFamily.Race
        };

        var taperProgression = fixture.WorkoutProgression with
        {
            PhaseProgressions =
            [
                new PhaseWorkoutProgressionDefinition
                {
                    PhaseKey = PhaseKey.Taper,
                    Stages =
                    [
                        new WorkoutProgressionStageDefinition
                        {
                            StageKey = "TAPER_RACE_DAY",
                            RelativeOrder = 1,
                            WorkoutCandidateKeys = [raceWorkout.Metadata.Key],
                            MinimumExposures = 1,
                            MaximumExposures = 1,
                            CompressionBehavior = StageCompressionBehavior.Protected,
                            ExtensionBehavior = StageExtensionBehavior.FixedExposure,
                            Requires = []
                        }
                    ]
                }
            ]
        };

        var masterWithConfirmedTaper = fixture.MasterTemplate with
        {
            Phases = fixture.MasterTemplate.Phases.Select(p => p.PhaseKey == PhaseKey.Taper
                ? p with { EligibleWorkoutFamilies = [WorkoutFamily.Easy, WorkoutFamily.LongRun, WorkoutFamily.Quality, WorkoutFamily.Race] }
                : p).ToList()
        };

        var snapshot = new CatalogSnapshotBuilder()
            .With(masterWithConfirmedTaper).With(fixture.Layout).With(fixture.LevelModifier)
            .With(taperProgression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(raceWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(fixture.RulePack)
            .Build();

        var result = WorkoutProgressionValidator.Validate(taperProgression, snapshot);

        Assert.DoesNotContain(result.Issues, i => i.Code == "WP_CANDIDATE_FAMILY_NOT_ELIGIBLE_FOR_PHASE");
    }

    [Fact]
    public void UnsupportedFamily_StillRejected_InAPhaseThatDoesNotAllowIt()
    {
        // Proves the TAPER correction did not weaken validation globally: FOUNDATION still only allows
        // EASY/LONG_RUN, so a RACE-family candidate there must still fail.
        var fixture = new CombinationFixture();
        var raceWorkout = fixture.ThresholdWorkout with
        {
            Metadata = fixture.ThresholdWorkout.Metadata with { Key = "RACE_IN_FOUNDATION_TEST" },
            Family = WorkoutFamily.Race
        };

        var foundationProgression = fixture.WorkoutProgression with
        {
            PhaseProgressions =
            [
                new PhaseWorkoutProgressionDefinition
                {
                    PhaseKey = PhaseKey.Foundation,
                    Stages =
                    [
                        new WorkoutProgressionStageDefinition
                        {
                            StageKey = "FOUNDATION_INVALID_RACE_CANDIDATE",
                            RelativeOrder = 1,
                            WorkoutCandidateKeys = [raceWorkout.Metadata.Key],
                            MinimumExposures = 1,
                            MaximumExposures = 1,
                            CompressionBehavior = StageCompressionBehavior.Compressible,
                            ExtensionBehavior = StageExtensionBehavior.Extendable,
                            Requires = []
                        }
                    ]
                }
            ]
        };

        var snapshot = new CatalogSnapshotBuilder()
            .With(fixture.MasterTemplate).With(fixture.Layout).With(fixture.LevelModifier)
            .With(foundationProgression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(raceWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(fixture.RulePack)
            .Build();

        var result = WorkoutProgressionValidator.Validate(foundationProgression, snapshot);

        Assert.Contains(result.Issues, i => i.Code == "WP_CANDIDATE_FAMILY_NOT_ELIGIBLE_FOR_PHASE");
    }

    [Fact]
    public void PhaseKeyAndWorkoutFamily_RemainIndependentConcepts()
    {
        // No validator should ever compare EligiblePhase directly against WorkoutFamily — the two are
        // typed as distinct enums and cannot be compared without a compile error, which is itself the
        // structural guarantee; this test documents that invariant explicitly.
        Assert.NotEqual(typeof(PhaseKey), typeof(WorkoutFamily));
    }

    [Fact]
    public void TenK4dIntermediateCombinationV1_StillReferencesMasterV1_AndValidates()
    {
        // v1 is a published, immutable historical artifact — it must never be mutated to point at the
        // corrected TEN_K_MASTER v2. It keeps referencing v1 (pre-TAPER-fix) forever.
        var repository = new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog"));
        var snapshot = repository.LoadSnapshot();
        var combinationV1 = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 1);

        Assert.Equal(1, combinationV1.MasterTemplate.Version);

        var result = TemplateCombinationValidator.Validate(combinationV1, snapshot);
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => $"{i.Code}: {i.Message}")));
    }

    [Fact]
    public void TenK4dIntermediateCombinationV2_ReferencesMasterV2_AndValidates()
    {
        var repository = new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog"));
        var snapshot = repository.LoadSnapshot();
        var combinationV2 = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 2);

        Assert.Equal(2, combinationV2.MasterTemplate.Version);

        var result = TemplateCombinationValidator.Validate(combinationV2, snapshot);
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => $"{i.Code}: {i.Message}")));
    }

    [Fact]
    public void CombinationV1AndV2_HaveDifferentContentHashes()
    {
        var serializer = new PlanCatalog.Infrastructure.Serialization.SystemTextJsonCanonicalSerializer();
        var hasher = new PlanCatalog.Infrastructure.Hashing.Sha256ContentHasher();
        var repository = new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog"));
        var snapshot = repository.LoadSnapshot();

        var v1 = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 1);
        var v2 = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 2);

        var hashV1 = PlanCatalog.Infrastructure.Hashing.CatalogDocumentHasher.ComputeContentHash(serializer, hasher, v1);
        var hashV2 = PlanCatalog.Infrastructure.Hashing.CatalogDocumentHasher.ComputeContentHash(serializer, hasher, v2);

        Assert.NotEqual(hashV1, hashV2);
    }

    [Fact]
    public void CombinationV1_ContentHash_MatchesHistoricallyPublishedHash()
    {
        // The exact hash pinned by releases 1.0.0 / 0.1.0-pilot / 0.2.0-pilot, computed before the
        // masterTemplate reference was ever mutated. Restoring v1 must reproduce this exact hash.
        const string historicallyPinnedHash = "c6324371a352a78d744583ee6bd0d36bd434b9214ff46d5ecf107e2656876c71";

        var serializer = new PlanCatalog.Infrastructure.Serialization.SystemTextJsonCanonicalSerializer();
        var hasher = new PlanCatalog.Infrastructure.Hashing.Sha256ContentHasher();
        var repository = new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog"));
        var snapshot = repository.LoadSnapshot();
        var v1 = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 1);

        var hash = PlanCatalog.Infrastructure.Hashing.CatalogDocumentHasher.ComputeContentHash(serializer, hasher, v1);

        Assert.Equal(historicallyPinnedHash, hash);
    }
}
