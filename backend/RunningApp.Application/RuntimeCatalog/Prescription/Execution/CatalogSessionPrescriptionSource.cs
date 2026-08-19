using PlanCatalog.Contracts.Prescriptions;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Execution;

/// <summary>
/// Phase 10K-FREQ.6D.3D — the discriminated boundary a session's prescription source can take: the
/// existing lossy legacy <see cref="CatalogWorkoutPrescription"/> path, or the lossless
/// <see cref="ExecutableWorkoutPrescription"/> Contracts value carried unmodified (never copied
/// field-by-field into a backend-local mirror — see RUNNINGAPP_CONSUMER_DOES_NOT_DUPLICATE_EXECUTION_AUTHORITY).
///
/// Follows this codebase's existing "abstract base + exactly two sealed subclasses" discriminator
/// convention (<see cref="RunningApp.Application.Commands.Plan.PlanPreviewCommand"/>).
///
/// Phase 10K-FREQ.6D.4D Split C wired this type into <see cref="CatalogSessionPrescriptionPlanner"/>'s
/// live session-building branch: every <see cref="CatalogPrescribedSession"/> now carries a
/// <see cref="CatalogPrescribedSession.PrescriptionSource"/>, classified from the bound session's own
/// <see cref="Schedule.Binding.BoundCatalogSession.PrescriptionProfileKey"/>/<c>Version</c> (Split B),
/// resolved via <see cref="ExecutionPrescriptionIndex.ResolveExact"/> for <see cref="ProfileBacked"/>
/// sessions — never falling back to <see cref="Legacy"/> once a session is explicitly ProfileBacked.
/// </summary>
internal abstract record CatalogSessionPrescriptionSource
{
    internal sealed record Legacy(CatalogWorkoutPrescription Prescription) : CatalogSessionPrescriptionSource;

    internal sealed record ProfileBacked(ExecutableWorkoutPrescription Prescription) : CatalogSessionPrescriptionSource;
}
