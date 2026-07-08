using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Audit;
using PlanCatalog.Core.Ports;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Schema;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>
/// Establishes and locks down the canonical Production-readiness error contract against the real,
/// post-retirement active root (TEN_K__4D__INTERMEDIATE v4) — see
/// artifacts/audits/production-readiness-error-contract-audit.md. Contract: one
/// <see cref="ContentDecisionGuardError"/> per distinct blocking artifact identity
/// (DocumentType, Key, Version); every one of that artifact's blocking field-level
/// <see cref="DomainContentDecision"/> entries is carried, structured, in
/// <see cref="ContentDecisionGuardError.BlockingDecisions"/>. Artifact-level and decision-level counts
/// are exposed separately and must never be compared to each other.
/// </summary>
public sealed class ProductionReadinessErrorContractTests : IDisposable
{
    private readonly string _artifactsRoot = Path.Combine(Path.GetTempPath(), "plan-catalog-tests", Guid.NewGuid().ToString("N"));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private static IRetirementLedger RealRetirementLedger()
    {
        var path = Path.Combine(RepoRoot(), "artifacts", "appsel-plan-catalog", "retirements.json");
        Assert.True(File.Exists(path));
        return new FileSystemRetirementLedger(path);
    }

    private CatalogPublisher CreatePublisher(out FileSystemPublishedArtifactRepository publishedRepository)
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        var sourceRepository = new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog"));
        var schemaValidator = new JsonSchemaNetValidator(Path.Combine(RepoRoot(), "schemas"));
        var bundleAssembler = new CatalogBundleAssembler(serializer, hasher);
        publishedRepository = new FileSystemPublishedArtifactRepository(_artifactsRoot);

