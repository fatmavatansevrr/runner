using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

public sealed class LongHorizonActiveReadModelWorkoutMutationGovernanceTests
{
    [Fact]
    public void RegistryMarkdownAndPhaseDocument_AreCurrentUniqueAndComplete()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var risks = document.RootElement.GetProperty("risks").EnumerateArray().ToArray();
        Assert.NotEmpty(risks);
        Assert.Equal(risks.Length, risks.Select(r => r.GetProperty("id").GetString()).Distinct().Count());
        Assert.Equal(
            risks.Length,
            risks.Count(r => r.GetProperty("status").GetString() == "OPEN")
            + risks.Count(r => r.GetProperty("status").GetString() == "CLOSED"));

        var td = Assert.Single(risks, r => r.GetProperty("id").GetString() == "TD-LONG-HORIZON-ACTIVE-READ-MODEL-WORKOUT-MUTATION-001");
        Assert.Equal("CLOSED", td.GetProperty("status").GetString());
        foreach (var field in new[] { "readArchitecture", "publicSessionIdentity", "homeContract", "calendarContract", "detailContract",
            "outcomeModel", "completionTransaction", "completionIdempotency", "completionConcurrency", "notTodayPolicy", "weekTerminality",
            "checkpointEvidence", "activationTriggerPolicy", "flutterReadiness", "migration", "authorization", "leakage", "tests" })
            Assert.True(td.TryGetProperty(field, out _), field);

        var markdown = File.ReadAllText(MarkdownPath());
        Assert.Contains("TD-LONG-HORIZON-ACTIVE-READ-MODEL-WORKOUT-MUTATION-001", markdown);

        var phase = File.ReadAllText(PhasePath());
        Assert.Equal(43, Regex.Matches(phase, @"(?m)^## \d+\. ").Count);
        Assert.Contains("Flutter is unchanged", phase);
        Assert.Contains("No hosted service", phase);
        Assert.Contains("20260805081427_Phase4L4RollingSessionOutcomes", phase);
    }

    private static string Root() => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../"));
    private static string JsonPath() => Path.Combine(Root(), "plan-catalog/artifacts/audits/activation-readiness-risks.json");
    private static string MarkdownPath() => Path.Combine(Root(), "plan-catalog/artifacts/audits/activation-readiness-risks.md");
    private static string PhasePath() => Path.Combine(Root(), "PHASE4L_4_LONG_HORIZON_ACTIVE_READ_MODEL_WORKOUT_MUTATION_AND_FLUTTER_CONTRACT_READINESS.md");
}
