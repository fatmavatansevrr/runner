using PlanCatalog.Core.Audit;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Metadata;
using CoreEnums = PlanCatalog.Core.Enums;
using ContractsEnums = PlanCatalog.Contracts.Enums;

namespace PlanCatalog.Core.Validation;

/// <summary>Final publish gate — see brief §12.11. Schema/domain/graph validation must already have run.</summary>
public static class PublishReadinessValidator
{
    public static ValidationResult Validate(
        CatalogSourceSnapshot snapshot,
        ValidationResult schemaValidation,
        ValidationResult domainAndGraphValidation)
    {
        var issues = new List<ValidationIssue>();

        if (!schemaValidation.IsValid)
        {
            issues.Add(new ValidationIssue("PUBLISH_SCHEMA_VALIDATION_FAILED", ValidationSeverity.Error,
                "Schema validation must pass before publish readiness can be assessed.", "$"));
        }

        if (!domainAndGraphValidation.IsValid)
        {
            issues.Add(new ValidationIssue("PUBLISH_DOMAIN_VALIDATION_FAILED", ValidationSeverity.Error,
                "Domain/graph validation must pass before publish readiness can be assessed.", "$"));
        }

        foreach (var metadata in AllMetadata(snapshot))
        {
            if (metadata.Status != CoreEnums.CatalogStatus.Draft && string.IsNullOrEmpty(metadata.ContentHash))
            {
                issues.Add(new ValidationIssue("PUBLISH_CONTENT_HASH_MISSING", ValidationSeverity.Error,
                    $"'{metadata.DocumentType}/{metadata.Key}' v{metadata.Version} has status {metadata.Status} but no ContentHash.", "$.metadata.contentHash"));
            }
        }

        var hashCollisions = AllMetadata(snapshot)
            .GroupBy(m => (m.DocumentType, m.Key, m.Version))
            .Where(g => g.Select(m => m.ContentHash).Distinct().Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var (documentType, key, version) in hashCollisions)
        {
            issues.Add(new ValidationIssue("PUBLISH_HASH_MISMATCH_FOR_SAME_KEY_VERSION", ValidationSeverity.Error,
                $"'{documentType}/{key}' v{version} has multiple differing content hashes.", "$.metadata.contentHash"));
        }

        return new ValidationResult(issues);
    }

    /// <summary>
    /// Enforces the release-channel content-decision guard: production never accepts placeholder
    /// content; a pilot/draft channel may include it only with an explicit opt-in, which downgrades
    /// the finding to a warning that the caller should record in the release manifest.
    ///
    /// Backward-compatible projection of <see cref="ValidateContentDecisionsDetailed"/> onto the
    /// generic <see cref="ValidationResult"/> shape — see that method for the canonical, structured
    /// contract (one error per blocking artifact identity, with every blocking field-level decision
    /// preserved in structured form, never only inside a concatenated message string). See
    /// artifacts/audits/production-readiness-error-contract-audit.md for the full contract rationale.
    /// </summary>
    public static ValidationResult ValidateContentDecisions(
        IEnumerable<(string DocumentType, string Key, int Version)> bundleArtifacts,
        ContractsEnums.ReleaseChannel channel,
        bool allowUnconfirmedContent) =>
        ValidateContentDecisionsDetailed(bundleArtifacts, channel, allowUnconfirmedContent).ToValidationResult();

    /// <summary>
    /// Canonical, structured form of the content-decision guard. Grouping key is the artifact identity
    /// (DocumentType, Key, Version): one <see cref="ContentDecisionGuardError"/> is emitted per distinct
    /// blocking artifact, and every one of that artifact's blocking <see cref="DomainContentDecision"/>
    /// entries is carried in <see cref="ContentDecisionGuardError.BlockingDecisions"/> — no decision is
    /// dropped, merged invisibly, or double-counted. <see cref="ContentDecisionGuardResult.BlockingArtifactCount"/>
    /// and <see cref="ContentDecisionGuardResult.BlockingDecisionCount"/> are catalog-content properties
    /// of the supplied bundle closure, independent of channel/flag.
    /// </summary>
    public static ContentDecisionGuardResult ValidateContentDecisionsDetailed(
        IEnumerable<(string DocumentType, string Key, int Version)> bundleArtifacts,
        ContractsEnums.ReleaseChannel channel,
        bool allowUnconfirmedContent)
    {
        var errors = new List<ContentDecisionGuardError>();
        var blockingArtifactIdentities = new HashSet<(string, string, int)>();
        var blockingDecisionCount = 0;

        foreach (var (documentType, key, version) in bundleArtifacts.Distinct())
        {
            var blocking = PilotDomainContentAudit.BlockingEntriesFor(documentType, key, version);
            if (blocking.Count == 0)
            {
                continue;
            }

            blockingArtifactIdentities.Add((documentType, key, version));
            blockingDecisionCount += blocking.Count;

            var decisions = blocking
                .Select(b => new ProductionReadinessBlockingDecision(b.EntryId, b.JsonPath, b.Classification.ToString(), b.SourceSectionOrReason))
                .ToList();

            var (code, severity) = channel == ContractsEnums.ReleaseChannel.Production
                ? ("PUBLISH_PRODUCTION_CONTAINS_UNCONFIRMED_CONTENT", ValidationSeverity.Error)
                : !allowUnconfirmedContent
                    ? ("PUBLISH_UNCONFIRMED_CONTENT_REQUIRES_EXPLICIT_FLAG", ValidationSeverity.Error)
                    : ("PUBLISH_UNCONFIRMED_CONTENT_ACKNOWLEDGED", ValidationSeverity.Warning);

            errors.Add(new ContentDecisionGuardError(code, severity, documentType, key, version, decisions));
        }

        return new ContentDecisionGuardResult(blockingArtifactIdentities.Count, blockingDecisionCount, errors);
    }

    private static IEnumerable<CatalogDocumentMetadata> AllMetadata(CatalogSourceSnapshot snapshot) =>
        snapshot.PlanTemplates.Select(x => x.Metadata)
            .Concat(snapshot.RunLayouts.Select(x => x.Metadata))
            .Concat(snapshot.LevelModifiers.Select(x => x.Metadata))
            .Concat(snapshot.WorkoutProgressions.Select(x => x.Metadata))
            .Concat(snapshot.ProgressionModifiers.Select(x => x.Metadata))
            .Concat(snapshot.Workouts.Select(x => x.Metadata))
            .Concat(snapshot.RuntimeConditionValueRegistries.Select(x => x.Metadata))
            .Concat(snapshot.PeakVolumeBandPolicies.Select(x => x.Metadata))
            .Concat(snapshot.RulePacks.Select(x => x.Metadata))
            .Concat(snapshot.Combinations.Select(x => x.Metadata));
}
