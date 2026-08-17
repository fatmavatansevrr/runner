namespace RunningApp.Application.RuntimeCatalog.Prescription.Execution;

/// <summary>
/// Phase 10K-FREQ.6D.3D — typed, fail-closed failures for the Process B execution-prescription
/// ingestion boundary (<see cref="ExecutionPrescriptionIndex"/>). None of these is caught and
/// silently converted into legacy behavior — a profile-backed bundle that fails any of these
/// checks fails the whole ingestion, per FREQ.6D.3C's own "no partial bundle or legacy fallback"
/// invariant carried forward from Process A.
/// </summary>
internal abstract class ExecutionPrescriptionBoundaryException : Exception
{
    protected ExecutionPrescriptionBoundaryException(string message) : base(message) { }
}

/// <summary>An <c>ExecutableWorkoutPrescription</c> entry failed the Contracts boundary validator (<c>ExecutableWorkoutPrescriptionValidator.Validate</c>), including an unsupported <c>ContractSchemaVersion</c>.</summary>
internal sealed class ExecutionPrescriptionContractInvalidException : ExecutionPrescriptionBoundaryException
{
    public ExecutionPrescriptionContractInvalidException(string message) : base(message) { }
}

/// <summary>Two or more entries in <c>PublishedTemplateBundle.ExecutionPrescriptions</c> share the same exact <c>SourceProfile</c> provenance (DocumentType, Key, Version) — ambiguous, never resolved by first/last-wins.</summary>
internal sealed class ExecutionPrescriptionDuplicateProvenanceException : ExecutionPrescriptionBoundaryException
{
    public ExecutionPrescriptionDuplicateProvenanceException(string message) : base(message) { }
}

/// <summary>No entry in a profile-backed bundle's index matches the exact requested provenance. Never resolved by nearest/latest/first-match.</summary>
internal sealed class ExecutionPrescriptionNotFoundException : ExecutionPrescriptionBoundaryException
{
    public ExecutionPrescriptionNotFoundException(string message) : base(message) { }
}

/// <summary>An exact-lookup was attempted against a legacy (non-profile-backed) index. A legacy bundle has no execution-prescription index to resolve against.</summary>
internal sealed class ExecutionPrescriptionLegacyIndexException : ExecutionPrescriptionBoundaryException
{
    public ExecutionPrescriptionLegacyIndexException(string message) : base(message) { }
}
