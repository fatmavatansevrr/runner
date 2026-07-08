using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PlanCatalog.Infrastructure.Audit;

/// <summary>Generates golden-fixture-v3-integrity.{json,md} by directly re-computing every check against the live fixture files.</summary>
public static class GoldenFixtureIntegrityReportWriter
{
    private sealed record Check(string Name, string Expected, string Actual, bool Passed);

    public static (string JsonPath, string MarkdownPath) Write(string repoRoot)
    {
        var auditsDir = Path.Combine(repoRoot, "artifacts", "audits");
        Directory.CreateDirectory(auditsDir);

        var fixtureDir = Path.Combine(repoRoot, "docs", "canonical", "golden-fixture-v3");
        var checks = RunChecks(fixtureDir);

        var jsonPath = Path.Combine(auditsDir, "golden-fixture-v3-integrity.json");
        var mdPath = Path.Combine(auditsDir, "golden-fixture-v3-integrity.md");

        File.WriteAllText(jsonPath, BuildJson(checks));
        File.WriteAllText(mdPath, BuildMarkdown(checks));

        return (jsonPath, mdPath);
    }

    private static List<Check> RunChecks(string fixtureDir)
    {
        var checks = new List<Check>();

        string DtPath() => Path.Combine(fixtureDir, "golden-10k-intermediate-4d-12w.v3.decisiontrace.json");
        string PdPath() => Path.Combine(fixtureDir, "golden-10k-intermediate-4d-12w.v3.plandocument.json");
        string MdPath() => Path.Combine(fixtureDir, "golden-10k-intermediate-4d-12w.v3.md");
        string YamlPath() => Path.Combine(fixtureDir, "progression_rules_v2.yaml");

        var filesExist = File.Exists(DtPath()) && File.Exists(PdPath()) && File.Exists(MdPath()) && File.Exists(YamlPath());
        checks.Add(new Check("All four canonical fixture files exist", "true", filesExist.ToString(), filesExist));

        if (!filesExist)
        {
            return checks;
        }

        var dt = JsonNode.Parse(File.ReadAllText(DtPath()))!.AsObject();
        var pd = JsonNode.Parse(File.ReadAllText(PdPath()))!.AsObject();
        var mdText = File.ReadAllText(MdPath());
        var dtText = File.ReadAllText(DtPath());
        var pdText = File.ReadAllText(PdPath());
        var yamlText = File.ReadAllText(YamlPath());

        checks.Add(new Check("DecisionTrace schemaVersion", "3", dt["schemaVersion"]!.GetValue<int>().ToString(), dt["schemaVersion"]!.GetValue<int>() == 3));
        checks.Add(new Check("PlanDocument schemaVersion", "3", pd["schemaVersion"]!.GetValue<int>().ToString(), pd["schemaVersion"]!.GetValue<int>() == 3));
        checks.Add(new Check("PlanDocument fixtureRevision", "3", pd["fixtureRevision"]!.GetValue<int>().ToString(), pd["fixtureRevision"]!.GetValue<int>() == 3));

        var dtKey = dt["fixtureKey"]!.GetValue<string>();
        var pdKey = pd["fixtureKey"]!.GetValue<string>();
        checks.Add(new Check("Both fixtures use fixtureKey GOLDEN_10K_INTERMEDIATE_4D_12W", "GOLDEN_10K_INTERMEDIATE_4D_12W / GOLDEN_10K_INTERMEDIATE_4D_12W",
            $"{dtKey} / {pdKey}", dtKey == "GOLDEN_10K_INTERMEDIATE_4D_12W" && pdKey == "GOLDEN_10K_INTERMEDIATE_4D_12W"));

        var yamlSchemaOk = yamlText.Contains("schemaVersion: 2", StringComparison.Ordinal);
        checks.Add(new Check("progression_rules_v2.yaml schemaVersion", "2", yamlSchemaOk ? "2" : "NOT FOUND", yamlSchemaOk));

        var expectedHash = pd["contentHash"]!.GetValue<string>();
        var computedHash = ComputeCanonicalHash(pd);
        checks.Add(new Check("PlanDocument contentHash verifies", expectedHash, computedHash, string.Equals(expectedHash, computedHash, StringComparison.Ordinal)));

        var weeks = pd["weeks"]!.AsArray();
        var week1 = weeks[0]!.AsObject();
        var week12 = weeks[11]!.AsObject();
        checks.Add(new Check("Week 1 boundary", "2026-08-03 -> 2026-08-09",
            $"{week1["weekStartDate"]!.GetValue<string>()} -> {week1["weekEndDate"]!.GetValue<string>()}",
            week1["weekStartDate"]!.GetValue<string>() == "2026-08-03" && week1["weekEndDate"]!.GetValue<string>() == "2026-08-09"));
        checks.Add(new Check("Week 12 boundary", "2026-10-19 -> 2026-10-25",
            $"{week12["weekStartDate"]!.GetValue<string>()} -> {week12["weekEndDate"]!.GetValue<string>()}",
            week12["weekStartDate"]!.GetValue<string>() == "2026-10-19" && week12["weekEndDate"]!.GetValue<string>() == "2026-10-25"));

        var raceWeekStart = pd["horizon"]!["raceWeekStartDate"]!.GetValue<string>();
        checks.Add(new Check("horizon.raceWeekStartDate", "2026-10-19", raceWeekStart, raceWeekStart == "2026-10-19"));

        var allSevenDaySpans = weeks.Select(w => w!.AsObject())
            .All(w => DateOnly.Parse(w["weekEndDate"]!.GetValue<string>()).DayNumber - DateOnly.Parse(w["weekStartDate"]!.GetValue<string>()).DayNumber == 6);
        checks.Add(new Check("Every week spans seven calendar days, Monday->Sunday", "true", allSevenDaySpans.ToString(), allSevenDaySpans));

        var trainingDistances = week12["days"]!.AsArray()
            .Select(d => d!.AsObject())
            .Where(d => d["dayType"] is null)
            .Select(d => d["workout"]!["plannedDistanceKm"]!.GetValue<decimal>())
            .ToList();
        var taperOk = trainingDistances.SequenceEqual(new[] { 8m, 8m, 4m });
        checks.Add(new Check("Taper training distribution", "8 + 8 + 4 km", string.Join(" + ", trainingDistances) + " km", taperOk));

        var week12Total = week12["totalPlannedDistanceKm"]!.GetValue<decimal>();
        checks.Add(new Check("Week 12 total (training 20 + race 10 = 30 km)", "30",
            week12Total.ToString(System.Globalization.CultureInfo.InvariantCulture), week12Total == 30m));

        var raceDay = week12["days"]!.AsArray().Select(d => d!.AsObject()).Single(d => d["dayType"]?.GetValue<string>() == "RACE");
        var raceLoad = raceDay["workout"]!["loadClassification"]!.GetValue<string>();
        var raceScope = raceDay["workout"]!["stimulusAccountingScope"]!.GetValue<string>();
        checks.Add(new Check("Race day loadClassification", "RACE", raceLoad, raceLoad == "RACE"));
        checks.Add(new Check("Race day stimulusAccountingScope", "RACE_EXCLUDED_FROM_TRAINING_HARD_COUNT", raceScope, raceScope == "RACE_EXCLUDED_FROM_TRAINING_HARD_COUNT"));

        foreach (var (name, needle, haystack) in new[]
        {
            ("PHASE_TRANSITION_DELOAD present", "PHASE_TRANSITION_DELOAD", dtText),
            ("PLANNED_SINGLE_SESSION_SPIKE_CHECK present", "PLANNED_SINGLE_SESSION_SPIKE_CHECK", dtText),
            ("FINAL_VALIDATION present", "FINAL_VALIDATION", dtText),
            ("WARNING_POLICY_V2 present", "WARNING_POLICY_V2", dtText),
            ("ESTIMATED_THRESHOLD_EFFORT present", "ESTIMATED_THRESHOLD_EFFORT", pdText),
        })
        {
            var found = haystack.Contains(needle, StringComparison.Ordinal);
            checks.Add(new Check(name, "present", found ? "present" : "MISSING", found));
        }

        var obsoleteAbsentFromJson = !dtText.Contains("CURRENT_LACTATE_THRESHOLD_EFFORT", StringComparison.Ordinal) &&
                                      !pdText.Contains("CURRENT_LACTATE_THRESHOLD_EFFORT", StringComparison.Ordinal);
        checks.Add(new Check("Obsolete CURRENT_LACTATE_THRESHOLD_EFFORT absent from JSON fixtures", "0 occurrences",
            obsoleteAbsentFromJson ? "0 occurrences" : "FOUND", obsoleteAbsentFromJson));

        foreach (var phrase in new[] { "Warning Policy", "Warning Presentation", "Provisional Peak", "Weekly Volume Curve" })
        {
            var found = mdText.Contains(phrase, StringComparison.Ordinal);
            checks.Add(new Check($"'{phrase}' pipeline stage documented", "present", found ? "present" : "MISSING", found));
        }

        var prescriptionModes = new HashSet<string>();
        var accountingModes = new HashSet<string>();
        foreach (var weekNode in weeks)
        {
            foreach (var dayNode in weekNode!["days"]!.AsArray())
            {
                var workout = dayNode!["workout"]?.AsObject();
                if (workout is null) continue;
                if (workout["prescriptionMode"] is { } pm) prescriptionModes.Add(pm.GetValue<string>());
                if (workout["distanceAccountingMode"] is { } am) accountingModes.Add(am.GetValue<string>());
            }
        }

        var noOverlap = !prescriptionModes.Overlaps(accountingModes);
        checks.Add(new Check("PrescriptionMode and DistanceAccountingMode vocabularies do not overlap",
            "no shared values", noOverlap ? "no shared values" : "OVERLAP FOUND", noOverlap));

        return checks;
    }

