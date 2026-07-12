using System.Text.Json;
using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Bundles;
using PlanCatalog.Core.Audit;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Schema;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>D3: RUNTIME_CONDITION_VALUES_V1 vocabulary for PACE_SOURCE_IN, TIME_ADEQUACY_IN, CORE_ENTRY_READINESS_IN.</summary>
public sealed class D3RuntimeConditionRegistryResolutionTests
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

    private static Core.Catalog.CatalogSourceSnapshot LoadSnapshot() =>
        new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog")).LoadSnapshot();

    private static PublishedTemplateBundle BuildBundle(int version)
    {
        var snapshot = LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        return new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher()).Assemble(stamped, "TEN_K__4D__INTERMEDIATE", version);
    }

    [Fact]
    public void RegistrySchemaV1_RemainsReadable_AndV2ArtifactValidatesAgainstSchema()
    {
        var validator = new JsonSchemaNetValidator(Path.Combine(AppContext.BaseDirectory, "TestSchemas"));

        var legacyV1 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "registries", "runtime-condition-values.v1.json"));
        var newV2 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "registries", "runtime-condition-values.v2.json"));

        Assert.True(validator.Validate(DocumentTypes.RuntimeConditionValueRegistry, legacyV1).IsValid);
        Assert.True(validator.Validate(DocumentTypes.RuntimeConditionValueRegistry, newV2).IsValid);
    }

    [Fact]
    public void RegistryV2_ContainsExactlyApprovedD3Vocabulary()
    {
        var snapshot = LoadSnapshot();
        var registry = snapshot.RuntimeConditionValueRegistries.Single(r => r.Metadata.Key == "RUNTIME_CONDITION_VALUES_V1" && r.Metadata.Version == 2);

        var paceSource = registry.ConditionValueSets.Single(s => s.ConditionType == Contracts.Enums.RuntimeConditionType.PaceSourceIn);
        var timeAdequacy = registry.ConditionValueSets.Single(s => s.ConditionType == Contracts.Enums.RuntimeConditionType.TimeAdequacyIn);
        var coreEntryReadiness = registry.ConditionValueSets.Single(s => s.ConditionType == Contracts.Enums.RuntimeConditionType.CoreEntryReadinessIn);

        Assert.Equal(new HashSet<string> { "NONE", "RECENT_RACE", "ESTIMATED", "TARGET_TIME" }, paceSource.AllowedValues);
        Assert.Equal(new HashSet<string> { "ADEQUATE", "COMPRESSED", "INSUFFICIENT" }, timeAdequacy.AllowedValues);
        Assert.Equal(new HashSet<string> { "READY", "CAUTION", "NOT_READY" }, coreEntryReadiness.AllowedValues);
    }

    [Fact]
    public void RegistryV2_LeavesConfirmedConditionTypesUnchanged()
    {
        var snapshot = LoadSnapshot();
        var v1 = snapshot.RuntimeConditionValueRegistries.Single(r => r.Metadata.Key == "RUNTIME_CONDITION_VALUES_V1" && r.Metadata.Version == 1);
        var v2 = snapshot.RuntimeConditionValueRegistries.Single(r => r.Metadata.Key == "RUNTIME_CONDITION_VALUES_V1" && r.Metadata.Version == 2);

        var v1GoalFeasibility = v1.ConditionValueSets.Single(s => s.ConditionType == Contracts.Enums.RuntimeConditionType.GoalFeasibilityIn).AllowedValues;
        var v2GoalFeasibility = v2.ConditionValueSets.Single(s => s.ConditionType == Contracts.Enums.RuntimeConditionType.GoalFeasibilityIn).AllowedValues;
        var v1PlanMode = v1.ConditionValueSets.Single(s => s.ConditionType == Contracts.Enums.RuntimeConditionType.PlanModeIn).AllowedValues;
        var v2PlanMode = v2.ConditionValueSets.Single(s => s.ConditionType == Contracts.Enums.RuntimeConditionType.PlanModeIn).AllowedValues;

        Assert.Equal(v1GoalFeasibility, v2GoalFeasibility);
        Assert.Equal(v1PlanMode, v2PlanMode);
    }

    [Fact]
    public void RegistryV2_PassesStructuralValidator()
    {
        var snapshot = LoadSnapshot();
        var registry = snapshot.RuntimeConditionValueRegistries.Single(r => r.Metadata.Key == "RUNTIME_CONDITION_VALUES_V1" && r.Metadata.Version == 2);

        Assert.True(RuntimeConditionValueRegistryValidator.Validate(registry).IsValid);
    }

    [Fact]
    public void NoNonCanonicalValuesRemainAnywhereInRepositorySource()
    {
        var prohibited = new[] { "RACE_RESULT", "TIME_TRIAL", "NOT_PROVIDED", "TIGHT", "UNKNOWN", "BEGINNER", "ELITE" };
        var catalogFiles = Directory.GetFiles(Path.Combine(RepoRoot(), "catalog"), "*.json", SearchOption.AllDirectories);

        foreach (var file in catalogFiles.Where(f => !f.Contains(Path.Combine("registries", "runtime-condition-values.v1.json"))))
        {
            var content = File.ReadAllText(file);
            foreach (var value in prohibited)
            {
                Assert.False(content.Contains($"\"{value}\""), $"{file} contains non-canonical value {value}.");
            }
        }
    }

    [Fact]
    public void CandidateClosure_RemovesD3FromV8Candidate_D4AndD13Remain()
    {
        var v7Remaining = BlockingEntries(Closure(BuildBundle(7))).Select(e => $"{e.Key}:{e.JsonPath}").OrderBy(x => x).ToList();
        var v8Remaining = BlockingEntries(Closure(BuildBundle(8))).Select(e => $"{e.Key}:{e.JsonPath}").OrderBy(x => x).ToList();

        Assert.Equal(3, BlockerScopeMeasurement.ScopedDecisionCount(Closure(BuildBundle(7))));
        Assert.Equal(2, BlockerScopeMeasurement.ScopedDecisionCount(Closure(BuildBundle(8))));

        Assert.Contains(v7Remaining, e => e.StartsWith("RUNTIME_CONDITION_VALUES_V1:"));
        Assert.DoesNotContain(v8Remaining, e => e.StartsWith("RUNTIME_CONDITION_VALUES_V1:"));

        Assert.Equal([
            "GOAL_PACE_TEN_K:$.eligiblePhases, $.complexityTier, $.allowedPrescriptionModes, $.components",
            "PEAK_VOLUME_BANDS_V1:$.entries[TEN_K,NEW|ADVANCED|EXPERIENCED,3|4|5]"
        ], v8Remaining);
    }

    [Fact]
    public void Wave5D2DecisionsRemainUnchangedInV8Closure()
    {
        var snapshot = LoadSnapshot();
        var modifier = snapshot.ProgressionModifiers.Single(m => m.Metadata.Key == "INTERMEDIATE_PROGRESSION_MODIFIER_V1" && m.Metadata.Version == 2);

        Assert.Null(modifier.MaximumComplexityTier);
        Assert.Equal(1, modifier.MaximumHardSessionsPerWeek);
        Assert.Equal(1.0m, modifier.MainSetDoseMultiplier);
        Assert.True(modifier.AllowGoalPaceRehearsal);
        Assert.False(modifier.AllowSecondHardStimulus);

        var bundle = BuildBundle(8);
        Assert.Equal(2, bundle.ProgressionModifier.Version);
    }

    [Fact]
    public void V8Bundle_ReferencesExactCascadeVersions_AndDoesNotBumpUnrelatedArtifacts()
    {
        var v7 = BuildBundle(7);
        var v8 = BuildBundle(8);

        Assert.Equal(8, v8.BundleVersion);
        Assert.Equal(3, v8.RulePack.Version);
        Assert.Equal(2, v8.RuntimeConditionValueRegistry.Version);

        Assert.Equal(v7.MasterTemplate.Version, v8.MasterTemplate.Version);
        Assert.Equal(v7.Layout.Version, v8.Layout.Version);
        Assert.Equal(v7.LevelModifier.Version, v8.LevelModifier.Version);
        Assert.Equal(v7.WorkoutProgression.Version, v8.WorkoutProgression.Version);
        Assert.Equal(v7.ProgressionModifier.Version, v8.ProgressionModifier.Version);
        Assert.Equal(v7.PeakVolumeBandPolicy.Version, v8.PeakVolumeBandPolicy.Version);
        Assert.Equal(v7.Workouts.OrderBy(w => w.Key).Select(w => (w.Key, w.Version)),
                     v8.Workouts.OrderBy(w => w.Key).Select(w => (w.Key, w.Version)));
    }

    [Fact]
    public void V8CandidateBuild_IsByteIdenticalAcrossThreeBuilds()
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var json1 = serializer.Serialize(BuildBundle(8));
        var json2 = serializer.Serialize(BuildBundle(8));
        var json3 = serializer.Serialize(BuildBundle(8));

        Assert.Equal(json1, json2);
        Assert.Equal(json2, json3);
        Assert.Equal(BuildBundle(8).BundleContentHash, BuildBundle(8).BundleContentHash);
    }

    [Fact]
    public void PriorCandidates_AreUnchangedByD3Resolution()
    {
        var v6 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "combinations", "ten-k-4d-intermediate.v6.json"));
        var v7 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "combinations", "ten-k-4d-intermediate.v7.json"));

        Assert.Contains("\"version\": 6", v6);
        Assert.Contains("\"version\": 7", v7);
        using var v7Doc = JsonDocument.Parse(v7);
        Assert.Equal(2, v7Doc.RootElement.GetProperty("rulePack").GetProperty("version").GetInt32());
    }

    private static IEnumerable<BlockerScopeMeasurement.ArtifactIdentity> Closure(PublishedTemplateBundle bundle)
    {
        yield return Id(bundle.Combination);
        yield return Id(bundle.MasterTemplate);
        yield return Id(bundle.Layout);
        yield return Id(bundle.LevelModifier);
        yield return Id(bundle.WorkoutProgression);
        yield return Id(bundle.ProgressionModifier);
        yield return Id(bundle.RulePack);
        yield return Id(bundle.RuntimeConditionValueRegistry);
        yield return Id(bundle.PeakVolumeBandPolicy);
        foreach (var workout in bundle.Workouts)
        {
            yield return Id(workout);
        }
    }

    private static BlockerScopeMeasurement.ArtifactIdentity Id(Contracts.References.CatalogArtifactReference reference) =>
        new(reference.DocumentType, reference.Key, reference.Version);

    private static IReadOnlyList<DomainContentDecision> BlockingEntries(IEnumerable<BlockerScopeMeasurement.ArtifactIdentity> closure)
    {
        var scope = closure.ToHashSet();
        return PilotDomainContentAudit.Entries
            .Where(e => e.IsBlocking && scope.Contains(new BlockerScopeMeasurement.ArtifactIdentity(e.DocumentType, e.Key, e.Version)))
            .ToList();
    }
}