        return new CatalogPublisher(sourceRepository, schemaValidator, serializer, hasher, bundleAssembler, publishedRepository, RealRetirementLedger());
    }

    private static ContentDecisionGuardResult PublishAgainstActiveRootAndCaptureDetail(CatalogPublisher publisher, string releaseVersion)
    {
        var ex = Assert.Throws<CatalogValidationException>(() =>
            publisher.Publish(releaseVersion, ReleaseChannel.Production, allowUnconfirmedContent: true));
        Assert.Equal("Content decision guard", ex.Stage);
        Assert.NotNull(ex.ContentDecisionDetail);
        return ex.ContentDecisionDetail!;
    }

    [Fact]
    public void ActiveRootClosure_ContainsExactlyThirteenBlockingDecisions()
    {
        var detail = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-1");
        Assert.Equal(13, detail.BlockingDecisionCount);
    }

    [Fact]
    public void ActiveRootClosure_ContainsExactlyNineBlockingArtifacts()
    {
        var detail = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-2");
        Assert.Equal(9, detail.BlockingArtifactCount);
    }

    [Fact]
    public void NoBlockerBelongsOnlyToARetiredRoot()
    {
        // TEN_K__4D__INTERMEDIATE v1/v2/v3 are retired; the only combination reachable from a Production
        // publish attempt against the real ledger is v4. Every blocking artifact identity returned must
        // therefore be reachable from v4's own dependency closure, never from a retired root's closure only.
        var detail = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-3");
        var expectedActiveRootArtifacts = new HashSet<(string, string, int)>
        {
            ("RUN_LAYOUT", "RUN_LAYOUT_4D", 1),
            ("PROGRESSION_MODIFIER", "INTERMEDIATE_PROGRESSION_MODIFIER_V1", 1),
            ("RUNTIME_CONDITION_VALUE_REGISTRY", "RUNTIME_CONDITION_VALUES_V1", 1),
            ("PEAK_VOLUME_BAND_POLICY", "PEAK_VOLUME_BANDS_V1", 2),
            ("WORKOUT_DEFINITION", "EASY_STANDARD", 2),
            ("WORKOUT_DEFINITION", "FARTLEK", 2),
            ("WORKOUT_DEFINITION", "GOAL_PACE_TEN_K", 1),
            ("WORKOUT_DEFINITION", "LONG_RUN_STANDARD", 2),
            ("WORKOUT_DEFINITION", "THRESHOLD_TEMPO", 2),
        };

        var actual = detail.Errors.Select(e => (e.DocumentType, e.Key, e.Version)).ToHashSet();
        Assert.Equal(expectedActiveRootArtifacts, actual);
    }

    [Fact]
    public void ProductionPublish_FailsWithPublishProductionContainsUnconfirmedContent()
    {
        var detail = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-4");
        Assert.All(detail.Errors, e => Assert.Equal("PUBLISH_PRODUCTION_CONTAINS_UNCONFIRMED_CONTENT", e.ErrorCode));
        Assert.All(detail.Errors, e => Assert.Equal(ValidationSeverity.Error, e.Severity));
    }

    [Fact]
    public void ErrorContract_IsDeterministicAcrossRepeatedCalls()
    {
        var first = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-5a");
        var second = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-5b");

        Assert.Equal(first.BlockingArtifactCount, second.BlockingArtifactCount);
        Assert.Equal(first.BlockingDecisionCount, second.BlockingDecisionCount);
        Assert.Equal(first.Errors.Count, second.Errors.Count);

        var firstEntryIds = first.Errors.SelectMany(e => e.BlockingDecisions.Select(d => d.EntryId)).OrderBy(x => x).ToList();
        var secondEntryIds = second.Errors.SelectMany(e => e.BlockingDecisions.Select(d => d.EntryId)).OrderBy(x => x).ToList();
        Assert.Equal(firstEntryIds, secondEntryIds);
    }

    [Fact]
    public void EveryOneOfTheThirteenDecisions_AppearsExactlyOnce()
    {
        var detail = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-6");

        var entryIds = detail.Errors.SelectMany(e => e.BlockingDecisions.Select(d => d.EntryId)).ToList();
        Assert.Equal(13, entryIds.Count);
        Assert.Equal(entryIds.Count, entryIds.Distinct().Count());
    }

    [Fact]
    public void EveryDecision_MapsToTheCorrectOwningArtifact()
    {
        var detail = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-7");

        foreach (var error in detail.Errors)
        {
            foreach (var decision in error.BlockingDecisions)
            {
                var sourceEntry = PilotDomainContentAudit.Entries.Single(e => e.EntryId == decision.EntryId);
                Assert.Equal(error.DocumentType, sourceEntry.DocumentType);
                Assert.Equal(error.Key, sourceEntry.Key);
                Assert.Equal(error.Version, sourceEntry.Version);
            }
        }
    }

    [Fact]
    public void NoBlockingDecision_IsLostThroughArtifactGrouping()
    {
        var detail = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-8");

        var scope = detail.Errors.Select(e => new BlockerScopeMeasurement.ArtifactIdentity(e.DocumentType, e.Key, e.Version)).ToList();
        var expectedFromIndependentMeasurement = BlockerScopeMeasurement.ScopedDecisionCount(scope);

        Assert.Equal(expectedFromIndependentMeasurement, detail.BlockingDecisionCount);
        Assert.Equal(expectedFromIndependentMeasurement, detail.Errors.Sum(e => e.BlockingDecisions.Count));
    }

    [Fact]
    public void TopLevelErrorCount_MatchesBlockingArtifactCount_NotBlockingDecisionCount()
    {
        // This is the exact question this task set out to answer: one Production error today represents
        // one blocking ARTIFACT identity, not one field-level decision.
        var detail = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-9");

        Assert.Equal(9, detail.Errors.Count);
        Assert.Equal(detail.BlockingArtifactCount, detail.Errors.Count);
        Assert.NotEqual(detail.BlockingDecisionCount, detail.Errors.Count);
    }

    [Fact]
    public void BlockingDecisionCount_EqualsThirteen()
    {
        var detail = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-10");
        Assert.Equal(13, detail.BlockingDecisionCount);
    }

    [Fact]
    public void BlockingArtifactCount_EqualsNine()
    {
        var detail = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-11");
        Assert.Equal(9, detail.BlockingArtifactCount);
    }

    [Fact]
    public void BackwardCompatibleValidationResult_HasOneIssuePerArtifact_MatchingDetailedErrors()
    {
        var detail = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-12");
        var asValidationResult = detail.ToValidationResult();

        Assert.Equal(detail.Errors.Count, asValidationResult.Issues.Count);
        Assert.All(asValidationResult.Issues, i => Assert.Equal("PUBLISH_PRODUCTION_CONTAINS_UNCONFIRMED_CONTENT", i.Code));
    }

    [Fact]
    public void MachineReadableOutput_IncludesStructuredBlockingDecisions_NotOnlyAConcatenatedMessage()
    {
        var detail = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-13");

        foreach (var error in detail.Errors)
        {
            Assert.NotEmpty(error.BlockingDecisions);
            foreach (var decision in error.BlockingDecisions)
            {
                Assert.False(string.IsNullOrWhiteSpace(decision.EntryId));
                Assert.False(string.IsNullOrWhiteSpace(decision.FieldPath));
                Assert.False(string.IsNullOrWhiteSpace(decision.Classification));
                Assert.False(string.IsNullOrWhiteSpace(decision.Reason));
            }
        }
    }

    [Fact]
    public void ProductionPublish_WritesNoPartialRelease()
    {
        var publisher = CreatePublisher(out var repository);
        Assert.Throws<CatalogValidationException>(() =>
            publisher.Publish("contract-test-14", ReleaseChannel.Production, allowUnconfirmedContent: true));

        Assert.False(repository.ReleaseExists("contract-test-14"));
    }

    [Fact]
    public void FullCatalogEligibleUnionCounts_EqualActiveRootCounts_WhileV4IsTheOnlyEligibleRoot()
    {
        // With v1/v2/v3 retired, the eligible-release-union scope collapses to exactly v4's closure, so
        // the eligible-union decision/artifact counts must equal the active-root counts — see
        // artifacts/audits/placeholder-scope-audit.md.
        var detail = PublishAgainstActiveRootAndCaptureDetail(CreatePublisher(out _), "contract-test-15");
        var activeRootScope = detail.Errors.Select(e => new BlockerScopeMeasurement.ArtifactIdentity(e.DocumentType, e.Key, e.Version)).ToList();

        Assert.Equal(BlockerScopeMeasurement.ScopedDecisionCount(activeRootScope), detail.BlockingDecisionCount);
        Assert.Equal(BlockerScopeMeasurement.ScopedArtifactCount(activeRootScope), detail.BlockingArtifactCount);
    }

    public void Dispose()
    {
        if (Directory.Exists(_artifactsRoot))
        {
            Directory.Delete(_artifactsRoot, recursive: true);
        }
    }
}
