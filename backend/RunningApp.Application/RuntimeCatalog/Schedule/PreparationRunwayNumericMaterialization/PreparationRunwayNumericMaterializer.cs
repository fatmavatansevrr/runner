using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;

/// <summary>
/// Pure, dark-unwired numeric enrichment of already materialized undated
/// runway weeks. No caller is registered in DI or any public orchestration
/// path.
/// </summary>
internal static class PreparationRunwayNumericMaterializer
{
    public static PreparationRunwayNumericMaterializationResult<TKey> Materialize<TKey>(
        PreparationRunwayNumericMaterializationRequest<TKey> request) where TKey : notnull
    {
        var trace = new List<string>();
        try
        {
            var validation = ValidateRequest(request);
            if (validation is not null) return Fail<TKey>(validation.Value.Code, validation.Value.Reason, trace);

            var weeks = request.MaterializedWeeks.OrderBy(w => w.RunwayWeekNumber).ToArray();
            var policy = request.Policy;
            var rawStartingWeekly = ResolveStartingWeeklyVolume(request.StartingLoadEvidence, policy);
            var targetWeekly = request.CoreWeekOneTarget.WeeklyVolumeKm;

            // Phase 10K-FREQ.6D.16/FREQ.6D.17: there is no approved non-taper
            // runway reduction coefficient, so Runway can never grow DOWN from
            // an entry evidence above Core's boundary via its own interpolation.
            // The approved resolution (FREQ.6D.16) is not a new reduction
            // formula -- it clamps Runway's own starting point to the boundary
            // it was already never allowed to exceed, reusing Core's own
            // already-computed Week-1 target as the ceiling (never a new
            // number). When evidence is already at or below the target this is
            // a byte-identical no-op. The interpolation formula below already
            // handles start == target correctly (a flat bridge), so no other
            // code path changes.
            var startingWeekly = Math.Min(rawStartingWeekly, targetWeekly);
            trace.Add($"raw_starting_weekly={rawStartingWeekly:0.###}km; target_core_week_one={targetWeekly:0.###}km; effective_starting_weekly={startingWeekly:0.###}km");

            var rawStartingLongRun = ResolveStartingLongRun(
                request.StartingLoadEvidence, rawStartingWeekly, policy);
            var targetLongRun = request.CoreWeekOneTarget.LongRunDistanceKm;
            var startingLongRun = Math.Min(rawStartingLongRun, targetLongRun);
            trace.Add($"raw_starting_long_run={rawStartingLongRun:0.###}km; target_long_run={targetLongRun:0.###}km; effective_starting_long_run={startingLongRun:0.###}km");

            var prescribed = new List<PreparationRunwayPrescribedWeek<TKey>>(weeks.Length);
            double? previousWeekly = null;
            double? previousLongRun = null;
            var developmentTransitions = weeks.Length - 2; // terminal Transition is an exact maintenance bridge.

            for (var index = 0; index < weeks.Length; index++)
            {
                var isTransition = index == weeks.Length - 1;
                var progress = isTransition ? 1d : index / (double)developmentTransitions;
                var unroundedWeekly = startingWeekly + ((targetWeekly - startingWeekly) * progress);
                var weekly = Round(unroundedWeekly, policy.RoundingIncrementKm);
                var unroundedLongRun = startingLongRun + ((targetLongRun - startingLongRun) * progress);
                var longRun = Round(unroundedLongRun, policy.RoundingIncrementKm);

                if (isTransition)
                {
                    weekly = targetWeekly;
                    longRun = targetLongRun;
                }

                var weeklyChange = previousWeekly is null ? 0d : Round(weekly - previousWeekly.Value, policy.RoundingIncrementKm);
                double? weeklyRatio = previousWeekly is null or 0 ? null : weeklyChange / previousWeekly.Value;
                if (weeklyChange > policy.AbsoluteWeeklyIncrementCapKm + policy.ContinuityToleranceKm ||
                    weeklyRatio > policy.HardMaxWeeklyIncreaseRatio + policy.ContinuityToleranceKm)
                {
                    return Fail<TKey>(PreparationRunwayNumericMaterializationFailureCode.WeeklyChangeLimitExceeded,
                        $"Week {weeks[index].RunwayWeekNumber} requires {weeklyChange:0.###}km ({weeklyRatio:P2}), exceeding approved limits.", trace);
                }
                if (weeklyChange < -policy.ContinuityToleranceKm)
                {
                    return Fail<TKey>(PreparationRunwayNumericMaterializationFailureCode.RunwayProgressionInfeasible,
                        $"Week {weeks[index].RunwayWeekNumber} would require an unapproved runway reduction.", trace);
                }

                if (previousLongRun is not null && longRun + policy.ContinuityToleranceKm < previousLongRun.Value)
                {
                    return Fail<TKey>(PreparationRunwayNumericMaterializationFailureCode.LongRunContinuityViolation,
                        $"Week {weeks[index].RunwayWeekNumber} would reduce the long run without an approved settle rule.", trace);
                }

                // `share` is a ratio (0..1), not a km quantity -- it needs a
                // ratio-scale tolerance, not the km-scale ContinuityToleranceKm
                // epsilon (meant for exact sum-reconciliation checks). Each
                // policy now owns its own LongRunShareTolerance explicitly:
                // Default/ThreeDayIntermediate/BeginnerFourDay keep the exact
                // prior numeric behavior (a governance test asserts one of
                // them still rejects a real violation at the original tight
                // margin -- see the direction-matrix diagnostic test in
                // LongHorizonCoreWeekOneEvidenceAuthorityDiagnosticTests).
                // FiveDayIntermediate (Phase 10K-FREQ.6D.10) needs a wider,
                // still-non-invented tolerance because FREQ.6C approved
                // exactly two 5D long-run figures (28% selection, 36% hard
                // cap) with no separate preferred-minimum, so its floor sits
                // exactly at the selection share with zero nominal gap -- see
                // TenKPreparationRunwayNumericPolicyFactory for the derivation.
                var share = longRun / weekly;
                if (share + policy.LongRunShareTolerance < policy.LongRunPreferredMinimumShare ||
                    share - policy.LongRunShareTolerance > policy.LongRunPreferredMaximumShare ||
                    share - policy.LongRunShareTolerance > policy.LongRunHardCapShare)
                {
                    return Fail<TKey>(PreparationRunwayNumericMaterializationFailureCode.LongRunShareViolation,
                        $"Week {weeks[index].RunwayWeekNumber} long-run share {share:P2} is outside the approved range.", trace);
                }

                // Runway itself always has exactly one KEY_SESSION for every
                // pre-GEN.29 shape (FREQ.6D.6: dual KEY begins only at real
                // Core Week 1); the EASY_SUPPORT count is read from the real
                // materialized layout width so it generalizes to any approved
                // Runway shape. Phase 10K-GEN.29 -- this call previously never
                // passed keySessionCount, always defaulting to 1 even on a 2D
                // Pattern-B week (which has zero KEY_SESSION slots): the
                // allocator would then silently reserve volume for a KEY
                // session that no slot in the week actually has, producing a
                // rounded-slot-sum shortfall against the weekly total (the
                // very next invariant check below). Reading keySessionCount
                // from the real materialized week, exactly like
                // easySupportCount already does, fixes this -- byte-identical
                // for every pre-GEN.29 shape (always exactly 1 there).
                var keySessionCount = weeks[index].OrderedWorkoutSlots.Count(s => s.SlotRole == PreparationRunwaySlotRole.KeySession);
                var easySupportCount = weeks[index].OrderedWorkoutSlots.Count(s => s.SlotRole == PreparationRunwaySlotRole.EasySupport);
                FourDaySessionDistanceAllocation allocation;
                try
                {
                    allocation = FourDaySessionDistanceAllocationPolicy.Allocate(weekly, longRun, keySessionCount: keySessionCount, easySupportCount: easySupportCount);
                }
                catch (CatalogSessionPrescriptionInfeasibleException exception)
                {
                    return Fail<TKey>(PreparationRunwayNumericMaterializationFailureCode.SlotDistributionInfeasible,
                        $"Week {weeks[index].RunwayWeekNumber}: {exception.Message}", trace);
                }

                var slots = BuildSlots(weeks[index], allocation, request.Unit, policy);
                if (Math.Abs(slots.Sum(s => s.PlannedDistanceKm) - weekly) > policy.ContinuityToleranceKm)
                {
                    return Fail<TKey>(PreparationRunwayNumericMaterializationFailureCode.RoundingInvariantViolation,
                        $"Week {weeks[index].RunwayWeekNumber} rounded slots do not equal the weekly total.", trace);
                }

                var trajectory = TrajectoryFor(weeks[index].BlockType?.ToString() ?? "UNKNOWN", isTransition, weeklyChange);
                var numericTrace = new PreparationRunwayNumericDecisionTrace(
                    weeks[index].RunwayWeekNumber,
                    weeks[index].BlockType?.ToString() ?? "UNKNOWN",
                    unroundedWeekly,
                    weekly,
                    previousWeekly,
                    weeklyChange,
                    weeklyRatio,
                    unroundedLongRun,
                    longRun,
                    share,
                    trajectory,
                    weeklyChange == 0 ? "maintenance" : "hard_ratio_and_absolute_increment_caps_validated",
                    policy.RoundingRule,
                    [
                        request.StartingLoadEvidence.SourceProvenance,
                        request.CoreWeekOneTarget.SourceProvenance,
                        $"{policy.PolicyKey} v{policy.PolicyVersion}",
                        "V1_FOUR_DAY_SESSION_VOLUME_ALLOCATION_POLICY v1",
                    ]);

                prescribed.Add(new PreparationRunwayPrescribedWeek<TKey>(weeks[index], weekly, longRun, slots, numericTrace));
                previousWeekly = weekly;
                previousLongRun = longRun;
            }

            var continuity = AnalyzeContinuity(prescribed[^1], request.CoreWeekOneTarget, policy);
            if (!continuity.IsWithinTolerance)
            {
                return Fail<TKey>(PreparationRunwayNumericMaterializationFailureCode.CoreEntryContinuityViolation,
                    "Final runway quantities do not exactly reproduce the authoritative Core Week 1 boundary.", trace);
            }

            trace.Add("terminal_transition=exact_core_week_one_numeric_boundary; no_partial_prescription=true");
            return PreparationRunwayNumericMaterializationResult<TKey>.Success(prescribed, continuity, trace);
        }
        catch (InvalidOperationException exception)
        {
            return Fail<TKey>(PreparationRunwayNumericMaterializationFailureCode.NumericMaterializationInvariantViolation,
                exception.Message, trace);
        }
    }

