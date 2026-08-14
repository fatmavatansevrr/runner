using System.Text.Json;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

public sealed class LongHorizonFullDarkLifecycleValidationGovernanceTests
{
    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PHASE4K_9_FULL_21_TO_52_ROLLING_DARK_LIFECYCLE_VALIDATION_RETRY_AND_BOUNDARY_MATRIX.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    private static JsonElement Record()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Root(), "plan-catalog", "artifacts", "audits", "activation-readiness-risks.json")));
        return document.RootElement.GetProperty("risks").EnumerateArray().Single(r =>
            r.GetProperty("id").GetString() == "TD-LONG-HORIZON-FULL-DARK-LIFECYCLE-VALIDATION-001").Clone();
    }

    [Fact]
    public void GovernanceRecordContainsEveryRequiredFieldAndIsClosed()
    {
        var record = Record();
        foreach (var field in new[]
        {
            "status", "harnessEntryPoint", "scenarioContract", "lifecycleState", "routing", "horizonMatrix",
            "profileMatrix", "loadMatrix", "paceMatrix", "growthValidation", "maintenanceValidation",
            "blockedValidation", "retryTransition", "runwayValidation", "coreValidation", "boundaryValidation",
            "calendarValidation", "finalCompletionValidation", "atomicityMatrix", "determinismReplay", "loopSafety",
            "auditTrace", "liveIntegrationStatus", "tests",
        })
            Assert.True(record.TryGetProperty(field, out _), $"Missing required governance field {field}.");
        Assert.Equal("CLOSED", record.GetProperty("status").GetString());
    }

    [Fact]
    public void AggregateAndJsonMarkdownParityAreCurrent()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Root(), "plan-catalog", "artifacts", "audits", "activation-readiness-risks.json")));
        var risks = document.RootElement.GetProperty("risks").EnumerateArray().ToArray();
        Assert.NotEmpty(risks);
        Assert.Equal(
            risks.Length,
            risks.Count(r => r.GetProperty("status").GetString() == "OPEN")
            + risks.Count(r => r.GetProperty("status").GetString() == "CLOSED"));
        Assert.Equal(risks.Length, risks.Select(r => r.GetProperty("id").GetString()).Distinct().Count());
        var markdown = File.ReadAllText(Path.Combine(Root(), "plan-catalog", "artifacts", "audits", "activation-readiness-risks.md"));
        Assert.Contains("TD-LONG-HORIZON-FULL-DARK-LIFECYCLE-VALIDATION-001", markdown);
    }

    [Fact]
    public void RequiredAppendOnlyRecordsCarryPhase4K9UpdateAndRedesignRemainsOpen()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Root(), "plan-catalog", "artifacts", "audits", "activation-readiness-risks.json")));
        var risks = document.RootElement.GetProperty("risks").EnumerateArray().ToDictionary(r => r.GetProperty("id").GetString()!, r => r);
        foreach (var id in new[]
        {
            "TD-LONG-HORIZON-RUNWAY-CORE-JIT-RUNTIME-001",
            "TD-LONG-HORIZON-JIT-REAL-CORE-CONDITION-CALENDAR-COMPOSITION-001",
            "TD-LONG-HORIZON-ACTIVATED-SESSION-CALENDAR-PROJECTION-001",
        })
            Assert.True(risks[id].TryGetProperty("phase4K9Update", out _), $"{id} lacks phase4K9Update.");
        var redesign = risks["TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001"];
        Assert.Equal("OPEN", redesign.GetProperty("status").GetString());
        Assert.Contains(redesign.GetProperty("requiredResolution").EnumerateArray(), item =>
            item.GetString()!.Contains("UPDATE (Phase 4K.9)", StringComparison.Ordinal));
    }

    [Fact]
    public void PhaseDocumentHasExactlyThirtySixRequiredSectionsAndSuccessMarkers()
    {
        var text = File.ReadAllText(Path.Combine(Root(),
            "PHASE4K_9_FULL_21_TO_52_ROLLING_DARK_LIFECYCLE_VALIDATION_RETRY_AND_BOUNDARY_MATRIX.md"));
        Assert.Equal(36, text.Split('\n').Count(line => line.StartsWith("## ", StringComparison.Ordinal)));
        Assert.Contains("LONG_HORIZON_FULL_21_TO_52_ROLLING_DARK_LIFECYCLE_VALIDATION_COMPLETED", text);
        Assert.Contains("Phase 4L.1", text);
    }

    [Fact]
    public void HarnessSourcesAreInternalAndContainNoLiveRegistrationOrPersistenceWrite()
    {
        var directory = Path.Combine(Root(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule",
            "LongHorizon", "RollingActivation", "LifecycleValidation");
        var text = string.Join('\n', Directory.GetFiles(directory, "*.cs").Select(File.ReadAllText));
        Assert.DoesNotContain("public sealed class LongHorizonFullDarkLifecycleHarness", text);
        Assert.DoesNotContain("AddScoped<ILongHorizonFullDarkLifecycleHarness", text);
        Assert.DoesNotContain("SaveChanges", text);
        Assert.DoesNotContain("DateTime.UtcNow", text);
        Assert.DoesNotContain("DateTime.Now", text);
    }

    [Fact]
    public void HarnessInvokesExistingRuntimeInterfacesAndDoesNotDeclareNumericOrCalendarPolicy()
    {
        var text = File.ReadAllText(Path.Combine(Root(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule",
            "LongHorizon", "RollingActivation", "LifecycleValidation", "LongHorizonFullDarkLifecycleHarness.cs"));
        Assert.Contains("ILongHorizonRollingInitialActivationRuntime", text);
        Assert.Contains("ILongHorizonRollingCheckpointRuntime", text);
        Assert.Contains("ILongHorizonRollingJitCompositionOrchestrator", text);
        Assert.DoesNotContain("LongRunSelectionShare", text);
        Assert.DoesNotContain("RecoveryVolumeRatio", text);
        Assert.DoesNotContain("WeekStartDate(", text);
        Assert.DoesNotContain("AssignedDate(", text);
    }
}
