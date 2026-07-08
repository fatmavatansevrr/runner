using PlanCatalog.Contracts;
using PlanCatalog.Core.Ports;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>
/// Milestone G: active-version policy preparation only — see
/// artifacts/audits/part3-retirement-and-release-plan.md. Tests 37-39 of the Part 2 required-tests list.
/// <see cref="ActiveVersionUniquenessValidator"/> is exercised only against isolated fixtures here; it is
/// NOT wired into CatalogPublisher and does not run against the real catalog automatically in Part 2.
/// </summary>
public sealed class ActiveVersionPreparationTests
{
    private sealed class FakeRetirementLedger(params (string DocumentType, string Key, int Version)[] retired) : IRetirementLedger
    {
        public bool IsRetired(string documentType, string key, int version) =>
            retired.Contains((documentType, key, version));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    [Fact]
    public void TwoActiveVersionsOfOneKey_FailInIsolatedPolicyValidation()
    {
        // Test 37.
        var fixture = new CombinationFixture();
        var secondVersion = fixture.Combination with { Metadata = fixture.Combination.Metadata with { Version = 2 } };

        var result = ActiveVersionUniquenessValidator.Validate([fixture.Combination, secondVersion]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "ACTIVE_COMBINATION_VERSION_NOT_UNIQUE");
    }

    [Fact]
    public void OneActivePlusRetiredHistoricalVersions_Passes()
    {
        // Test 38.
        var fixture = new CombinationFixture();
        var historical1 = fixture.Combination with { Metadata = fixture.Combination.Metadata with { Version = 1 } };
        var historical2 = fixture.Combination with { Metadata = fixture.Combination.Metadata with { Version = 2 } };
        var active = fixture.Combination with { Metadata = fixture.Combination.Metadata with { Version = 3 } };

        var ledger = new FakeRetirementLedger(
            (DocumentTypes.TemplateCombination, fixture.Combination.Metadata.Key, 1),
            (DocumentTypes.TemplateCombination, fixture.Combination.Metadata.Key, 2));

        var result = ActiveVersionUniquenessValidator.Validate([historical1, historical2, active], ledger);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RealCatalogRetirementPlan_HasBeenExecuted_ExactlyOneEligibleVersionRemains()
    {
        // Test 39, updated for Part 3: artifacts/audits/part3-retirement-and-release-plan.json's plan has
        // now been executed — TEN_K__4D__INTERMEDIATE v1/v2/v3 are retired via the real retirement ledger,
        // leaving exactly v4 as the sole non-retired, publish-eligible version. See
        // artifacts/audits/retirement-ledger-application.md for the full before/after record.
        var repo = new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog"));
        var snapshot = repo.LoadSnapshot();
        var realRetirementLedgerPath = Path.Combine(RepoRoot(), "artifacts", "appsel-plan-catalog", "retirements.json");
        Assert.True(File.Exists(realRetirementLedgerPath));

        var realLedger = new FileSystemRetirementLedger(realRetirementLedgerPath);
        var result = ActiveVersionUniquenessValidator.Validate(snapshot.Combinations, realLedger);

        Assert.True(result.IsValid);
        Assert.True(realLedger.IsRetired(DocumentTypes.TemplateCombination, "TEN_K__4D__INTERMEDIATE", 1));
        Assert.True(realLedger.IsRetired(DocumentTypes.TemplateCombination, "TEN_K__4D__INTERMEDIATE", 2));
        Assert.True(realLedger.IsRetired(DocumentTypes.TemplateCombination, "TEN_K__4D__INTERMEDIATE", 3));
        Assert.False(realLedger.IsRetired(DocumentTypes.TemplateCombination, "TEN_K__4D__INTERMEDIATE", 4));
    }
}
