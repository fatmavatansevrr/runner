using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.Manifests;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Ports;
using PlanCatalog.Core.Validation;

namespace PlanCatalog.Infrastructure.Publishing;

/// <summary>
/// Orchestrates the atomic DRAFT→VALIDATED→PUBLISHED workflow — see brief §16.2. A failed publish
/// never leaves a partial release: everything is staged before <see cref="IPublishedArtifactRepository.WriteRelease"/>
/// performs the atomic move. Also enforces the release-channel content-decision guard and retirement
/// resolution: a NEW build may never resolve a RETIRED dependency, and production never accepts
/// PLACEHOLDER_UNCONFIRMED domain content.
/// </summary>
public sealed class CatalogPublisher(
    ICatalogSourceRepository sourceRepository,
    IJsonSchemaValidator schemaValidator,
    ICanonicalJsonSerializer serializer,
    IContentHasher hasher,
    ICatalogBundleAssembler bundleAssembler,
    IPublishedArtifactRepository publishedRepository,
    IRetirementLedger retirementLedger,
    ICrossReleaseHashExceptionRegistry? crossReleaseHashExceptionRegistry = null) : ICatalogPublisher
{
    private readonly ICrossReleaseHashExceptionRegistry _crossReleaseHashExceptionRegistry =
        crossReleaseHashExceptionRegistry ?? NullCrossReleaseHashExceptionRegistry.Instance;

    public CatalogReleaseManifest Publish(string releaseVersion, ReleaseChannel channel, bool allowUnconfirmedContent = false)
    {
        if (publishedRepository.ReleaseExists(releaseVersion))
        {
            throw new InvalidOperationException($"Release '{releaseVersion}' already exists and is immutable.");
        }

        var (manifest, files) = BuildRelease(releaseVersion, channel, allowUnconfirmedContent);
        publishedRepository.WriteRelease(releaseVersion, manifest, files);
        return manifest;
    }

    /// <summary>Runs the full build+validate pipeline without writing to <c>artifacts/</c> — used for CLI dry-run/preview.</summary>
    public CatalogReleaseManifest BuildPreview(string releaseVersion, ReleaseChannel channel, bool allowUnconfirmedContent = false) =>
        BuildRelease(releaseVersion, channel, allowUnconfirmedContent).Manifest;

    private (CatalogReleaseManifest Manifest, Dictionary<string, string> Files) BuildRelease(string releaseVersion, ReleaseChannel channel, bool allowUnconfirmedContent)
    {
        var snapshot = sourceRepository.LoadSnapshot();

        var schemaResult = ValidateSchemas(snapshot);
        if (!schemaResult.IsValid)
        {
            throw new CatalogValidationException("Schema validation", schemaResult);
        }

        // Milestone A1 (source-integrity): runs across the FULL, unfiltered snapshot — including any
        // retired combinations — and never consults retirement eligibility. A malformed retired artifact
        // still fails here; a validly-retired combination whose dependency is also retired does NOT fail
        // here (that would incorrectly block the whole release — see
        // artifacts/audits/deterministic-graph-prechange-assessment.md Finding 1).
        var domainResult = CatalogGraphValidator.Validate(snapshot);
        if (!domainResult.IsValid)
        {
            throw new CatalogValidationException("Domain/graph validation", domainResult);
        }

        var stamped = CatalogStamper.StampAsPublished(serializer, hasher, snapshot);

        var readiness = PublishReadinessValidator.Validate(stamped, schemaResult, domainResult);
        if (!readiness.IsValid)
        {
            throw new CatalogValidationException("Publish readiness", readiness);
        }

        // A retired combination stays in source (for audit / historical-release verification) but is not
        // eligible to become a bundle, or an artifact, in any newly built release — see
        // artifacts/audits/full-catalog-retirement-packaging-audit.md.
        var eligibleCombinations = stamped.Combinations
            .Where(c => !retirementLedger.IsRetired(c.Metadata.DocumentType, c.Metadata.Key, c.Metadata.Version))
            .ToList();
        var stampedForRelease = stamped with { Combinations = eligibleCombinations };

        // Milestone A2 (publish-graph): runs only for each eligible (already non-retired) root and its
        // exact dependency closure — RulePack/master requirement compatibility, pinned-registry condition
        // validation, layout coverage, and defense-in-depth dependency-closure retirement checking.
        var publishGraphIssues = eligibleCombinations
            .SelectMany(c => Core.Validation.CandidatePublishGraphValidator.Validate(stamped, c, retirementLedger).Issues)
            .ToList();
        if (publishGraphIssues.Count > 0)
        {
            throw new CatalogValidationException("Candidate publish-graph validation", new Core.Validation.ValidationResult(publishGraphIssues));
        }

        var bundles = eligibleCombinations
            .Select(c => bundleAssembler.Assemble(stamped, c.Metadata.Key, c.Metadata.Version, retirementLedger))
            .ToList();

        var bundleArtifactTuples = bundles.SelectMany(BundleArtifactTuples).Distinct().ToList();
        var contentDecisionDetail = PublishReadinessValidator.ValidateContentDecisionsDetailed(bundleArtifactTuples, channel, allowUnconfirmedContent);
        if (!contentDecisionDetail.IsValid)
        {
            throw new CatalogValidationException("Content decision guard", contentDecisionDetail.ToValidationResult(), contentDecisionDetail);
        }

        var unconfirmedWarnings = bundleArtifactTuples
            .Where(t => Core.Audit.PilotDomainContentAudit.HasBlockingUnconfirmedContent(t.DocumentType, t.Key, t.Version))
            .Select(t => new UnconfirmedContentWarning
            {
                DocumentType = t.DocumentType,
                Key = t.Key,
                Version = t.Version,
                Message = $"Contains PLACEHOLDER_UNCONFIRMED content, acknowledged for {channel} channel via --allow-unconfirmed-content."
            })
            .ToList();

        var artifactRefs = AllMetadata(stampedForRelease).Select(CatalogArtifactReferences.ToRef).ToList();
        var bundleRefs = bundles.Select(b => new CatalogArtifactReference
        {
            DocumentType = DocumentTypes.PublishedTemplateBundle,
            Key = b.BundleKey,
            Version = b.BundleVersion,
            ContentHash = b.BundleContentHash
        }).ToList();

        var crossReleaseIssues = ValidateCrossReleaseHashConsistency(artifactRefs, bundleRefs);
        if (crossReleaseIssues.Count > 0)
        {
            throw new CatalogValidationException("Cross-release hash consistency", new PlanCatalog.Core.Validation.ValidationResult(crossReleaseIssues));
        }

        var manifestDraft = new CatalogReleaseManifest
        {
            ReleaseKey = "appsel-plan-catalog",
            ReleaseVersion = releaseVersion,
            Channel = channel,
            Artifacts = artifactRefs,
            Bundles = bundleRefs,
            UnconfirmedContentWarnings = unconfirmedWarnings,
            ManifestContentHash = string.Empty
        };

        var manifestHash = Hashing.CatalogDocumentHasher.ComputeHashExcludingField(serializer, hasher, manifestDraft, "manifestContentHash");
        var manifest = manifestDraft with { ManifestContentHash = manifestHash };

        var files = BuildReleaseFiles(stampedForRelease, bundles, manifest);

        return (manifest, files);
    }

    /// <summary>
    /// Rejects a new publish if any artifact or bundle it would publish shares a (documentType, key,
    /// version) identity with something already published in a previous release under a different
    /// content hash — see artifacts/audits/cross-release-hash-consistency-audit.md. Known, explicitly
    /// documented pre-existing mismatches are skipped via <see cref="ICrossReleaseHashExceptionRegistry"/>
    /// (never normalized or silently accepted; only exact, registered identity+release+hash tuples).
    /// </summary>
    private List<PlanCatalog.Core.Validation.ValidationIssue> ValidateCrossReleaseHashConsistency(
        List<CatalogArtifactReference> artifactRefs,
        List<CatalogArtifactReference> bundleRefs)
    {
        var issues = new List<PlanCatalog.Core.Validation.ValidationIssue>();

        var newIdentityHashes = artifactRefs.Concat(bundleRefs)
            .GroupBy(r => (r.DocumentType, r.Key, r.Version))
            .ToDictionary(g => g.Key, g => g.First().ContentHash);

        foreach (var existingReleaseVersion in publishedRepository.ListReleaseVersions())
        {
            CatalogReleaseManifest existingManifest;
            try
            {
                existingManifest = publishedRepository.ReadManifest(existingReleaseVersion);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var existingRef in existingManifest.Artifacts.Concat(existingManifest.Bundles))
            {
                if (!newIdentityHashes.TryGetValue((existingRef.DocumentType, existingRef.Key, existingRef.Version), out var newHash) ||
                    newHash == existingRef.ContentHash)
                {
                    continue;
                }

                if (_crossReleaseHashExceptionRegistry.IsKnownException(existingRef.DocumentType, existingRef.Key, existingRef.Version, existingReleaseVersion, existingRef.ContentHash))
                {
                    continue;
                }

                issues.Add(new PlanCatalog.Core.Validation.ValidationIssue("PUBLISH_HASH_MISMATCH_FOR_SAME_KEY_VERSION", PlanCatalog.Core.Validation.ValidationSeverity.Error,
                    $"'{existingRef.DocumentType}/{existingRef.Key}' v{existingRef.Version} is already published in release '{existingReleaseVersion}' with content hash '{existingRef.ContentHash}', " +
                    $"but this new release would publish it with a different hash '{newHash}'.", "$"));
            }
        }

        return issues;
    }

    private static IEnumerable<(string DocumentType, string Key, int Version)> BundleArtifactTuples(Contracts.Bundles.PublishedTemplateBundle bundle)
    {
        yield return (bundle.Combination.DocumentType, bundle.Combination.Key, bundle.Combination.Version);
        yield return (bundle.MasterTemplate.DocumentType, bundle.MasterTemplate.Key, bundle.MasterTemplate.Version);
        yield return (bundle.Layout.DocumentType, bundle.Layout.Key, bundle.Layout.Version);
        yield return (bundle.LevelModifier.DocumentType, bundle.LevelModifier.Key, bundle.LevelModifier.Version);
        yield return (bundle.WorkoutProgression.DocumentType, bundle.WorkoutProgression.Key, bundle.WorkoutProgression.Version);
        yield return (bundle.ProgressionModifier.DocumentType, bundle.ProgressionModifier.Key, bundle.ProgressionModifier.Version);
        yield return (bundle.RulePack.DocumentType, bundle.RulePack.Key, bundle.RulePack.Version);
        yield return (bundle.RuntimeConditionValueRegistry.DocumentType, bundle.RuntimeConditionValueRegistry.Key, bundle.RuntimeConditionValueRegistry.Version);
        yield return (bundle.PeakVolumeBandPolicy.DocumentType, bundle.PeakVolumeBandPolicy.Key, bundle.PeakVolumeBandPolicy.Version);
        foreach (var workout in bundle.Workouts)
        {
            yield return (workout.DocumentType, workout.Key, workout.Version);
        }
    }

    private PlanCatalog.Core.Validation.ValidationResult ValidateSchemas(CatalogSourceSnapshot snapshot)
    {
        var issues = new List<PlanCatalog.Core.Validation.ValidationIssue>();

        void ValidateEach<T>(IEnumerable<T> documents, string documentType)
        {
            foreach (var document in documents)
            {
                var json = serializer.Serialize(document);
                issues.AddRange(schemaValidator.Validate(documentType, json).Issues);
            }
        }

        ValidateEach(snapshot.PlanTemplates, DocumentTypes.PlanTemplate);
        ValidateEach(snapshot.RunLayouts, DocumentTypes.RunLayout);
        ValidateEach(snapshot.LevelModifiers, DocumentTypes.LevelModifier);
        ValidateEach(snapshot.WorkoutProgressions, DocumentTypes.WorkoutProgression);
        ValidateEach(snapshot.ProgressionModifiers, DocumentTypes.ProgressionModifier);
        ValidateEach(snapshot.Workouts, DocumentTypes.WorkoutDefinition);
        ValidateEach(snapshot.RuntimeConditionValueRegistries, DocumentTypes.RuntimeConditionValueRegistry);
        ValidateEach(snapshot.PeakVolumeBandPolicies, DocumentTypes.PeakVolumeBandPolicy);
        ValidateEach(snapshot.RulePacks, DocumentTypes.RulePack);
        ValidateEach(snapshot.Combinations, DocumentTypes.TemplateCombination);

        return new PlanCatalog.Core.Validation.ValidationResult(issues);
    }

    private static IEnumerable<PlanCatalog.Core.Metadata.CatalogDocumentMetadata> AllMetadata(CatalogSourceSnapshot snapshot) =>
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

    private Dictionary<string, string> BuildReleaseFiles(
        CatalogSourceSnapshot stamped,
        IReadOnlyList<Contracts.Bundles.PublishedTemplateBundle> bundles,
        CatalogReleaseManifest manifest)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);

        void AddAll<T>(IEnumerable<T> documents, string subFolder, Func<T, string> keyOf, Func<T, int> versionOf)
        {
            foreach (var document in documents)
            {
                var relativePath = $"{subFolder}/{keyOf(document)}.v{versionOf(document)}.json";
                files[relativePath] = serializer.Serialize(document);
            }
        }

        AddAll(stamped.PlanTemplates, "templates", d => d.Metadata.Key, d => d.Metadata.Version);
        AddAll(stamped.RunLayouts, "layouts", d => d.Metadata.Key, d => d.Metadata.Version);
        AddAll(stamped.LevelModifiers, "level-modifiers", d => d.Metadata.Key, d => d.Metadata.Version);
        AddAll(stamped.WorkoutProgressions, "workout-progressions", d => d.Metadata.Key, d => d.Metadata.Version);
        AddAll(stamped.ProgressionModifiers, "progression-modifiers", d => d.Metadata.Key, d => d.Metadata.Version);
        AddAll(stamped.Workouts, "workouts", d => d.Metadata.Key, d => d.Metadata.Version);
        AddAll(stamped.RuntimeConditionValueRegistries, "registries", d => d.Metadata.Key, d => d.Metadata.Version);
        AddAll(stamped.PeakVolumeBandPolicies, "policies", d => d.Metadata.Key, d => d.Metadata.Version);
        AddAll(stamped.RulePacks, "rule-packs", d => d.Metadata.Key, d => d.Metadata.Version);
        AddAll(stamped.Combinations, "combinations", d => d.Metadata.Key, d => d.Metadata.Version);
        AddAll(bundles, "bundles", b => b.BundleKey, b => b.BundleVersion);

        files["release-manifest.json"] = serializer.Serialize(manifest);

        var checksums = files
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{hasher.ComputeHash(kv.Value)}  {kv.Key}");
        files["checksums.sha256"] = string.Join('\n', checksums) + "\n";

        return files;
    }
}
