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
/// This type is intentionally NOT wired into <see cref="CatalogSessionPrescriptionPlanner"/>'s live
/// session-building branch in this phase: selecting which sessions become <see cref="ProfileBacked"/>
/// requires an exact profile reference per (week, lane), which is FREQ.6D.4's own frozen scope (Week ×
/// LaneOrdinal → exact progression stage → exact profile reference), not this phase's. It exists here,
/// proven by tests, as the seam FREQ.6D.4 wires into.
/// </summary>
internal abstract record CatalogSessionPrescriptionSource
{
    internal sealed record Legacy(CatalogWorkoutPrescription Prescription) : CatalogSessionPrescriptionSource;

    internal sealed record ProfileBacked(ExecutableWorkoutPrescription Prescription) : CatalogSessionPrescriptionSource;
}
