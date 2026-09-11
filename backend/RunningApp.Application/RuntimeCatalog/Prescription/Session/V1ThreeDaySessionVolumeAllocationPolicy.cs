using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

internal static class V1ThreeDaySessionVolumeAllocationPolicy
{
    public const string PolicyKey = "V1_TEN_K_INTERMEDIATE_3D_SESSION_ALLOCATION_POLICY";
    public const int PolicyVersion = 1;
    public const double MinimumKeyKm = 4d;
    public const double MinimumEasyKm = 3d;
    public const double MinimumLongKm = 5d;

    /// <summary>
    /// Phase 10K-GEN.23 -- the frozen Option-1 Beginner×3D TAPER-week-only
    /// minima triple (GEN.21's DOMAIN_DECISION_REQUIRED escalation,
    /// resolved). KEY reuses TAPER_SHARPEN's existing approved 3.0km floor
    /// verbatim (<see cref="V1TaperSharpenPrescriptionPolicy.MinSessionDistanceKm"/>).
    /// EASY/LONG are new. Deliberately a SEPARATE, lower triple from
    /// <see cref="MinimumKeyKm"/>/<see cref="MinimumEasyKm"/>/<see cref="MinimumLongKm"/>
    /// above, which remain byte-identical and apply to every non-taper week
    /// (and to every Intermediate×3D week, taper included -- Intermediate's
    /// own taper gate is unmodified) exactly as before this phase.
    /// </summary>
    public const double BeginnerTaperMinimumKeyKm = 3d;
    public const double BeginnerTaperMinimumEasyKm = 2.5d;
    public const double BeginnerTaperMinimumLongKm = 3d;
    private const double KeyShare = 0.35d;
    private const double EasyShare = 0.25d;

    /// <summary>
    /// The TEN_K Intermediate/Beginner x3D own hard-cap share default (GEN.2B/GEN.21-23
    /// authority) -- unchanged, still the value every existing 10K caller observes.
    /// </summary>
    private const double DefaultLongHardCap = 0.42d;

