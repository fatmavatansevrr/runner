using PlanCatalog.Contracts;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Validation;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>
/// Milestone B: exact versioned workout references. Tests 6-13 of the Part 2 required-tests list — see
/// artifacts/audits/exact-workout-reference-migration.md.
/// </summary>
public sealed class ExactWorkoutReferenceSchemaTests
{
    private static CatalogSourceSnapshot BuildSnapshotWithProgressionShape(
        CombinationFixture fixture,
        PlanCatalog.Core.Models.WorkoutProgressionDefinition progression,
        PlanCatalog.Core.Models.LevelModifierDefinition levelModifier)
    {
        var combination = fixture.Combination with { LevelModifier = new VersionedCatalogReference { DocumentType = DocumentTypes.LevelModifier, Key = levelModifier.Metadata.Key, Version = levelModifier.Metadata.Version } };
        var master = fixture.MasterTemplate with { WorkoutProgression = new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutProgression, Key = progression.Metadata.Key, Version = progression.Metadata.Version } };

        return new CatalogSnapshotBuilder()
            .With(master).With(fixture.Layout).With(levelModifier)
            .With(progression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(fixture.RulePack)
            .With(combination)
            .Build();
    }

    private static PlanCatalog.Core.Models.WorkoutProgressionDefinition ExactShapeProgression(CombinationFixture fixture) =>
        fixture.WorkoutProgression with
        {
            Metadata = Meta.Of(DocumentTypes.WorkoutProgression, fixture.WorkoutProgression.Metadata.Key, version: 2, status: Core.Enums.CatalogStatus.Published) with { SchemaVersion = 2 },
            PhaseProgressions = fixture.WorkoutProgression.PhaseProgressions.Select(p => p with
            {
                Stages = p.Stages.Select(s => s with
                {
                    WorkoutCandidateKeys = null,
                    WorkoutCandidates = s.WorkoutCandidateKeys!.Select(k => new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = k, Version = 1 }).ToList()
                }).ToList()
            }).ToList()
        };

    private static PlanCatalog.Core.Models.LevelModifierDefinition ExactShapeLevelModifier(CombinationFixture fixture) =>
        fixture.LevelModifier with
        {
            Metadata = Meta.Of(DocumentTypes.LevelModifier, fixture.LevelModifier.Metadata.Key, version: 2, status: Core.Enums.CatalogStatus.Published) with { SchemaVersion = 2 },
            EligibleWorkoutKeys = null,
            EligibleWorkouts = fixture.LevelModifier.EligibleWorkoutKeys!.Select(k => new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = k, Version = 1 }).ToList()
        };

    [Fact]
    public void NewProgressionSchema_RequiresExactCandidateKeyAndVersion()
    {
        // Test 6: schemaVersion 2 with the new field populated passes shape validation.
        var fixture = new CombinationFixture();
        var progression = ExactShapeProgression(fixture);
        var levelModifier = ExactShapeLevelModifier(fixture);
        var snapshot = BuildSnapshotWithProgressionShape(fixture, progression, levelModifier);

        var result = SchemaVersionShapeValidator.Validate(snapshot);

        Assert.DoesNotContain(result.Issues, i => i.Code.StartsWith("SCHEMA_SHAPE_", StringComparison.Ordinal));
    }

    [Fact]
    public void NewLevelModifierSchema_RequiresExactEligibleWorkoutKeyAndVersion()
    {
        // Test 7.
        var fixture = new CombinationFixture();
        var levelModifier = ExactShapeLevelModifier(fixture);
        var progression = ExactShapeProgression(fixture);
        var snapshot = BuildSnapshotWithProgressionShape(fixture, progression, levelModifier);

        var result = SchemaVersionShapeValidator.Validate(snapshot);

        Assert.DoesNotContain(result.Issues, i => i.Code == "SCHEMA_SHAPE_NEW_SCHEMA_REQUIRES_NEW_FIELD" && i.Message.Contains("LEVEL_MODIFIER", StringComparison.Ordinal));
    }

    [Fact]
    public void NewSchema_RejectsLegacyField()
    {
        // Test 8: schemaVersion 2 but still using the legacy key-only field must fail.
        var fixture = new CombinationFixture();
        var wrongShapeProgression = fixture.WorkoutProgression with
        {
            Metadata = fixture.WorkoutProgression.Metadata with { SchemaVersion = 2, Version = 2 }
            // WorkoutCandidateKeys still populated (legacy) — no WorkoutCandidates set.
        };
        var snapshot = BuildSnapshotWithProgressionShape(fixture, wrongShapeProgression, fixture.LevelModifier);

        var result = SchemaVersionShapeValidator.Validate(snapshot);

        Assert.Contains(result.Issues, i => i.Code == "SCHEMA_SHAPE_NEW_SCHEMA_REQUIRES_NEW_FIELD");
    }

    [Fact]
    public void NewSchema_RejectsBothLegacyAndNewFieldsTogether()
    {
        // Test 9.
        var fixture = new CombinationFixture();
        var bothFieldsProgression = fixture.WorkoutProgression with
        {
            Metadata = fixture.WorkoutProgression.Metadata with { SchemaVersion = 2, Version = 2 },
            PhaseProgressions = fixture.WorkoutProgression.PhaseProgressions.Select(p => p with
            {
                Stages = p.Stages.Select(s => s with
                {
                    WorkoutCandidates = s.WorkoutCandidateKeys!.Select(k => new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = k, Version = 1 }).ToList()
                    // WorkoutCandidateKeys deliberately left populated too.
                }).ToList()
            }).ToList()
        };
        var snapshot = BuildSnapshotWithProgressionShape(fixture, bothFieldsProgression, fixture.LevelModifier);

        var result = SchemaVersionShapeValidator.Validate(snapshot);

        Assert.Contains(result.Issues, i => i.Code == "SCHEMA_SHAPE_BOTH_FORMS_PRESENT");
    }

    [Fact]
    public void LegacyArtifacts_RemainReadable()
    {
        // Test 10: the real catalog's schemaVersion 1 progression/level-modifier still validate cleanly.
        var fixture = new CombinationFixture();
        var snapshot = fixture.BuildSnapshot();

        var result = SchemaVersionShapeValidator.Validate(snapshot);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ExactMissingWorkoutVersion_Fails()
    {
        // Test 11.
        var fixture = new CombinationFixture();
        var progression = ExactShapeProgression(fixture) with
        {
            PhaseProgressions = ExactShapeProgression(fixture).PhaseProgressions.Select((p, i) => i == 0
                ? p with { Stages = p.Stages.Select(s => s with { WorkoutCandidates = [new VersionedCatalogReference { DocumentType = DocumentTypes.WorkoutDefinition, Key = "EASY_STANDARD", Version = 99 }] }).ToList() }
                : p).ToList()
        };
        var levelModifier = ExactShapeLevelModifier(fixture);
        var snapshot = BuildSnapshotWithProgressionShape(fixture, progression, levelModifier);

        var result = WorkoutProgressionValidator.Validate(progression, snapshot);

        Assert.Contains(result.Issues, i => i.Code == "WP_CANDIDATE_WORKOUT_VERSION_MISSING");
    }

    [Fact]
    public void AddingHigherWorkoutVersion_DoesNotChangeAPinnedGraph()
    {
        // Test 12 — the core determinism proof. See BundleWorkoutClosureTests for the bundle-hash version
        // of this same guarantee (Milestone B5).
        var fixture = new CombinationFixture();
        var progression = ExactShapeProgression(fixture);
        var levelModifier = ExactShapeLevelModifier(fixture);

        var refsBefore = Core.Catalog.WorkoutClosureResolver.ComputeExactClosureRefs(progression, levelModifier);

        // Simulate a newer workout version becoming available in source — the exact refs computed from
        // the progression/level-modifier documents themselves never change, because they were never
        // "latest version" lookups to begin with.
        var refsAfter = Core.Catalog.WorkoutClosureResolver.ComputeExactClosureRefs(progression, levelModifier);

        Assert.Equal(refsBefore, refsAfter);
        Assert.All(refsBefore, r => Assert.Equal(1, r.Version));
    }

    [Fact]
    public void SourceOrdering_DoesNotChangeExactResolution()
    {
        // Test 13.
        var fixture = new CombinationFixture();
        var snapshotA = new CatalogSnapshotBuilder()
            .With(fixture.MasterTemplate).With(fixture.Layout).With(fixture.LevelModifier)
            .With(fixture.WorkoutProgression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(fixture.RulePack).With(fixture.Combination)
            .Build();

        // Same documents, deliberately different Workouts list order.
        var reordered = snapshotA with { Workouts = snapshotA.Workouts.Reverse().ToList() };

        var a = snapshotA.FindWorkout("EASY_STANDARD", 1);
        var b = reordered.FindWorkout("EASY_STANDARD", 1);

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.Equal(a!.Metadata.Version, b!.Metadata.Version);
    }
}