    private static (PreparationRunwayNumericMaterializationFailureCode Code, string Reason)? ValidateRequest<TKey>(
        PreparationRunwayNumericMaterializationRequest<TKey>? request) where TKey : notnull
    {
        if (request is null || request.Policy is null || request.StartingLoadEvidence is null || request.CoreWeekOneTarget is null || request.MaterializedWeeks is null)
            return (PreparationRunwayNumericMaterializationFailureCode.InvalidNumericMaterializationRequest, "Request and required policies must be present.");
        if (request.Policy.PolicyKey != TenKPreparationRunwayNumericPolicyFactory.PolicyKey || request.Policy.PolicyVersion != TenKPreparationRunwayNumericPolicyFactory.PolicyVersion)
            return (PreparationRunwayNumericMaterializationFailureCode.MissingStartingLoadPolicy, "The approved TEN_K runway numeric policy is required.");
        var evidence = request.StartingLoadEvidence;
        if ((evidence.WeeklyVolumeState == PreparationRunwayLoadEvidenceState.Provided && evidence.RecentWeeklyVolumeKm is not > 0) ||
            (evidence.WeeklyVolumeState == PreparationRunwayLoadEvidenceState.Missing && evidence.RecentWeeklyVolumeKm is not null) ||
            (evidence.WeeklyVolumeState == PreparationRunwayLoadEvidenceState.NoRecentRunningBase && evidence.RecentWeeklyVolumeKm is not null and not 0))
            return (PreparationRunwayNumericMaterializationFailureCode.InvalidRecentWeeklyVolume, "Recent weekly volume conflicts with its typed evidence state.");
        if ((evidence.LongestRunState == PreparationRunwayLoadEvidenceState.Provided && evidence.RecentLongestRunKm is not > 0) ||
            (evidence.LongestRunState == PreparationRunwayLoadEvidenceState.Missing && evidence.RecentLongestRunKm is not null) ||
            (evidence.LongestRunState == PreparationRunwayLoadEvidenceState.NoRecentRunningBase && evidence.RecentLongestRunKm is not null and not 0))
            return (PreparationRunwayNumericMaterializationFailureCode.InvalidRecentLongestRun, "Recent longest run conflicts with its typed evidence state.");
        if (request.MaterializedWeeks.Count is < 3 or > 8)
            return (PreparationRunwayNumericMaterializationFailureCode.InvalidNumericMaterializationRequest, "Runway must contain 3..8 full weeks.");
        var ordered = request.MaterializedWeeks.OrderBy(w => w.RunwayWeekNumber).ToArray();
        // Phase 10K-GEN.29 -- this shape check previously consulted only
        // PreparationRunwayWeeklyShape.IsValid (the standard 1 KEY + 1 LONG +
        // N EASY shape), rejecting every real 2D Model B week (GEN.11 §1:
        // exactly 1 LONG_RUN + exactly one of {KEY_SESSION, EASY_SUPPORT}) --
        // the same "not every caller shape considered" defect family
        // GEN.10/GEN.20/GEN.27/GEN.28 already found repeated instances of,
        // here in the numeric-materialization layer specifically. Mirrors
        // PreparationRunwayWeekMaterializer.ValidateWeekCardinality's own
        // already-correct dual-shape check exactly. Zero-delta for every
        // pre-GEN.29 (non-2D) week: IsValid alone already accepted them, and
        // IsValidTwoDayModelB never matches a >2-slot week.
        if (!ordered.Select(w => w.RunwayWeekNumber).SequenceEqual(Enumerable.Range(1, ordered.Length)) ||
            ordered.Any(w => !PreparationRunwayWeeklyShape.IsValid(w.OrderedWorkoutSlots.Select(s => s.SlotRole).ToArray()) &&
                              !PreparationRunwayWeeklyShape.IsValidTwoDayModelB(w.OrderedWorkoutSlots.Select(s => s.SlotRole).ToArray())))
            return (PreparationRunwayNumericMaterializationFailureCode.InvalidNumericMaterializationRequest, "Structural weeks must be contiguous, canonical-shape (1 KEY + 1 LONG + N EASY, or the 2D Model B 1 LONG + exactly one of KEY/EASY) weeks.");
        if (!string.Equals(ordered[^1].BlockType?.ToString(), "PreSpecificTransition", StringComparison.Ordinal))
            return (PreparationRunwayNumericMaterializationFailureCode.InvalidNumericMaterializationRequest, "The terminal week must be PreSpecificTransition.");
        var target = request.CoreWeekOneTarget;
        if (!V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayCandidate(target.CandidateKey, target.CandidateVersion) ||
            target.WeeklyVolumeKm <= 0 || target.LongRunDistanceKm <= 0 ||
            target.OrderedSlots.Count(s => s.Role == PreparationRunwaySlotRole.KeySession) < 1 ||
            target.OrderedSlots.Count(s => s.Role == PreparationRunwaySlotRole.LongRun) != 1)
            return (PreparationRunwayNumericMaterializationFailureCode.CoreWeekOneTargetUnavailable, "A complete approved Core Week 1 target (>=1 KEY, exactly 1 LONG_RUN) is required.");
        if (Math.Abs(target.OrderedSlots.Sum(s => s.DistanceKm) - target.WeeklyVolumeKm) > request.Policy.ContinuityToleranceKm)
            return (PreparationRunwayNumericMaterializationFailureCode.CoreWeekOneTargetUnavailable, "Core Week 1 slot quantities do not equal its weekly total.");
        if (target.LongRunDistanceKm > target.WeeklyVolumeKm)
            return (PreparationRunwayNumericMaterializationFailureCode.CoreWeekOneTargetUnavailable, "Core Week 1 long run exceeds its weekly total.");
        return null;
    }

