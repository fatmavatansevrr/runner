using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4D.2 — PaceSourceResolver tests. No pace
/// projection, Riegel conversion, or race-time equivalence is exercised —
/// this resolver only decides which registry-valid PACE_SOURCE_IN source
/// type applies. No TrainingWeek/TrainingDay is created; no resolver output
/// is wired into generation.
/// </summary>
public sealed class PaceSourceResolverTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !(Directory.Exists(Path.Combine(dir.FullName, "backend")) && Directory.Exists(Path.Combine(dir.FullName, "plan-catalog"))))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root.");
    }

    private static IRuntimeConditionRegistryReader NewRegistryReader() =>
        new RuntimeConditionRegistryReader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<RuntimeConditionRegistryReader>.Instance);

    private static readonly PlanCatalogReference RegistryRef = new("RUNTIME_CONDITION_VALUES_V1", 2);

    private static RuntimeResolverContext Context(ResolverInputSnapshot input, DateOnly? asOfDate = null) =>
        new() { InputSnapshot = input, AsOfDate = asOfDate };

    // ─── Interface implementation ───────────────────────────────────────────

    [Fact]
    public void PaceSourceResolver_ImplementsIPaceSourceResolver()
    {
        Assert.IsAssignableFrom<IPaceSourceResolver>(new PaceSourceResolver());
    }

    [Fact]
    public void PaceSourceResolver_ConditionTypeIsPaceSourceIn()
    {
        Assert.Equal("PACE_SOURCE_IN", new PaceSourceResolver().ConditionType);
    }

    [Fact]
    public void PaceSourceResolver_DoesNotRequireCoreCycle()
    {
        // Independent of TimeAdequacyResolver -- no CoreCycle needed at all.
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot();

        var result = resolver.Resolve(Context(input));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
    }

    // ─── A. Complete recent race evidence → RECENT_RACE ─────────────────────

    [Fact]
    public void Resolve_CompleteRecentRaceEvidence_ReturnsEvaluatedRecentRace()
    {
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot
        {
            RecentRaceDistanceKm = 5.0,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = new DateOnly(2026, 6, 15),
        };

        var result = resolver.Resolve(Context(input));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal("RECENT_RACE", result.OutputValue);
        Assert.Equal("RECENT_RACE_RESULT_PROVIDED", result.ReasonCode);
    }

    [Fact]
    public void Resolve_CompleteRecentRaceEvidence_MetadataIncludesRaceFields()
    {
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot
        {
            RecentRaceDistanceKm = 5.0,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = new DateOnly(2026, 6, 15),
        };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("5", result.Metadata["recentRaceDistanceKm"]);
        Assert.Equal("1450", result.Metadata["recentRaceFinishTimeSeconds"]);
        Assert.Equal("2026-06-15", result.Metadata["recentRaceDate"]);
    }

    [Fact]
    public void Resolve_CompleteRecentRaceEvidence_EvenWithTargetTimePresent_StillPrefersRecentRace()
    {
        // Output priority: RECENT_RACE outranks TARGET_TIME when both are usable.
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot
        {
            RecentRaceDistanceKm = 5.0,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = new DateOnly(2026, 6, 15),
            TargetFinishTimeSeconds = 3000,
        };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("RECENT_RACE", result.OutputValue);
    }

    // ─── Recency metadata ────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, "FULL")]
    [InlineData(30, "FULL")]
    [InlineData(31, "HIGH")]
    [InlineData(60, "HIGH")]
    [InlineData(61, "MODERATE")]
    [InlineData(90, "MODERATE")]
    [InlineData(91, "LOW_CONFIRMATION_NEEDED")]
    [InlineData(180, "LOW_CONFIRMATION_NEEDED")]
    [InlineData(181, "NOT_USABLE_AS_PACE_ANCHOR")]
    [InlineData(400, "NOT_USABLE_AS_PACE_ANCHOR")]
    public void Resolve_WithAsOfDate_ComputesRaceResultAgeDaysAndConfidenceLadder(int ageDays, string expectedConfidence)
    {
        var resolver = new PaceSourceResolver();
        var raceDate = new DateOnly(2026, 6, 15);
        var input = new ResolverInputSnapshot
        {
            RecentRaceDistanceKm = 5.0,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = raceDate,
        };

        var result = resolver.Resolve(Context(input, asOfDate: raceDate.AddDays(ageDays)));

        Assert.Equal(ageDays.ToString(), result.Metadata["raceResultAgeDays"]);
        Assert.Equal(expectedConfidence, result.Metadata["paceRecencyConfidence"]);
        Assert.Equal(expectedConfidence, result.ConfidenceLabel);
    }

    [Fact]
    public void Resolve_StaleRecentRace_StillOutputsRecentRace_NeverRejected()
    {
        // Do not reject old recentRaceDate -- outputValue stays RECENT_RACE
        // regardless of confidence, per explicit task instruction.
        var resolver = new PaceSourceResolver();
        var raceDate = new DateOnly(2024, 1, 1);
        var input = new ResolverInputSnapshot
        {
            RecentRaceDistanceKm = 5.0,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = raceDate,
        };

        var result = resolver.Resolve(Context(input, asOfDate: new DateOnly(2026, 6, 15)));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal("RECENT_RACE", result.OutputValue);
        Assert.Equal("NOT_USABLE_AS_PACE_ANCHOR", result.Metadata["paceRecencyConfidence"]);
    }

    [Fact]
    public void Resolve_WithoutAsOfDate_DoesNotInventReferenceDate_ReportsNotComputed()
    {
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot
        {
            RecentRaceDistanceKm = 5.0,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = new DateOnly(2026, 6, 15),
        };

        var result = resolver.Resolve(Context(input, asOfDate: null));

        Assert.Equal("RECENT_RACE", result.OutputValue);
        Assert.False(result.Metadata.ContainsKey("raceResultAgeDays"));
        Assert.Equal("NOT_COMPUTED_NO_REFERENCE_DATE", result.Metadata["paceRecencyConfidence"]);
        Assert.Null(result.ConfidenceLabel);
    }

    [Fact]
    public void Resolve_AsOfDateBeforeRecentRaceDate_ThrowsArgumentException()
    {
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot
        {
            RecentRaceDistanceKm = 5.0,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = new DateOnly(2026, 6, 15),
        };

        Assert.Throws<ArgumentException>(() => resolver.Resolve(Context(input, asOfDate: new DateOnly(2026, 1, 1))));
    }

    // ─── B. Target finish time evidence → TARGET_TIME ───────────────────────

    [Fact]
    public void Resolve_NoRecentRace_TargetFinishTimePresent_ReturnsEvaluatedTargetTime()
    {
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 };

        var result = resolver.Resolve(Context(input));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal("TARGET_TIME", result.OutputValue);
        Assert.Equal("TARGET_FINISH_TIME_PROVIDED", result.ReasonCode);
        Assert.Equal("3000", result.Metadata["targetFinishTimeSeconds"]);
    }

    // ─── D. No pace evidence → NONE (Evaluated, not NotEvaluated) ───────────

    [Fact]
    public void Resolve_NoEvidenceAtAll_ReturnsEvaluatedNone()
    {
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot();

        var result = resolver.Resolve(Context(input));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal("NONE", result.OutputValue);
        Assert.Equal("NO_PACE_EVIDENCE_PROVIDED", result.ReasonCode);
    }

    [Fact]
    public void Resolve_None_IsEvaluatedStatus_NotNotEvaluated()
    {
        // Explicit contract distinction: absence of pace evidence is a valid
        // Evaluated/NONE outcome, never NotEvaluated.
        var resolver = new PaceSourceResolver();
        var result = resolver.Resolve(Context(new ResolverInputSnapshot()));

        Assert.NotEqual(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.NotNull(result.OutputValue);
    }

    // ─── Partial recent race evidence ────────────────────────────────────────

    [Fact]
    public void Resolve_PartialRecentRace_DistanceOnly_FallsThroughToTargetTime_WithWarning()
    {
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot
        {
            RecentRaceDistanceKm = 5.0, // distance only, no finish time or date
            TargetFinishTimeSeconds = 3000,
        };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("TARGET_TIME", result.OutputValue);
        Assert.Contains(result.Warnings, w => w.Contains("Partial recent race evidence"));
    }

    [Fact]
    public void Resolve_PartialRecentRace_FinishTimeOnly_NoTargetTime_FallsThroughToNone_WithWarning()
    {
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot
        {
            RecentRaceFinishTimeSeconds = 1450, // finish time only
        };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("NONE", result.OutputValue);
        Assert.Contains(result.Warnings, w => w.Contains("Partial recent race evidence"));
    }

    [Fact]
    public void Resolve_PartialRecentRace_DateOnly_NoTargetTime_FallsThroughToNone_WithWarning()
    {
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot
        {
            RecentRaceDate = new DateOnly(2026, 6, 15), // date only
        };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("NONE", result.OutputValue);
        Assert.Contains(result.Warnings, w => w.Contains("Partial recent race evidence"));
    }

    [Fact]
    public void Resolve_PartialRecentRace_DistanceAndTimeButNoDate_IsNotTreatedAsRecentRace()
    {
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot
        {
            RecentRaceDistanceKm = 5.0,
            RecentRaceFinishTimeSeconds = 1450,
            // RecentRaceDate missing
        };

        var result = resolver.Resolve(Context(input));

        Assert.NotEqual("RECENT_RACE", result.OutputValue);
        Assert.Equal("NONE", result.OutputValue);
    }

    [Fact]
    public void Resolve_NoRecentRaceFieldsAtAll_DoesNotEmitPartialEvidenceWarning()
    {
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 };

        var result = resolver.Resolve(Context(input));

        Assert.DoesNotContain(result.Warnings, w => w.Contains("Partial recent race evidence"));
    }

    // ─── C. ESTIMATED — not emitted in V1 ────────────────────────────────────

    [Fact]
    public void Resolve_OnlyWeeklyVolumeLongestRunRunsPerWeek_NoRecentRaceNoTargetTime_ReturnsNone_NotEstimated()
    {
        // recentWeeklyVolumeKm/recentLongestRunKm/recentRunsPerWeek are NOT,
        // by themselves, an approved V1 pace-estimate method -- must not
        // produce ESTIMATED.
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot
        {
            RecentWeeklyVolumeKm = 24.0,
            RecentLongestRunKm = 9.0,
            RecentRunsPerWeek = 4,
        };

        var result = resolver.Resolve(Context(input));

        Assert.Equal("NONE", result.OutputValue);
        Assert.NotEqual("ESTIMATED", result.OutputValue);
    }

    // ─── Registry validation ─────────────────────────────────────────────────

    [Theory]
    [InlineData("NONE")]
    [InlineData("RECENT_RACE")]
    [InlineData("ESTIMATED")]
    [InlineData("TARGET_TIME")]
    public async Task RegistryValidation_AllFourPaceSourceValues_AreRegistryValid(string value)
    {
        var snapshot = await NewRegistryReader().LoadAsync(RegistryRef);

        Assert.True(snapshot.IsValidValue("PACE_SOURCE_IN", value));
    }

    [Theory]
    [InlineData("HIGH")]
    [InlineData("MODERATE")]
    [InlineData("FULL")]
    [InlineData("LOW_CONFIRMATION_NEEDED")]
    [InlineData("NOT_USABLE_AS_PACE_ANCHOR")]
    public async Task RegistryValidation_ConfidenceLabels_AreNotValidPaceSourceOutputValues(string confidenceLabel)
    {
        var snapshot = await NewRegistryReader().LoadAsync(RegistryRef);

        Assert.False(snapshot.IsValidValue("PACE_SOURCE_IN", confidenceLabel));
    }

    [Fact]
    public async Task RegistryValidation_ResolverProducedOutputValues_AreAllRegistryValid()
    {
        var registrySnapshot = await NewRegistryReader().LoadAsync(RegistryRef);
        var resolver = new PaceSourceResolver();

        var scenarios = new[]
        {
            new ResolverInputSnapshot { RecentRaceDistanceKm = 5.0, RecentRaceFinishTimeSeconds = 1450, RecentRaceDate = new DateOnly(2026, 6, 15) },
            new ResolverInputSnapshot { TargetFinishTimeSeconds = 3000 },
            new ResolverInputSnapshot(),
        };

        foreach (var scenario in scenarios)
        {
            var result = resolver.Resolve(Context(scenario));
            Assert.True(registrySnapshot.IsValid(result));
        }
    }

    [Fact]
    public async Task RegistryValidation_NotEvaluatedIsNotAValidOutputValue()
    {
        var snapshot = await NewRegistryReader().LoadAsync(RegistryRef);

        Assert.False(snapshot.IsValidValue("PACE_SOURCE_IN", "NotEvaluated"));
        Assert.False(snapshot.IsValidValue("PACE_SOURCE_IN", "NOT_EVALUATED"));
    }

    // ─── Validation layering: resolver does not re-validate numeric input ───

    [Fact]
    public void Resolve_ResolverDoesNotThrowOnValidPositiveRecentRaceFields()
    {
        // Confirms the resolver's own logic path does not perform redundant
        // positivity validation (already handled upstream by Phase 4B).
        var resolver = new PaceSourceResolver();
        var input = new ResolverInputSnapshot
        {
            RecentRaceDistanceKm = 0.1,
            RecentRaceFinishTimeSeconds = 1,
            RecentRaceDate = new DateOnly(2026, 6, 15),
        };

        var result = resolver.Resolve(Context(input, asOfDate: new DateOnly(2026, 6, 15)));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal("RECENT_RACE", result.OutputValue);
    }
}
