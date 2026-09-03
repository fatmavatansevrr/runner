namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// Phase 10K-GEN.23 -- implements the frozen Option-1 taper-minimum triple
/// approved on GEN.21's DOMAIN_DECISION_REQUIRED escalation (Phase K):
/// KEY=3.0km (TAPER_SHARPEN's existing floor, reused verbatim,
/// unmodified), EASY=2.5km (new, PRODUCT_DEFAULT), LONG=3.0km (new,
/// PRODUCT_DEFAULT_WITH_COACHING_PRACTICE_SUPPORT). This is a
/// TAPER-week-only floor, deliberately distinct from and lower than the
/// unchanged normal-week 12.0km (4.0+3.0+5.0) floor
/// (<see cref="Session.V1ThreeDaySessionVolumeAllocationPolicy"/>) --
/// GEN.21 §6/§7 found these are two genuinely separate authorities that had
/// never been distinguished before, not a single number.
/// </summary>
internal static class V1BeginnerThreeDayVolumeEligibilityPolicy
{
    public const double MinimumFullLayoutTaperWeeklyVolumeKm = 8.5d;

    /// <summary>
    /// Exact break-even, re-derived and verified this phase (not assumed):
    /// smallest 0.5km-grid pre-taper value X such that
    /// Round0.5(X * 0.53) &gt;= 8.5. At X=16.0: 16.0*0.53=8.48 -> Round0.5 =
    /// 8.5 (passes, exactly at the boundary -- the tightest point of the
    /// approved [16.0,20.0]km PeakVolumeBand, GEN.5A.2). At X=15.5:
    /// 15.5*0.53=8.215 -> Round0.5 = 8.0 (fails). This confirms the
    /// PeakVolumeBand's own frozen minimum (16.0km) is exactly the taper
    /// break-even point for this new floor -- zero slack, not a coincidence
    /// of a different number.
    /// </summary>
    public const double TaperBreakEvenPreTaperKm = 16.0d;
}
