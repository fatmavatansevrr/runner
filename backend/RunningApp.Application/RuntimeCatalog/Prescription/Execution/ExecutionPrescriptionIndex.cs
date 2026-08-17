using PlanCatalog.Contracts.Bundles;
using PlanCatalog.Contracts.Prescriptions;
using PlanCatalog.Contracts.References;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Execution;

/// <summary>
/// Phase 10K-FREQ.6D.3D — the exact-reference lookup of an already-resolved
/// <see cref="ExecutableWorkoutPrescription"/> out of a <see cref="PublishedTemplateBundle"/>'s
/// <c>ExecutionPrescriptions</c>. This is lookup of an already-resolved execution value, never catalog
/// selection: it does not choose a profile, does not know about lanes/KEY ordinals/dose category
/// preference, and never falls back to nearest/latest/first-match. FREQ.6D.4 owns producing the exact
/// <see cref="VersionedCatalogReference"/> this index is queried with.
/// </summary>
internal readonly record struct ExecutionPrescriptionLookupKey(string DocumentType, string Key, int Version)
{
    public static ExecutionPrescriptionLookupKey From(CatalogArtifactReference reference) =>
        new(reference.DocumentType, reference.Key, reference.Version);

    public static ExecutionPrescriptionLookupKey From(VersionedCatalogReference reference) =>
        new(reference.DocumentType, reference.Key, reference.Version);
}

internal sealed class ExecutionPrescriptionIndex
{
    private readonly IReadOnlyDictionary<ExecutionPrescriptionLookupKey, ExecutableWorkoutPrescription>? _byProfileProvenance;

    private ExecutionPrescriptionIndex(IReadOnlyDictionary<ExecutionPrescriptionLookupKey, ExecutableWorkoutPrescription>? byProfileProvenance)
    {
        _byProfileProvenance = byProfileProvenance;
    }

    /// <summary>True when the source bundle declared a non-null <c>ExecutionPrescriptions</c> collection (profile-backed). False for a legacy bundle (<c>ExecutionPrescriptions == null</c>).</summary>
    public bool IsProfileBacked => _byProfileProvenance is not null;

    public int Count => _byProfileProvenance?.Count ?? 0;

    /// <summary>
    /// Builds the index from a real <see cref="PublishedTemplateBundle"/>. Fail-closed: every entry is
    /// validated with the Contracts boundary validator (unsupported schema version, malformed values,
    /// inconsistent recovery cardinality all reject the whole bundle — never a partial index), and any
    /// duplicate exact provenance rejects the whole bundle. Null <c>ExecutionPrescriptions</c> yields the
    /// legacy (<see cref="IsProfileBacked"/> == false) index, per FREQ.6D.3B/3C's null-vs-legacy semantics.
    /// </summary>
    public static ExecutionPrescriptionIndex Build(PublishedTemplateBundle bundle)
    {
        if (bundle.ExecutionPrescriptions is null)
        {
            return new ExecutionPrescriptionIndex(byProfileProvenance: null);
        }

        var index = new Dictionary<ExecutionPrescriptionLookupKey, ExecutableWorkoutPrescription>();

        foreach (var execution in bundle.ExecutionPrescriptions)
        {
            var errors = ExecutableWorkoutPrescriptionValidator.Validate(execution);
            if (errors.Count > 0)
            {
                throw new ExecutionPrescriptionContractInvalidException(
                    $"Bundle '{bundle.BundleKey}' v{bundle.BundleVersion} carries an invalid execution prescription " +
                    $"for source profile '{execution.SourceProfile.Key}' v{execution.SourceProfile.Version}: {string.Join(", ", errors)}.");
            }

            var key = ExecutionPrescriptionLookupKey.From(execution.SourceProfile);
            if (!index.TryAdd(key, execution))
            {
                throw new ExecutionPrescriptionDuplicateProvenanceException(
                    $"Bundle '{bundle.BundleKey}' v{bundle.BundleVersion} carries more than one execution prescription " +
                    $"for the same exact source profile provenance ({key.DocumentType} '{key.Key}' v{key.Version}).");
            }
        }

        return new ExecutionPrescriptionIndex(index);
    }

    /// <summary>
    /// Resolves exactly one <see cref="ExecutableWorkoutPrescription"/> for the given exact profile
    /// reference. No latest/nearest/first-match resolution exists. A missing exact match — including a
    /// key that exists at a different version — fails closed.
    /// </summary>
    public ExecutableWorkoutPrescription ResolveExact(VersionedCatalogReference profileRef)
    {
        if (_byProfileProvenance is null)
        {
            throw new ExecutionPrescriptionLegacyIndexException(
                $"Cannot resolve execution prescription for '{profileRef.Key}' v{profileRef.Version}: this bundle is legacy " +
                "(ExecutionPrescriptions == null) and carries no execution-prescription index.");
        }

        var key = ExecutionPrescriptionLookupKey.From(profileRef);
        if (_byProfileProvenance.TryGetValue(key, out var execution))
        {
            return execution;
        }

        throw new ExecutionPrescriptionNotFoundException(
            $"No execution prescription found for exact reference {key.DocumentType} '{key.Key}' v{key.Version}.");
    }
}