    private static double ResolveStartingWeeklyVolume(PreparationRunwayStartingLoadEvidence evidence, PreparationRunwayNumericPolicy policy) =>
        evidence.WeeklyVolumeState switch
        {
            PreparationRunwayLoadEvidenceState.Provided when evidence.RecentWeeklyVolumeKm is > 0 => Round(evidence.RecentWeeklyVolumeKm.Value, policy.RoundingIncrementKm),
            PreparationRunwayLoadEvidenceState.Provided => throw new InvalidOperationException("Provided recent weekly volume must be positive."),
            PreparationRunwayLoadEvidenceState.Missing when evidence.RecentWeeklyVolumeKm is null => policy.MissingWeeklyVolumeDefaultKm,
            PreparationRunwayLoadEvidenceState.NoRecentRunningBase when evidence.RecentWeeklyVolumeKm is null or 0 => policy.NoRecentRunningBaseDefaultKm,
            _ => throw new InvalidOperationException("Weekly-volume evidence state and value are inconsistent."),
        };

    private static double ResolveStartingLongRun(
        PreparationRunwayStartingLoadEvidence evidence,
        double weekly,
        PreparationRunwayNumericPolicy policy)
    {
        if (evidence.LongestRunState == PreparationRunwayLoadEvidenceState.Provided && evidence.RecentLongestRunKm is not > 0)
            throw new InvalidOperationException("Provided recent longest run must be positive.");
        if (evidence.LongestRunState == PreparationRunwayLoadEvidenceState.Missing && evidence.RecentLongestRunKm is not null)
            throw new InvalidOperationException("Missing longest-run evidence cannot carry a numeric value.");
        if (evidence.LongestRunState == PreparationRunwayLoadEvidenceState.NoRecentRunningBase && evidence.RecentLongestRunKm is not null and not 0)
            throw new InvalidOperationException("NoRecentRunningBase longest-run evidence cannot carry a positive value.");

        var lower = Round(weekly * policy.LongRunPreferredMinimumShare, policy.RoundingIncrementKm);
        var upper = Round(Math.Min(weekly * policy.LongRunPreferredMaximumShare, weekly * policy.LongRunHardCapShare), policy.RoundingIncrementKm);
        var selected = Round(weekly * policy.LongRunSelectionShare, policy.RoundingIncrementKm);
        if (evidence.LongestRunState == PreparationRunwayLoadEvidenceState.Provided)
            selected = Round(Math.Min(evidence.RecentLongestRunKm!.Value, selected), policy.RoundingIncrementKm);
        return Math.Min(upper, Math.Max(lower, selected));
    }

