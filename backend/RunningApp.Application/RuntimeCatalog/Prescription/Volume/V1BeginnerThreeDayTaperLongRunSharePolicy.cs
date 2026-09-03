namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// Phase 10K-GEN.23 -- narrow, taper-week-only long-run-share override for
/// Beginner×3D, discovered to be genuinely required (not invented to force
/// a pass) by real arithmetic verification this phase: <see cref="VolumeSafetyPolicy.ThreeDayBeginner"/>'s
/// normal-week long-run shares (38%/42% preferred, 40% selection, 42% hard
/// cap, reused verbatim from <see cref="VolumeSafetyPolicy.ThreeDayIntermediate"/>)
/// are required to keep the unchanged 5.0km normal-week LONG floor
/// satisfiable at Beginner's own lower starting volumes (e.g. at the 12.0km
/// missing-readiness start, a 40% share is needed to clear 5.0km; a lower
/// share like 33% would compute only 4.0km and fail closed immediately).
/// But applying that SAME 40% share at the TAPER week produces a real
/// infeasibility at the taper floor's own tightest point: at the exact
/// 8.5km taper-week volume (16.0km pre-taper x 0.53, GEN.21's own binding
/// case), a 40% share computes LONG=3.5km, which combined with the new
/// 3.0km KEY floor and 2.5km EASY floor sums to 9.0km -- 0.5km OVER the
/// 8.5km weekly total, and <see cref="Session.V1ThreeDaySessionVolumeAllocationPolicy.Allocate"/>'s
/// reconciliation loop never adjusts LONG (only KEY/EASY absorb residual),
/// so this is a genuine, unrecoverable per-role infeasibility, not merely a
/// rounding nuisance -- confirmed by hand this phase before writing any
/// code.
///
/// The values below (30%/36% preferred, 33% selection, 40% hard cap) are
/// NOT a new invented number: they are <see cref="VolumeSafetyPolicy.BeginnerFourDay"/>'s
/// (and <see cref="VolumeSafetyPolicy.Default"/>'s) own already-approved
/// long-run shares, reused verbatim across a different frequency/context
/// exactly as this engagement's `EXISTING_SHARED_POLICY_REUSED_DUE_TO_NO_LEVEL_EFFECT`
/// pattern already does elsewhere. Verified exact at the binding case: at
/// 8.5km, 33% selection share computes Round0.5(8.5*0.33)=Round0.5(2.805)=3.0km
/// -- exactly the new LONG floor, zero slack -- so KEY(3.0)+EASY(2.5)+LONG(3.0)=8.5km
/// reconciles with no adjustment needed. This override applies ONLY to the
/// single taper week of a Beginner×3D candidate; every other week (and
/// every other candidate) is unaffected by construction.
/// </summary>
internal static class V1BeginnerThreeDayTaperLongRunSharePolicy
{
    public const double PreferredMinimumShare = 0.30d;
    public const double PreferredMaximumShare = 0.36d;
    public const double SelectionShare = 0.33d;
    public const double HardCapShare = 0.40d;
}