    private static string ComputeCanonicalHash(JsonObject pd)
    {
        JsonNode? Canon(JsonNode? n)
        {
            if (n is JsonArray arr)
            {
                var copy = new JsonArray();
                foreach (var item in arr) copy.Add(Canon(item?.DeepClone()));
                return copy;
            }

            if (n is JsonObject obj)
            {
                var keys = obj.Select(kv => kv.Key).Where(k => k != "contentHash" && obj[k] is not null).OrderBy(k => k, StringComparer.Ordinal).ToList();
                var copy = new JsonObject();
                foreach (var k in keys) copy[k] = Canon(obj[k]?.DeepClone());
                return copy;
            }

            return n?.DeepClone();
        }

        var canonical = Canon(pd);
        var options = new JsonSerializerOptions { WriteIndented = false, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var json = canonical!.ToJsonString(options).Normalize(NormalizationForm.FormC);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private static string BuildJson(List<Check> checks)
    {
        var document = new
        {
            generatedAtUtc = DateTime.UtcNow.ToString("O"),
            overallStatus = checks.All(c => c.Passed) ? "PASSED" : "FAILED",
            checks = checks.Select(c => new { check = c.Name, expected = c.Expected, actual = c.Actual, status = c.Passed ? "PASS" : "FAIL" })
        };

        return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string BuildMarkdown(List<Check> checks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Golden Fixture v3 — Integrity Verification");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.UtcNow:O}");
        sb.AppendLine();
        sb.AppendLine($"**Overall status: {(checks.All(c => c.Passed) ? "PASSED" : "FAILED")}**");
        sb.AppendLine();
        sb.AppendLine("| Check | Expected | Actual | Status |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var c in checks)
        {
            sb.AppendLine($"| {c.Name} | {c.Expected} | {c.Actual} | {(c.Passed ? "PASS" : "FAIL")} |");
        }

        return sb.ToString();
    }
}