    private static IReadOnlyList<PreparationRunwayPrescribedSlot<TKey>> BuildSlots<TKey>(
        PreparationRunwayMaterializedWeek<TKey> week,
        FourDaySessionDistanceAllocation allocation,
        PreparationRunwayQuantityUnit unit,
        PreparationRunwayNumericPolicy policy) where TKey : notnull
    {
        var easyOrdinal = 0;
        return week.OrderedWorkoutSlots.OrderBy(s => s.SlotOrdinal).Select(slot =>
        {
            var quantity = slot.SlotRole switch
            {
                PreparationRunwaySlotRole.KeySession => allocation.KeySessionDistanceKm,
                PreparationRunwaySlotRole.LongRun => allocation.LongRunDistanceKm,
                PreparationRunwaySlotRole.EasySupport => allocation.EasySupportDistancesKm[easyOrdinal++],
                _ => throw new InvalidOperationException("Unsupported runway slot role."),
            };
            return new PreparationRunwayPrescribedSlot<TKey>(
                slot, quantity, unit,
                $"{policy.PolicyKey} v{policy.PolicyVersion}; V1_FOUR_DAY_SESSION_VOLUME_ALLOCATION_POLICY v1; deterministic residual to last EASY_SUPPORT");
        }).ToArray();
    }

