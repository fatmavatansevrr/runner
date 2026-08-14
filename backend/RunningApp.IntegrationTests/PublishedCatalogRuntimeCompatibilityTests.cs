using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using Xunit;

namespace RunningApp.IntegrationTests;

public sealed class PublishedCatalogRuntimeCompatibilityTests
{
    [Fact]
    public async Task RealPublisher_V10Closure_IsManifestedChecksummedAndLoadableWithoutSourceFallback()
    {
        using var release = new PublishedCatalogTestRelease();
        var root = release.ReleaseRoot;
        Assert.True(File.Exists(Path.Combine(root, "release-manifest.json")));
        Assert.True(File.Exists(Path.Combine(root, "checksums.sha256")));
        Assert.DoesNotContain(Path.Combine("plan-catalog", "catalog"), root, StringComparison.OrdinalIgnoreCase);

        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = root });
        var candidate = await new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance)
            .LoadCandidateAsync(V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);

        Assert.Equal("PUBLISHED", candidate.CandidateStatus);
        Assert.All(candidate.DependencyStatuses, status => Assert.Equal("PUBLISHED", status.Value));
        Assert.Equal("TEN_K_MASTER", candidate.MasterTemplate.Key);
        Assert.Equal(6, candidate.MasterTemplate.Version);
        Assert.Equal("RUN_LAYOUT_4D", candidate.Layout.Key);
        Assert.Equal(2, candidate.Layout.Version);
        Assert.Equal("INTERMEDIATE_MODIFIER", candidate.LevelModifier.Key);
        Assert.Equal(6, candidate.LevelModifier.Version);
        Assert.Equal("APPSEL_RACE_PLAN_V1", candidate.RulePack.Key);
        Assert.Equal(4, candidate.RulePack.Version);

        var progression = await new CatalogWorkoutProgressionLoader(options).LoadAsync(candidate.WorkoutProgression);
        Assert.Equal(candidate.WorkoutProgression.Key, progression.Key);
        Assert.Equal(candidate.WorkoutProgression.Version, progression.Version);

        var workoutLoader = new CatalogWorkoutDefinitionLoader(options);
        foreach (var workoutRef in candidate.ReferencedWorkouts)
        {
            var workout = await workoutLoader.LoadAsync(workoutRef);
            Assert.Equal(workoutRef.Key, workout.Key);
            Assert.Equal(workoutRef.Version, workout.Version);
            Assert.Equal("PUBLISHED", workout.Status);
        }

        var peak = await new CatalogPeakVolumeBandLoader(options).LoadAsync(
            candidate.PeakVolumeBandPolicy,
            candidate.CanonicalDistanceFamily,
            candidate.Level,
            candidate.DaysPerWeek);
        Assert.Equal(candidate.PeakVolumeBandPolicy.Key, peak.SourceArtifactKey);
        Assert.Equal(candidate.PeakVolumeBandPolicy.Version, peak.SourceArtifactVersion);

        var registry = await new RuntimeConditionRegistryReader(
            options, NullLogger<RuntimeConditionRegistryReader>.Instance)
            .LoadAsync(candidate.RuntimeConditionValueRegistry);
        Assert.Equal(candidate.RuntimeConditionValueRegistry.Key, registry.RegistryKey);
        Assert.Equal(candidate.RuntimeConditionValueRegistry.Version, registry.RegistryVersion);

        var required = new[]
        {
            ("TEMPLATE_COMBINATION", candidate.CandidateKey, candidate.CandidateVersion),
            ("PLAN_TEMPLATE", candidate.MasterTemplate.Key, candidate.MasterTemplate.Version),
            ("RUN_LAYOUT", candidate.Layout.Key, candidate.Layout.Version),
            ("LEVEL_MODIFIER", candidate.LevelModifier.Key, candidate.LevelModifier.Version),
            ("RULE_PACK", candidate.RulePack.Key, candidate.RulePack.Version),
            ("WORKOUT_PROGRESSION", candidate.WorkoutProgression.Key, candidate.WorkoutProgression.Version),
            ("PROGRESSION_MODIFIER", candidate.ProgressionModifier.Key, candidate.ProgressionModifier.Version),
            ("PEAK_VOLUME_BAND_POLICY", candidate.PeakVolumeBandPolicy.Key, candidate.PeakVolumeBandPolicy.Version),
            ("RUNTIME_CONDITION_VALUE_REGISTRY", candidate.RuntimeConditionValueRegistry.Key, candidate.RuntimeConditionValueRegistry.Version),
        };
        Assert.All(required, identity => Assert.Contains(release.Manifest.Artifacts, artifact =>
            artifact.DocumentType == identity.Item1 && artifact.Key == identity.Item2 && artifact.Version == identity.Item3));
        Assert.All(candidate.ReferencedWorkouts, workout => Assert.Contains(release.Manifest.Artifacts, artifact =>
            artifact.DocumentType == "WORKOUT_DEFINITION" && artifact.Key == workout.Key && artifact.Version == workout.Version));

        VerifyManifestContentIdentities(root, release.Manifest);
        VerifyAllChecksums(root);
    }

    [Fact]
    public async Task PublishedArtifactMissingOrTampered_FailsClosedWithoutAuthoringFallback()
    {
        using var missingRelease = new PublishedCatalogTestRelease();
        var peakPath = Path.Combine(missingRelease.ReleaseRoot, "policies", "PEAK_VOLUME_BANDS_V1.v3.json");
        File.Delete(peakPath);
        var missingLoader = new CatalogPeakVolumeBandLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = missingRelease.ReleaseRoot }));
        await Assert.ThrowsAsync<PlanCatalogLoadException>(() => missingLoader.LoadAsync(
            new PlanCatalogReference("PEAK_VOLUME_BANDS_V1", 3), "TEN_K", "INTERMEDIATE", 4));

        using var tamperedRelease = new PublishedCatalogTestRelease();
        var registryPath = Path.Combine(tamperedRelease.ReleaseRoot, "registries", "RUNTIME_CONDITION_VALUES_V1.v2.json");
        File.AppendAllText(registryPath, Environment.NewLine);
        var registryReader = new RuntimeConditionRegistryReader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = tamperedRelease.ReleaseRoot }),
            NullLogger<RuntimeConditionRegistryReader>.Instance);
        await Assert.ThrowsAsync<PlanCatalogLoadException>(() => registryReader.LoadAsync(
            new PlanCatalogReference("RUNTIME_CONDITION_VALUES_V1", 2)));
    }

    [Fact]
    public async Task RetirementLedger_ExcludesV10FromNewRelease_WhileOtherVersionsCoexist()
    {
        using var liveRelease = new PublishedCatalogTestRelease();
        Assert.Contains(liveRelease.Manifest.Artifacts, artifact =>
            artifact.DocumentType == "TEMPLATE_COMBINATION" && artifact.Key == V1CatalogPilotIdentityPolicy.CandidateKey && artifact.Version == 9);
        Assert.Contains(liveRelease.Manifest.Artifacts, artifact =>
            artifact.DocumentType == "TEMPLATE_COMBINATION" && artifact.Key == V1CatalogPilotIdentityPolicy.CandidateKey && artifact.Version == 10);
        Assert.Equal(10, V1CatalogPilotIdentityPolicy.CandidateVersion);

        using var retiredRelease = new PublishedCatalogTestRelease(retireV10: true);
        Assert.DoesNotContain(retiredRelease.Manifest.Artifacts, artifact =>
            artifact.DocumentType == "TEMPLATE_COMBINATION" && artifact.Key == V1CatalogPilotIdentityPolicy.CandidateKey && artifact.Version == 10);
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = retiredRelease.ReleaseRoot }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        await Assert.ThrowsAsync<PlanCatalogLoadException>(() => loader.LoadCandidateAsync(
            V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion));
    }

    private static void VerifyAllChecksums(string root)
    {
        foreach (var line in File.ReadAllLines(Path.Combine(root, "checksums.sha256")))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split("  ", 2, StringSplitOptions.None);
            Assert.Equal(2, parts.Length);
            var path = Path.Combine(root, parts[1].Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), parts[1]);
            var actual = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(File.ReadAllText(path)))).ToLowerInvariant();
            Assert.Equal(parts[0], actual);
        }
    }

    private static void VerifyManifestContentIdentities(
        string root,
        PlanCatalog.Contracts.Manifests.CatalogReleaseManifest manifest)
    {
        foreach (var artifact in manifest.Artifacts)
        {
            var subfolder = artifact.DocumentType switch
            {
                "TEMPLATE_COMBINATION" => "combinations",
                "PLAN_TEMPLATE" => "templates",
                "RUN_LAYOUT" => "layouts",
                "LEVEL_MODIFIER" => "level-modifiers",
                "RULE_PACK" => "rule-packs",
                "WORKOUT_PROGRESSION" => "workout-progressions",
                "PROGRESSION_MODIFIER" => "progression-modifiers",
                "WORKOUT_DEFINITION" => "workouts",
                "PEAK_VOLUME_BAND_POLICY" => "policies",
                "RUNTIME_CONDITION_VALUE_REGISTRY" => "registries",
                _ => throw new Xunit.Sdk.XunitException($"Unmapped published artifact type {artifact.DocumentType}"),
            };
            using var document = JsonDocument.Parse(File.ReadAllText(
                Path.Combine(root, subfolder, $"{artifact.Key}.v{artifact.Version}.json")));
            var metadata = document.RootElement.GetProperty("metadata");
            Assert.Equal(artifact.DocumentType, metadata.GetProperty("documentType").GetString());
            Assert.Equal(artifact.Key, metadata.GetProperty("key").GetString());
            Assert.Equal(artifact.Version, metadata.GetProperty("version").GetInt32());
            Assert.Equal("PUBLISHED", metadata.GetProperty("status").GetString());
            Assert.Equal(artifact.ContentHash, metadata.GetProperty("contentHash").GetString());
        }
    }
}