    /// <param name="useBeginnerThreeDayTaperMinima">
    /// Phase 10K-GEN.23 -- true only for a Beginner×3D candidate's TAPER
    /// week (caller-computed: <c>weekly.IsTaperWeek &amp;&amp; Level == "NEW"</c>).
    /// Every existing caller (Intermediate×3D, every non-taper Beginner×3D
    /// week) passes false and observes byte-identical behavior to before
    /// this phase.
    /// </param>
    /// <param name="longRunHardCapShare">
    /// Phase HM.8 -- recurring-assumption-family fix: this session-allocation-level safety
    /// check previously re-validated the already-computed <paramref name="longRun"/> distance
    /// against a HARDCODED 0.42 share regardless of which upstream <see cref="Volume.VolumeSafetyPolicy"/>
    /// actually produced it -- correct for every existing TEN_K 3D policy (whose own
    /// <see cref="Volume.VolumeSafetyPolicy.LongRunHardCapShare"/> is exactly 0.42), but silently
    /// wrong for HALF_MARATHON Intermediate×3D's own frozen 0.46 (HM.7). Now an explicit,
    /// caller-supplied parameter defaulting to <see cref="DefaultLongHardCap"/> -- every existing
    /// caller that omits it observes byte-identical behavior; <see cref="CatalogSessionPrescriptionPlanner"/>
    /// now passes the real, already-resolved <c>WeeklyShareDecision.HardCapShare</c> from the
    /// upstream volume/long-run plan explicitly, so this check always agrees with the policy that
    /// actually generated the long run, not a second, independent, distance-blind assumption.
    /// </param>
    public static V1FourDayWeekAllocation Allocate(
        CatalogWeeklyVolumeWeek weekly, CatalogLongRunWeek longRun, IReadOnlyList<BoundCatalogSession> sessions,
        bool useBeginnerThreeDayTaperMinima = false, double longRunHardCapShare = DefaultLongHardCap)
    {
        var longHardCap = longRunHardCapShare;
        if (sessions.Count != 3 || sessions.Count(s => s.StructuralRole == "KEY_SESSION") != 1 ||
            sessions.Count(s => s.StructuralRole == "EASY_SUPPORT") != 1 || sessions.Count(s => s.StructuralRole == "LONG_RUN") != 1)
            throw new CatalogSessionPrescriptionInfeasibleException($"Week {weekly.WeekNumber} does not match RUN_LAYOUT_3D.");

        var minKey = useBeginnerThreeDayTaperMinima ? BeginnerTaperMinimumKeyKm : MinimumKeyKm;
        var minEasy = useBeginnerThreeDayTaperMinima ? BeginnerTaperMinimumEasyKm : MinimumEasyKm;
        var minLong = useBeginnerThreeDayTaperMinima ? BeginnerTaperMinimumLongKm : MinimumLongKm;

        var volume = weekly.PlannedWeeklyVolumeKm;
        if (volume < minKey + minEasy + minLong)
        {
            throw new CatalogSessionPrescriptionInfeasibleException(
                useBeginnerThreeDayTaperMinima
                    ? $"Week {weekly.WeekNumber} is below the 8.5km Beginner 3D taper-specific direct-prescription floor."
                    : $"Week {weekly.WeekNumber} is below the 12km 3D direct-prescription floor.");
        }

        var targets = new[] { volume * KeyShare, volume * EasyShare, volume * 0.40d };
        var current = new[] { Round(targets[0]), Round(targets[1]), longRun.PlannedLongRunDistanceKm };
        current[0] = Math.Max(minKey, current[0]);
        current[1] = Math.Max(minEasy, current[1]);
        if (current[2] < minLong || current[2] / volume > longHardCap + 0.0001d ||
            Math.Abs(current[2] * 2d - Math.Round(current[2] * 2d)) > 0.0001d)
            // HM.11 -- cosmetic-message correction (HM.8's own disclosed defect): this message
            // hardcoded the literal text "42%" even after HM.8 parameterized the actual cap via
            // longRunHardCapShare (which HALF_MARATHON Intermediate x3D already resolves to 46%
            // in real production). Reports the real, resolved cap now -- behaviorally zero-delta,
            // no validation condition above is touched.
            throw new CatalogSessionPrescriptionInfeasibleException($"Resolved 3D long run violates the minimum, {longHardCap * 100:0}% hard cap, or 0.5km granularity.");

        while (Math.Abs(current.Sum() - volume) > 0.001d)
        {
            var direction = current.Sum() < volume ? 0.5d : -0.5d;
            var candidates = Enumerable.Range(0, 3)
                .Select(i => new { Index = i, Value = current[i] + direction })
                .Where(c => c.Value >= new[] { minKey, minEasy, minLong }[c.Index])
                .Where(c => c.Index != 2)
                .OrderBy(c => Math.Abs(c.Value - targets[c.Index]))
                .ThenBy(c => c.Index)
                .ToList();
            if (candidates.Count == 0) throw new CatalogSessionPrescriptionInfeasibleException("No valid deterministic 3D reconciliation adjustment exists.");
            current[candidates[0].Index] = candidates[0].Value;
        }

        if (current[2] / volume > longHardCap + 0.0001d)
            // HM.11 -- same cosmetic-message correction as above.
            throw new CatalogSessionPrescriptionInfeasibleException($"Reconciled 3D long run exceeds the {longHardCap * 100:0}% hard cap.");

        return new V1FourDayWeekAllocation(weekly.WeekNumber, volume, current[2], Round(volume-current[2]), new[] { current[0] }, new[] { current[1] },
            new SessionVolumeAllocationTrace(weekly.WeekNumber, volume, current[2], Round(volume-current[2]), current[0], current[1], 0d, PolicyKey, PolicyVersion));
    }

    private static double Round(double value) => Math.Round(value * 2d, MidpointRounding.AwayFromZero) / 2d;
}