    private static PreparationRunwayCoreContinuityAnalysis AnalyzeContinuity<TKey>(
        PreparationRunwayPrescribedWeek<TKey> final,
        PreparationRunwayCoreWeekOneNumericTarget core,
        PreparationRunwayNumericPolicy policy) where TKey : notnull
    {
        double Delta(PreparationRunwaySlotRole role, int ordinal)
        {
            var runway = final.OrderedSlots.Single(s => s.StructuralSlot.SlotRole == role && s.StructuralSlot.RoleOrdinal == ordinal).PlannedDistanceKm;
            var target = core.OrderedSlots.Single(s => s.Role == role && s.RoleOrdinal == ordinal).DistanceKm;
            return Round(target - runway, policy.RoundingIncrementKm);
        }

        var weekly = core.WeeklyVolumeKm - final.PlannedWeeklyVolumeKm;
        var longRun = core.LongRunDistanceKm - final.PlannedLongRunDistanceKm;

        var finalKeyCount = final.OrderedSlots.Count(s => s.StructuralSlot.SlotRole == PreparationRunwaySlotRole.KeySession);
        var finalEasyCount = final.OrderedSlots.Count(s => s.StructuralSlot.SlotRole == PreparationRunwaySlotRole.EasySupport);
        var coreKeyCount = core.OrderedSlots.Count(s => s.Role == PreparationRunwaySlotRole.KeySession);
        var coreEasyCount = core.OrderedSlots.Count(s => s.Role == PreparationRunwaySlotRole.EasySupport);
        // Phase 10K-FREQ.6D.7: per FREQ.6D.6 (approved product decision), Core-entry
        // compatibility is total weekly volume and long-run distance continuity --
        // NOT per-slot KEY/EASY role-count equality. Per-slot deltas are only
        // meaningful (and are still checked, byte-for-byte as before) when Runway's
        // final week and the Core Week 1 target share the exact same role
        // composition, e.g. every existing Intermediate 4D case. When the approved
        // structure legitimately redistributes KEY/EASY counts across the boundary
        // (Intermediate 5D: 1 KEY + 3 EASY -> 2 KEY + 2 EASY), only weekly/long-run
        // totals are the compatibility authority.
        var roleCompositionMatches = finalKeyCount == coreKeyCount && finalEasyCount == coreEasyCount;

        var key = roleCompositionMatches ? Delta(PreparationRunwaySlotRole.KeySession, 1) : 0d;
        var easy = roleCompositionMatches
            ? Enumerable.Range(1, finalEasyCount).Select(ordinal => Delta(PreparationRunwaySlotRole.EasySupport, ordinal)).ToArray()
            : Array.Empty<double>();
        var within = roleCompositionMatches
            ? new[] { weekly, longRun, key }.Concat(easy).All(v => Math.Abs(v) <= policy.ContinuityToleranceKm)
            : new[] { weekly, longRun }.All(v => Math.Abs(v) <= policy.ContinuityToleranceKm);
        return new PreparationRunwayCoreContinuityAnalysis(
            weekly, longRun, key, easy, final.OrderedSlots.Count == core.OrderedSlots.Count,
            within, policy.ContinuityToleranceKm,
            core.SourceProvenance + (roleCompositionMatches
                ? "; exact numeric equality at the undated boundary"
                : "; approved KEY/EASY redistribution at the undated boundary -- weekly volume and long-run distance are the compatibility authority (FREQ.6D.6)"));
    }

    private static string TrajectoryFor(string block, bool transition, double change) => transition
        ? "PRE_SPECIFIC_TRANSITION exact Core Week 1 maintenance bridge"
        : block switch
        {
            "Consistency" => change == 0 ? "CONSISTENCY cadence maintenance" : "CONSISTENCY conservative bounded progression",
            "GeneralEndurance" => change == 0 ? "GENERAL_ENDURANCE maintenance at Core target" : "GENERAL_ENDURANCE bounded direct-runway development",
            "AerobicStrength" => change == 0 ? "AEROBIC_STRENGTH total-volume maintenance" : "AEROBIC_STRENGTH modest bounded total-volume continuation; no interval dose prescribed",
            _ => "bounded target interpolation",
        };

    private static PreparationRunwayNumericMaterializationResult<TKey> Fail<TKey>(
        PreparationRunwayNumericMaterializationFailureCode code, string reason, IReadOnlyList<string> trace) where TKey : notnull =>
        PreparationRunwayNumericMaterializationResult<TKey>.Failure(code, reason, trace.ToArray());

    private static double Round(double value, double increment) =>
        Math.Round(value / increment, MidpointRounding.AwayFromZero) * increment;
}
