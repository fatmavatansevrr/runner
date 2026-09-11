using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net;
using System.Net.Http.Json;
using RunningApp.Application.RuntimeCatalog;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog;

public sealed class PlanCatalogDeploymentPackagingTests
{
    // Phase 10K-FREQ.6D.26 -- 97 + 5 new Intermediate x6D source documents
    // (run-layout-6d.v1.json, ten-k-master.v8.json, peak-volume-bands.v5.json,
    // appsel-race-plan.v6.json, ten-k-6d-intermediate.v1.json) = 102.
    // Phase 10K-GEN.9 -- 102 + 19 new Advanced source documents (8
    // prescription-profiles, 1 progression-modifier, 2 level-modifiers,
    // peak-volume-bands.v6.json, appsel-race-plan.v7.json,
    // ten-k-master.v9.json, ten-k-workout-progression.v7.json, 4
    // combinations) = 121.
    // Phase 10K-GEN.12 -- 121 + 6 new 2D source documents
    // (run-layout-2d.v1.json, ten-k-master.v11.json,
    // peak-volume-bands.v7.json, appsel-race-plan.v8.json, 2
    // combinations) = 127. Zero new workout/progression-modifier content
    // (2D is single-KEY, reuses existing Level-agnostic workout
    // definitions verbatim, per GEN.11 §11).
    // Phase 10K-GEN.17 -- 127 + 1 new 2D-specific workout-progression
    // document (ten-k-workout-progression-2d.v1.json, GEN.14's halved
    // exposure content) = 128. ten-k-master.v11.json's own
    // workoutProgression reference was repointed at this new document
    // in place (no new master-template version needed -- v11 is
    // referenced by no combination other than the two 2D ones).
    // Phase 10K-GEN.23 -- +3 (peak-volume-bands.v8.json, appsel-race-plan.v9.json,
    // combinations/ten-k-3d-beginner.v1.json), advancing the exact count per
    // this engagement's own GEN.10/GEN.17 precedent (never weaken the check).
    // Phase HM.2 -- +8 source documents for the first full dark HM slice:
    // master, combination, workout, progression, level/progression modifiers,
    // peak-volume policy, and rule pack.
    // Phase HM.8 -- +1 new source document (combinations/half-marathon-3d-intermediate.v1.json).
    // half-marathon-master.v1.json and peak-volume-bands.v9.json were edited IN PLACE
    // (VALIDATED-status precedent, HM.5.1/1cc7a40) -- no new file for either.
    // Phase HM.11 -- +5 new source documents: half-marathon-master.v2.json (a NEW version,
    // not an in-place edit of v1 -- HM.9's own recommended lanes-widening would have broken
    // 3D/4D's existing single-lane allocation against the same shared v1 progression document,
    // mirroring the exact real 10K precedent of TEN_K_MASTER v7/TEN_K_WORKOUT_PROGRESSION_V1 v6
    // being a SEPARATE version pair from the v6/v5 pair 3D/4D still use, never an in-place edit
    // of the shared single-lane document); half-marathon-workout-progression.v2.json (the new
    // lanes-shaped secondary-KEY-lane document, HM.9/HM.10 authority); combinations/
    // half-marathon-5d-intermediate.v1.json; workouts/fartlek.v6.json (a new, additive
    // WORKOUT_DEFINITION version widening eligiblePhases to admit RACE_SPECIFIC -- v4/v5 are
    // both unmodified and retain their existing eligibility, exactly as every existing 10K/HM caller of
    // either already observes; RACE_SPECIFIC eligibility was a genuine, unanticipated gap found
    // by direct end-to-end reproduction, not by governance-phase hand-trace, since HM.9's own
    // frozen authority requires FARTLEK specifically -- not THRESHOLD_TEMPO -- as the
    // RaceSpecific secondary-lane content); intermediate-modifier.v9.json (the minimum additive
    // level-modifier version combining HM's exact workout closure with the already-existing
    // two-hard-stimulus Intermediate progression modifier v3). peak-volume-bands.v9.json was edited IN PLACE again
    // (a second purely-additive row) -- no new file for it.
    internal const int ExpectedRuntimeCatalogJsonFiles = 145;
    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RunningApp.sln")))
            directory = directory.Parent;
        return directory?.Parent?.FullName
            ?? throw new InvalidOperationException("Repository root was not found.");
    }

    private static string SourceCatalog() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");

    [Fact]
    public void ExplicitAbsoluteOverride_IsAuthoritative()
    {
        var resolution = PlanCatalogRootResolver.Resolve(SourceCatalog(), Path.GetTempPath(), false);
        Assert.Equal(PlanCatalogRootSource.ExplicitConfiguration, resolution.Source);
        Assert.Equal(Path.GetFullPath(SourceCatalog()), resolution.CatalogRootPath);
    }

    [Fact]
    public void ExplicitRelativeOverride_IsRootedAtContentRoot_NotCurrentDirectory()
    {
        var contentRoot = Path.Combine(RepoRoot(), "backend", "RunningApp.Api");
        var resolution = PlanCatalogRootResolver.Resolve("../../plan-catalog/catalog", contentRoot, true);
        Assert.Equal(Path.GetFullPath(SourceCatalog()), resolution.CatalogRootPath);
    }

    [Fact]
    public void PackagedFallback_IsPreferredWhenNoOverrideExists()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), $"catalog-package-{Guid.NewGuid():N}");
        var packaged = Path.Combine(contentRoot, "plan-catalog", "catalog");
        Directory.CreateDirectory(packaged);
        try
        {
            var resolution = PlanCatalogRootResolver.Resolve(null, contentRoot, false);
            Assert.Equal(PlanCatalogRootSource.PackagedContentRoot, resolution.Source);
            Assert.Equal(Path.GetFullPath(packaged), resolution.CatalogRootPath);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Fact]
    public void ProductionWithoutOverrideOrPackage_FailsClearly()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), $"catalog-missing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(contentRoot);
        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                PlanCatalogRootResolver.Resolve(null, contentRoot, false));
            Assert.Contains("Runtime plan catalog is missing", exception.Message);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Fact]
    public void RuntimeCatalogInventory_IsCompleteJsonValidAndCaseSafe()
    {
        var inventory = PlanCatalogPackageValidator.Validate(SourceCatalog());
        Assert.Equal(ExpectedRuntimeCatalogJsonFiles, inventory.JsonFileCount);
        Assert.Equal(inventory.RelativePaths.Count, inventory.RelativePaths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(inventory.RelativePaths, path =>
        {
            Assert.DoesNotContain('\\', path);
            using var _ = JsonDocument.Parse(File.ReadAllText(Path.Combine(SourceCatalog(), path.Replace('/', Path.DirectorySeparatorChar))));
        });
    }

    [Fact]
    public void ApiProjectPackagesOnlyCatalogJsonIntoDeterministicTarget()
    {
        var project = File.ReadAllText(Path.Combine(RepoRoot(), "backend", "RunningApp.Api", "RunningApp.Api.csproj"));
        Assert.Contains("plan-catalog\\catalog\\**\\*.json", project);
        Assert.Contains("Link=\"plan-catalog\\catalog\\%(RecursiveDir)%(Filename)%(Extension)\"", project);
        Assert.Contains("CopyToPublishDirectory=\"PreserveNewest\"", project);
        // Phase 10K-FREQ.6D.4D Split E: exactly one pinned published-bundle release folder is
        // additionally packaged (see PlanCatalogOptions.PublishedBundleReleaseVersion), never the
        // whole artifacts/ tree (other releases, docs, tests remain excluded).
        // Phase 10K-GEN.10 defect fix: bumped from the stale 1.1.0 pin to 1.3.0,
        // matching the real configured PublishedBundleReleaseVersion.
        // Phase 10K-GEN.35: bumped again to 1.4.0 alongside the 2D LongHorizon bundle
        // publication, matching appsettings.json's own bump.
        Assert.Contains("plan-catalog\\artifacts\\appsel-plan-catalog\\1.4.0\\bundles\\**\\*.json", project);
        Assert.DoesNotContain("plan-catalog\\docs", project);
        Assert.DoesNotContain("plan-catalog\\tests", project);
    }
}

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class PackagedPlanCatalogRealHttpSmokeTests
{
    [Fact]
    public async Task ReleaseBuildCatalog_GeneratesRealTwentyOneWeekPreview()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend")))
            directory = directory.Parent;
        var repo = directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
        var packagedCatalog = Path.Combine(
            repo, "backend", "RunningApp.Api", "bin", "Release", "net9.0", "plan-catalog", "catalog");
        Assert.Equal(PlanCatalogDeploymentPackagingTests.ExpectedRuntimeCatalogJsonFiles,
            PlanCatalogPackageValidator.Validate(packagedCatalog).JsonFileCount);

        await using var factory = new CustomWebApplicationFactory(
            "Development",
            new Dictionary<string, string?> { ["PlanCatalog:CatalogRootPath"] = packagedCatalog });
        using var client = factory.CreateClient();
        (await client.PostAsync("/api/v1/testing/reset", null)).EnsureSuccessStatusCode();

        var start = new DateOnly(2026, 9, 7);
        var response = await client.PostAsJsonAsync("/api/v1/plans/generate-preview/race/long-horizon", new
        {
            goal_distance = "ten_k",
            level = "intermediate",
            days_per_week = 4,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = new[] { "mon", "wed", "fri", "sun" },
            long_run_day = "sun",
            race_date = start.AddDays(21 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480,
            target_finish_time_source = "product_average",
            recent_weekly_volume_km = 20.0,
            recent_longest_run_km = 8.0,
            recent_runs_per_week = 4,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal(1, json!["contract_version"]!.GetValue<int>());
        Assert.Equal(21, json["total_weeks"]!.GetValue<int>());
        Assert.Equal(21, json["structural_roadmap"]!.AsArray().Count);
    }
}
