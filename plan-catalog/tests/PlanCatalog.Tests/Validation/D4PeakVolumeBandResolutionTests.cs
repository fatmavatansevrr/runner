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

/// <summary>D4: PEAK_VOLUME_BANDS_V1 non-INTERMEDIATE row removal for the TEN_K/4D/INTERMEDIATE pilot candidate.</summary>
public sealed class D4PeakVolumeBandResolutionTests
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
    public void PolicyV3_ContainsOnlyApprovedIntermediateRows()
    {
        var snapshot = LoadSnapshot();
        var policy = snapshot.PeakVolumeBandPolicies.Single(p => p.Metadata.Key == "PEAK_VOLUME_BANDS_V1" && p.Metadata.Version == 3);

        Assert.All(policy.Entries, e => Assert.Equal(Contracts.Enums.RunningExperience.Intermediate, e.Experience));
        Assert.All(policy.Entries, e => Assert.Equal(Contracts.Enums.DistanceFamily.TenK, e.DistanceFamily));
        Assert.Equal(3, policy.Entries.Count);
    }

    [Fact]
    public void PolicyV3_ContainsExactlyApprovedD4Bands()
    {
        var snapshot = LoadSnapshot();
        var policy = snapshot.PeakVolumeBandPolicies.Single(p => p.Metadata.Key == "PEAK_VOLUME_BANDS_V1" && p.Metadata.Version == 3);

        var threeDay = policy.Entries.Single(e => e.RunsPerWeek == 3);
        var fourDay = policy.Entries.Single(e => e.RunsPerWeek == 4);
        var fiveDay = policy.Entries.Single(e => e.RunsPerWeek == 5);

        Assert.Equal(22m, threeDay.MinimumKm); Assert.Equal(32m, threeDay.MaximumKm);
        Assert.Equal(30m, fourDay.MinimumKm); Assert.Equal(42m, fourDay.MaximumKm);
        Assert.Equal(36m, fiveDay.MinimumKm); Assert.Equal(50m, fiveDay.MaximumKm);
    }

    [Fact]
    public void PolicyV3_HasNoBeginnerAdvancedExperiencedEliteRows()
    {
        var snapshot = LoadSnapshot();
        var policy = snapshot.PeakVolumeBandPolicies.Single(p => p.Metadata.Key == "PEAK_VOLUME_BANDS_V1" && p.Metadata.Version == 3);

        Assert.DoesNotContain(policy.Entries, e => e.Experience == Contracts.Enums.RunningExperience.New);
        Assert.DoesNotContain(policy.Entries, e => e.Experience == Contracts.Enums.RunningExperience.Advanced);
        Assert.DoesNotContain(policy.Entries, e => e.Experience == Contracts.Enums.RunningExperience.Experienced);

        var raw = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "policies", "peak-volume-bands.v3.json"));
        Assert.DoesNotContain("\"NEW\"", raw);
        Assert.DoesNotContain("\"ADVANCED\"", raw);
        Assert.DoesNotContain("\"EXPERIENCED\"", raw);
        Assert.DoesNotContain("BEGINNER", raw);
        Assert.DoesNotContain("ELITE", raw);
    }

    [Fact]
    public void PolicyV3_PassesStructuralValidator()
    {
        var snapshot = LoadSnapshot();
        var policy = snapshot.PeakVolumeBandPolicies.Single(p => p.Metadata.Key == "PEAK_VOLUME_BANDS_V1" && p.Metadata.Version == 3);

        Assert.True(PeakVolumeBandPolicyValidator.Validate(policy).IsValid);
    }

    [Fact]
    public void V9Bundle_UsesApproved4DayRow_ForActiveCandidate()
    {
        var bundle = BuildBundle(9);
        Assert.Equal(3, bundle.PeakVolumeBandPolicy.Version);

        var snapshot = LoadSnapshot();
        var policy = snapshot.PeakVolumeBandPolicies.Single(p => p.Metadata.Key == "PEAK_VOLUME_BANDS_V1" && p.Metadata.Version == 3);
        var fourDay = policy.Entries.Single(e => e.RunsPerWeek == 4 && e.Experience == Contracts.Enums.RunningExperience.Intermediate && e.DistanceFamily == Contracts.Enums.DistanceFamily.TenK);

        Assert.Equal(30m, fourDay.MinimumKm);
        Assert.Equal(42m, fourDay.MaximumKm);
    }

    [Fact]
    public void CandidateClosure_RemovesD4FromV9Candidate_D13Remains()
    {
        var v8Remaining = BlockingEntries(Closure(BuildBundle(8))).Select(e => $"{e.Key}:{e.JsonPath}").OrderBy(x => x).ToList();
        var v9Remaining = BlockingEntries(Closure(BuildBundle(9))).Select(e => $"{e.Key}:{e.JsonPath}").OrderBy(x => x).ToList();

        Assert.Equal(2, BlockerScopeMeasurement.ScopedDecisionCount(Closure(BuildBundle(8))));
        Assert.Equal(1, BlockerScopeMeasurement.ScopedDecisionCount(Closure(BuildBundle(9))));

        Assert.Contains(v8Remaining, e => e.StartsWith("PEAK_VOLUME_BANDS_V1:"));
        Assert.DoesNotContain(v9Remaining, e => e.StartsWith("PEAK_VOLUME_BANDS_V1:"));

        Assert.Equal([
            "GOAL_PACE_TEN_K:$.eligiblePhases, $.complexityTier, $.allowedPrescriptionModes, $.components"
        ], v9Remaining);
    }

    [Fact]
    public void V9Bundle_ReferencesExactCascadeVersions_AndDoesNotBumpUnrelatedArtifacts()
    {
        var v8 = BuildBundle(8);
        var v9 = BuildBundle(9);

        Assert.Equal(9, v9.BundleVersion);
        Assert.Equal(4, v9.RulePack.Version);
        Assert.Equal(3, v9.PeakVolumeBandPolicy.Version);

        Assert.Equal(v8.MasterTemplate.Version, v9.MasterTemplate.Version);
        Assert.Equal(v8.Layout.Version, v9.Layout.Version);
        Assert.Equal(v8.LevelModifier.Version, v9.LevelModifier.Version);
        Assert.Equal(v8.WorkoutProgression.Version, v9.WorkoutProgression.Version);
        Assert.Equal(v8.ProgressionModifier.Version, v9.ProgressionModifier.Version);
        Assert.Equal(v8.RuntimeConditionValueRegistry.Version, v9.RuntimeConditionValueRegistry.Version);
        Assert.Equal(v8.Workouts.OrderBy(w => w.Key).Select(w => (w.Key, w.Version)),
                     v9.Workouts.OrderBy(w => w.Key).Select(w => (w.Key, w.Version)));
    }

    [Fact]
    public void V9CandidateBuild_IsByteIdenticalAcrossThreeBuilds()
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var json1 = serializer.Serialize(BuildBundle(9));
        var json2 = serializer.Serialize(BuildBundle(9));
        var json3 = serializer.Serialize(BuildBundle(9));

        Assert.Equal(json1, json2);
        Assert.Equal(json2, json3);
        Assert.Equal(BuildBundle(9).BundleContentHash, BuildBundle(9).BundleContentHash);
    }

    [Fact]
    public void PriorCandidates_AreUnchangedByD4Resolution()
    {
        var v7 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "combinations", "ten-k-4d-intermediate.v7.json"));
        var v8 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "combinations", "ten-k-4d-intermediate.v8.json"));

        using var v8Doc = JsonDocument.Parse(v8);
        Assert.Equal(3, v8Doc.RootElement.GetProperty("rulePack").GetProperty("version").GetInt32());
        Assert.Contains("\"version\": 7", v7);
    }

    [Fact]
    public void Wave5D2AndD3Decisions_RemainUnchangedInV9Closure()
    {
        var snapshot = LoadSnapshot();
        var modifier = snapshot.ProgressionModifiers.Single(m => m.Metadata.Key == "INTERMEDIATE_PROGRESSION_MODIFIER_V1" && m.Metadata.Version == 2);
        Assert.Null(modifier.MaximumComplexityTier);
        Assert.Equal(1, modifier.MaximumHardSessionsPerWeek);
        Assert.False(modifier.AllowSecondHardStimulus);

        var registry = snapshot.RuntimeConditionValueRegistries.Single(r => r.Metadata.Key == "RUNTIME_CONDITION_VALUES_V1" && r.Metadata.Version == 2);
        var paceSource = registry.ConditionValueSets.Single(s => s.ConditionType == Contracts.Enums.RuntimeConditionType.PaceSourceIn);
        Assert.Equal(new HashSet<string> { "NONE", "RECENT_RACE", "ESTIMATED", "TARGET_TIME" }, paceSource.AllowedValues);

        var bundle = BuildBundle(9);
        Assert.Equal(2, bundle.ProgressionModifier.Version);
        Assert.Equal(2, bundle.RuntimeConditionValueRegistry.Version);
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
