using RunningApp.Application.Exceptions;

namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.1 — enforces candidate/dependency lifecycle
/// eligibility before a catalog candidate may be used for a PUBLIC preview.
///
/// Governance rule: "Public catalog preview must select only a candidate
/// whose metadata.status is PUBLISHED. All referenced runtime dependencies
/// required by that candidate must also be runtime-eligible... A DRAFT or
/// VALIDATED candidate may be loaded only by tests or an explicitly internal
/// dry-run path."
/// </summary>
public interface ICatalogCandidateEligibilityGate
{
    /// <summary>
    /// Loads and validates <paramref name="candidateKey"/> v<paramref name="candidateVersion"/>
    /// for public use. Throws <see cref="CatalogCandidateNotPublishedException"/>
    /// if the candidate's own status is not PUBLISHED, or
    /// <see cref="CatalogDependencyNotRuntimeEligibleException"/> if any
    /// directly-loaded dependency's status is not PUBLISHED. Throws
    /// <see cref="CatalogPilotNotAvailableException"/> (wrapping the
    /// underlying <see cref="PlanCatalogLoadException"/>) if the candidate
    /// cannot be loaded at all.
    /// </summary>
    Task<PlanCatalogCandidateSummary> LoadForPublicPreviewAsync(string candidateKey, int candidateVersion, CancellationToken ct = default);

    /// <summary>
    /// Loads <paramref name="candidateKey"/> v<paramref name="candidateVersion"/>
    /// WITHOUT any lifecycle-status check — for tests and explicit internal
    /// dry-run use only. Never call this from a public request path.
    /// </summary>
    Task<PlanCatalogCandidateSummary> LoadForInternalDryRunAsync(string candidateKey, int candidateVersion, CancellationToken ct = default);
}

/// <inheritdoc cref="ICatalogCandidateEligibilityGate"/>
public sealed class CatalogCandidateEligibilityGate : ICatalogCandidateEligibilityGate
{
    private const string PublishedStatus = "PUBLISHED";

    private readonly IPlanCatalogBundleLoader _loader;

    public CatalogCandidateEligibilityGate(IPlanCatalogBundleLoader loader)
    {
        _loader = loader;
    }

    public async Task<PlanCatalogCandidateSummary> LoadForPublicPreviewAsync(string candidateKey, int candidateVersion, CancellationToken ct = default)
    {
        var summary = await LoadOrThrowPilotNotAvailableAsync(candidateKey, candidateVersion, ct);

        if (summary.CandidateStatus != PublishedStatus)
        {
            throw new CatalogCandidateNotPublishedException(
                $"Catalog candidate {candidateKey} v{candidateVersion} has status '{summary.CandidateStatus}', " +
                $"not '{PublishedStatus}'. It is not eligible for public preview.");
        }

        var ineligibleDependency = summary.DependencyStatuses.FirstOrDefault(kv => kv.Value != PublishedStatus);
        if (ineligibleDependency.Key is not null)
        {
            throw new CatalogDependencyNotRuntimeEligibleException(
                $"Catalog candidate {candidateKey} v{candidateVersion}'s dependency '{ineligibleDependency.Key}' has " +
                $"status '{ineligibleDependency.Value}', not '{PublishedStatus}'. The candidate itself is published, " +
                "but this dependency is not, so the candidate is not runtime-eligible.");
        }

        return summary;
    }

    public Task<PlanCatalogCandidateSummary> LoadForInternalDryRunAsync(string candidateKey, int candidateVersion, CancellationToken ct = default) =>
        LoadOrThrowPilotNotAvailableAsync(candidateKey, candidateVersion, ct);

    private async Task<PlanCatalogCandidateSummary> LoadOrThrowPilotNotAvailableAsync(string candidateKey, int candidateVersion, CancellationToken ct)
    {
        try
        {
            return await _loader.LoadCandidateAsync(candidateKey, candidateVersion, ct);
        }
        catch (PlanCatalogLoadException ex)
        {
            throw new CatalogPilotNotAvailableException(
                $"Catalog candidate {candidateKey} v{candidateVersion} could not be loaded: {ex.Message}", ex);
        }
    }
}
