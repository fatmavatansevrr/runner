using PlanCatalog.Contracts.Enums;
using PlanCatalog.Infrastructure.Repositories;
using Xunit;

namespace PlanCatalog.Tests.Golden;

/// <summary>
/// Confirms that the 4 pilot workout keys with direct Golden Fixture v3 evidence (EASY_STANDARD,
/// LONG_RUN_STANDARD, FARTLEK, THRESHOLD_TEMPO) carry the fixture-confirmed PrescriptionMode and
/// DistanceAccountingMode values. GOAL_PACE_TEN_K has no fixture evidence and is deliberately excluded.
/// </summary>
public sealed class PilotWorkoutFixtureConfirmationTests
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

    [Theory]
    [InlineData("EASY_STANDARD", PrescriptionMode.Distance, DistanceAccountingMode.ExactSessionTotal)]
    [InlineData("LONG_RUN_STANDARD", PrescriptionMode.Distance, DistanceAccountingMode.ExactSessionTotal)]
    [InlineData("FARTLEK", PrescriptionMode.Mixed, DistanceAccountingMode.EstimatedSessionTotal)]
    [InlineData("THRESHOLD_TEMPO", PrescriptionMode.Mixed, DistanceAccountingMode.EstimatedSessionTotal)]
    public void ConfirmedPilotWorkout_MatchesGoldenFixtureV3PrescriptionAndAccountingMode(
        string workoutKey, PrescriptionMode expectedPrescriptionMode, DistanceAccountingMode expectedAccountingMode)
    {
        var snapshot = LoadSnapshot();
        // v1 is restored to its original historically-published content (pre-vocabulary-reconciliation);
        // the fixture-confirmed values live on the corrected v2 artifact — see
        // artifacts/audits/published-workout-immutability-remediation.md.
        var workout = snapshot.Workouts.Single(w => w.Metadata.Key == workoutKey && w.Metadata.Version == 2);

        Assert.Contains(expectedPrescriptionMode, workout.AllowedPrescriptionModes);
        Assert.NotNull(workout.AllowedDistanceAccountingModes);
        Assert.Contains(expectedAccountingMode, workout.AllowedDistanceAccountingModes!);
    }

    [Fact]
    public void GoalPaceTenK_HasNoFixtureEvidence_RemainsUnconfirmedLegacyValue()
    {
        var snapshot = LoadSnapshot();
        var workout = snapshot.Workouts.Single(w => w.Metadata.Key == "GOAL_PACE_TEN_K");

        // No Golden Fixture v3 evidence exists for this key (it does not appear in the fixture's
        // workoutKeys at all) — it must not have been migrated to a guessed DISTANCE/MIXED value,
        // and must not have gained an invented AllowedDistanceAccountingModes value.
        Assert.Contains(PrescriptionMode.PaceBased, workout.AllowedPrescriptionModes);
        Assert.Null(workout.AllowedDistanceAccountingModes);
    }
}
