using System.Text.Json;
using PlanCatalog.Contracts.Bundles;
using PlanCatalog.Core.Audit;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// Activation safety clarification (post Wave 8 / D13) — see artifacts/audits/activation-safety-clarification.md.
///
/// Guards against the false inference "zero domain blockers implies publish/activation readiness."
/// activation-readiness-risks.json is DOCUMENTATION_ONLY: nothing in this codebase reads it. These tests
/// keep that fact and the open-risk count visible as a permanent regression guard, so that if a future
/// change silently makes the blocker count zero while risks remain unaddressed, or silently wires in
/// mechanical enforcement without updating this test, the discrepancy surfaces here.
/// </summary>
public sealed class ActivationSafetyGateTests
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

    private static PublishedTemplateBundle BuildBundle(int version)
    {
        var snapshot = new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog")).LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        return new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher()).Assemble(stamped, "TEN_K__4D__INTERMEDIATE", version);
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

    private static BlockerScopeMeasurement.ArtifactIdentity Id(PlanCatalog.Contracts.References.CatalogArtifactReference reference) =>
        new(reference.DocumentType, reference.Key, reference.Version);

    [Fact]
    public void V10Candidate_HasZeroDomainBlockers()
    {
        Assert.Equal(0, BlockerScopeMeasurement.ScopedDecisionCount(Closure(BuildBundle(10))));
    }

    [Fact]
    public void ActivationReadinessRisksFile_StillHasAtLeastOneOpenRisk()
    {
        // Zero domain blockers (proven above) must NOT be read as "no open risk remains" — this file is
        // the source of truth for risks that are NOT domain-content decisions and are NOT tracked by
        // BlockerScopeMeasurement at all.
        var path = Path.Combine(RepoRoot(), "artifacts", "audits", "activation-readiness-risks.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var risks = doc.RootElement.GetProperty("risks");

        var openCount = risks.EnumerateArray().Count(r => r.GetProperty("status").GetString() == "OPEN");
        Assert.True(openCount >= 1, "Expected at least one OPEN activation risk to remain visible after D13.");
    }

    [Fact]
    public void ActivationReadinessRisksFile_IsNotMechanicallyConsumedByAnySourceFile()
    {
        // Confirms the DOCUMENTATION_ONLY classification in activation-safety-clarification.md remains
        // true. If this ever starts failing, a mechanical gate has been introduced and the
        // classification/report should be revisited (not silently left stale).
        var srcFiles = Directory.GetFiles(Path.Combine(RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories);

        Assert.All(srcFiles, f =>
        {
            var content = File.ReadAllText(f);
            Assert.DoesNotContain("activation-readiness-risks", content, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void PublishReadinessValidator_HasNoActivationRiskParameter()
    {
        // Documents, as an executable fact, that the current publish gate signature has no extension
        // point for external risk-note data -- supporting the "no minimal safe extension point exists"
        // reasoning recorded in activation-safety-clarification.json for why a full gate was not added.
        var method = typeof(PlanCatalog.Core.Validation.PublishReadinessValidator)
            .GetMethod(nameof(PlanCatalog.Core.Validation.PublishReadinessValidator.Validate));

        Assert.NotNull(method);
        var parameterTypeNames = method!.GetParameters().Select(p => p.ParameterType.Name).ToList();
        Assert.DoesNotContain(parameterTypeNames, n => n.Contains("Risk", StringComparison.OrdinalIgnoreCase));
    }
}
