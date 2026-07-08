using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace PlanCatalog.Tests.Golden;

/// <summary>
/// Structural and hash integrity checks for the Golden Fixture v3 canonical source pack
/// (docs/canonical/golden-fixture-v3/). These are read-only checks against the fixture itself — this
/// test class does not assert anything about the pilot catalog's own domain-content correctness.
/// </summary>
public sealed class GoldenFixtureV3IntegrityTests
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

    private static string FixtureDir() => Path.Combine(RepoRoot(), "docs", "canonical", "golden-fixture-v3");

    private static JsonObject LoadDecisionTrace() =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(FixtureDir(), "golden-10k-intermediate-4d-12w.v3.decisiontrace.json")))!.AsObject();

    private static JsonObject LoadPlanDocument() =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(FixtureDir(), "golden-10k-intermediate-4d-12w.v3.plandocument.json")))!.AsObject();

    [Fact]
    public void AllFourCanonicalFixtureFiles_Exist()
    {
        Assert.True(File.Exists(Path.Combine(FixtureDir(), "golden-10k-intermediate-4d-12w.v3.decisiontrace.json")));
        Assert.True(File.Exists(Path.Combine(FixtureDir(), "golden-10k-intermediate-4d-12w.v3.plandocument.json")));
        Assert.True(File.Exists(Path.Combine(FixtureDir(), "golden-10k-intermediate-4d-12w.v3.md")));
        Assert.True(File.Exists(Path.Combine(FixtureDir(), "progression_rules_v2.yaml")));
    }

    [Fact]
    public void BothJsonFixtures_Parse()
    {
        Assert.NotNull(LoadDecisionTrace());
        Assert.NotNull(LoadPlanDocument());
    }

    [Fact]
    public void YamlFixture_ContainsExpectedSchemaVersionAndKey()
    {
        var yaml = File.ReadAllText(Path.Combine(FixtureDir(), "progression_rules_v2.yaml"));
        Assert.Contains("schemaVersion: 2", yaml, StringComparison.Ordinal);
        Assert.Contains("ruleFileKey: PROGRESSION_V2", yaml, StringComparison.Ordinal);
    }

    [Fact]
    public void FixtureKeys_Match_AcrossBothJsonFiles()
    {
        var dt = LoadDecisionTrace();
        var pd = LoadPlanDocument();

        Assert.Equal("GOLDEN_10K_INTERMEDIATE_4D_12W", dt["fixtureKey"]!.GetValue<string>());
        Assert.Equal("GOLDEN_10K_INTERMEDIATE_4D_12W", pd["fixtureKey"]!.GetValue<string>());
    }

    [Fact]
    public void SchemaAndRevisionVersions_MatchExpected()
    {
        var dt = LoadDecisionTrace();
        var pd = LoadPlanDocument();

        Assert.Equal(3, dt["schemaVersion"]!.GetValue<int>());
        Assert.Equal(3, pd["schemaVersion"]!.GetValue<int>());
        Assert.Equal(3, pd["fixtureRevision"]!.GetValue<int>());
    }

    [Fact]
    public void PlanDocumentContentHash_Verifies()
    {
        var pd = LoadPlanDocument();
        var expected = pd["contentHash"]!.GetValue<string>();

        JsonNode? Canon(JsonNode? n)
        {
            if (n is JsonArray arr)
            {
                var copy = new JsonArray();
                foreach (var item in arr)
                {
                    copy.Add(Canon(item?.DeepClone()));
                }

                return copy;
            }

            if (n is JsonObject obj)
            {
                var keys = obj.Select(kv => kv.Key)
                    .Where(k => k != "contentHash" && obj[k] is not null)
                    .OrderBy(k => k, StringComparer.Ordinal)
                    .ToList();

                var copy = new JsonObject();
                foreach (var k in keys)
                {
                    copy[k] = Canon(obj[k]?.DeepClone());
                }

                return copy;
            }

            return n?.DeepClone();
        }

        var canonical = Canon(pd);
        var options = new JsonSerializerOptions { WriteIndented = false, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var json = canonical!.ToJsonString(options).Normalize(NormalizationForm.FormC);
        var actual = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WeekBoundaries_MatchExpectedDates()
    {
        var pd = LoadPlanDocument();
        var weeks = pd["weeks"]!.AsArray();

        Assert.Equal(12, weeks.Count);

        var week1 = weeks[0]!.AsObject();
        Assert.Equal("2026-08-03", week1["weekStartDate"]!.GetValue<string>());
        Assert.Equal("2026-08-09", week1["weekEndDate"]!.GetValue<string>());

        var week12 = weeks[11]!.AsObject();
        Assert.Equal("2026-10-19", week12["weekStartDate"]!.GetValue<string>());
        Assert.Equal("2026-10-25", week12["weekEndDate"]!.GetValue<string>());

        Assert.Equal("2026-10-19", pd["horizon"]!["raceWeekStartDate"]!.GetValue<string>());

        foreach (var weekNode in weeks)
        {
            var week = weekNode!.AsObject();
            var start = DateOnly.Parse(week["weekStartDate"]!.GetValue<string>());
            var end = DateOnly.Parse(week["weekEndDate"]!.GetValue<string>());

            Assert.Equal(6, end.DayNumber - start.DayNumber);
            Assert.Equal(DayOfWeek.Monday, start.DayOfWeek);
            Assert.Equal(DayOfWeek.Sunday, end.DayOfWeek);
        }
    }

    [Fact]
    public void Week12Totals_MatchExpectedTaperDistribution()
    {
        var pd = LoadPlanDocument();
        var week12 = pd["weeks"]!.AsArray()[11]!.AsObject();

        Assert.Equal(20m, week12["plannedTrainingDistanceKm"]!.GetValue<decimal>());
        Assert.Equal(10m, week12["plannedRaceDistanceKm"]!.GetValue<decimal>());
        Assert.Equal(30m, week12["totalPlannedDistanceKm"]!.GetValue<decimal>());

        var days = week12["days"]!.AsArray();
        var trainingDistances = days
            .Select(d => d!.AsObject())
            .Where(d => d["dayType"] is null)
            .Select(d => d["workout"]!["plannedDistanceKm"]!.GetValue<decimal>())
            .ToList();

        Assert.Equal(new[] { 8m, 8m, 4m }, trainingDistances);
        Assert.Equal(20m, trainingDistances.Sum());
    }

    [Fact]
    public void RaceDay_AccountedSeparatelyFromTraining()
    {
        var pd = LoadPlanDocument();
        var week12 = pd["weeks"]!.AsArray()[11]!.AsObject();
        var raceDay = week12["days"]!.AsArray().Select(d => d!.AsObject()).Single(d => d["dayType"]?.GetValue<string>() == "RACE");

        Assert.Equal("RACE", raceDay["workout"]!["loadClassification"]!.GetValue<string>());
        Assert.Equal("RACE_EXCLUDED_FROM_TRAINING_HARD_COUNT", raceDay["workout"]!["stimulusAccountingScope"]!.GetValue<string>());
    }

    [Fact]
    public void PipelineAndPolicyInvariants_ArePresent()
    {
        var dtText = File.ReadAllText(Path.Combine(FixtureDir(), "golden-10k-intermediate-4d-12w.v3.decisiontrace.json"));
        var pdText = File.ReadAllText(Path.Combine(FixtureDir(), "golden-10k-intermediate-4d-12w.v3.plandocument.json"));
        var mdText = File.ReadAllText(Path.Combine(FixtureDir(), "golden-10k-intermediate-4d-12w.v3.md"));

        Assert.Contains("PHASE_TRANSITION_DELOAD", dtText, StringComparison.Ordinal);
        Assert.Contains("PLANNED_SINGLE_SESSION_SPIKE_CHECK", dtText, StringComparison.Ordinal);
        Assert.Contains("FINAL_VALIDATION", dtText, StringComparison.Ordinal);
        Assert.Contains("WARNING_POLICY_V2", dtText, StringComparison.Ordinal);
        Assert.Contains("ESTIMATED_THRESHOLD_EFFORT", pdText, StringComparison.Ordinal);

        Assert.DoesNotContain("CURRENT_LACTATE_THRESHOLD_EFFORT", dtText, StringComparison.Ordinal);
        Assert.DoesNotContain("CURRENT_LACTATE_THRESHOLD_EFFORT", pdText, StringComparison.Ordinal);
        Assert.Contains("CURRENT_LACTATE_THRESHOLD_EFFORT", mdText, StringComparison.Ordinal); // documented as superseded, not as an active field

        Assert.Contains("Warning Policy", mdText, StringComparison.Ordinal);
        Assert.Contains("Warning Presentation", mdText, StringComparison.Ordinal);
        Assert.Contains("Provisional Peak", mdText, StringComparison.Ordinal);
        Assert.Contains("Weekly Volume Curve", mdText, StringComparison.Ordinal);
    }

    [Fact]
    public void PrescriptionModeAndDistanceAccountingMode_AreDistinctInTheFixture()
    {
        var pd = LoadPlanDocument();
        var prescriptionModes = new HashSet<string>();
        var accountingModes = new HashSet<string>();

        foreach (var weekNode in pd["weeks"]!.AsArray())
        {
            foreach (var dayNode in weekNode!["days"]!.AsArray())
            {
                var workout = dayNode!["workout"]?.AsObject();
                if (workout is null)
                {
                    continue;
                }

                if (workout["prescriptionMode"] is { } pm)
                {
                    prescriptionModes.Add(pm.GetValue<string>());
                }

                if (workout["distanceAccountingMode"] is { } am)
                {
                    accountingModes.Add(am.GetValue<string>());
                }
            }
        }

        Assert.Equal(new HashSet<string> { "DISTANCE", "MIXED" }, prescriptionModes);
        Assert.Equal(new HashSet<string> { "EXACT_SESSION_TOTAL", "ESTIMATED_SESSION_TOTAL", "EMBEDDED_COMPONENTS" }, accountingModes);
        Assert.Empty(prescriptionModes.Intersect(accountingModes));
    }
}
