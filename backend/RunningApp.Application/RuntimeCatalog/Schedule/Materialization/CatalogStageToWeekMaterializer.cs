namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Identifies which materializer implementation/version produced a
/// <see cref="GeneratedCatalogPlanSkeleton"/> (Phase 4F.2 Decision 10) — pure
/// internal provenance, never a public API surface.
/// </summary>
public static class CatalogStageToWeekMaterializerVersion
{
    public const string V1 = "CATALOG_STAGE_TO_WEEK_MATERIALIZER_V1";
}

/// <summary>
/// One stage (phase)'s authoritative week-count allocation, e.g.
/// <c>("BUILD", 4)</c>. Ordered lists of these are supplied by the caller —
/// the materializer never invents, resolves, or rebalances an allocation
/// (Phase 4F.2 Decision 2). "Stage" here is the catalog's week-allocation
/// granularity (<c>phaseKey</c>, e.g. "FOUNDATION"/"BUILD"/"RACE_SPECIFIC"/
/// "TAPER" in <c>templates/ten-k-master.v6.json</c>), matching the
/// granularity Phase 4F.1's own <see cref="Schedule.GeneratedCatalogWeekPayload.StageKey"/>
/// already established — not the catalog's finer-grained, workout-selection
/// <c>stageKey</c> (e.g. "GOAL_PACE_REHEARSAL" in
/// <c>workout-progressions/ten-k-workout-progression.v5.json</c>), which has
/// no week-count allocation of its own and is out of scope for this phase
/// (no workout selection occurs here).
/// </summary>
public sealed record CatalogStageWeekAllocation(string StageKey, int WeekCount);

/// <summary>
/// Backend Integration Phase 4F.2 — the strongly typed, immutable set of
/// already-resolved inputs <see cref="ICatalogStageToWeekMaterializer"/>
/// consumes. Every value here must already be authoritative when supplied:
/// the materializer never reselects a candidate, stage, fallback, or
/// eligibility decision, and never loads or mutates catalog files itself.
/// </summary>
public sealed class CatalogStageToWeekMaterializationContext
{
    /// <summary>The user-selected plan start date, preserved exactly (Phase 4F.1 Decision 5).</summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>The single frozen domain evaluation date for this materialization attempt — the materializer never reads the wall clock.</summary>
    public required DateOnly AsOfDate { get; init; }

    public required int PlannedWeekCount { get; init; }

    /// <summary>Expected structural session-slot count per full week (e.g. 4 for the TEN_K/INTERMEDIATE/4D pilot).</summary>
    public required int DaysPerWeek { get; init; }

    public required string CanonicalDistanceFamily { get; init; }

    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }

    public required IReadOnlyDictionary<string, PlanCatalogReference> DependencyVersions { get; init; }

    /// <summary>The authoritative, ordered stage (phase) sequence, already resolved by the caller — e.g. <c>["FOUNDATION","BUILD","RACE_SPECIFIC","TAPER"]</c>. The materializer validates against this; it never selects or reorders it.</summary>
    public required IReadOnlyList<string> SelectedStageSequence { get; init; }

    /// <summary>The authoritative week-count allocation, one entry per <see cref="SelectedStageSequence"/> element, in the same order.</summary>
    public required IReadOnlyList<CatalogStageWeekAllocation> StageWeekAllocations { get; init; }

    /// <summary>Exact (key, version) identity of the run-layout document these session slots were drawn from — for provenance only.</summary>
    public required PlanCatalogReference RunLayout { get; init; }

    /// <summary>
    /// The run-layout's own ordered structural role sequence (e.g.
    /// <c>["KEY_SESSION","EASY_SUPPORT","EASY_SUPPORT","LONG_RUN"]</c> for
    /// <c>RUN_LAYOUT_4D v2</c>). Supplied directly rather than loaded by the
    /// materializer, which has no catalog-loader dependency.
    /// </summary>
    public required IReadOnlyList<string> RunLayoutSlotRoles { get; init; }
}

/// <summary>Wraps a successfully materialized skeleton. <see cref="ICatalogStageToWeekMaterializer.Materialize"/> never returns a failure variant — every failure is a thrown, typed exception (see <c>CatalogStageToWeekMaterializationExceptions.cs</c>), never a partial or swallowed-error result.</summary>
public sealed class GeneratedCatalogWeekSkeletonResult
{
    public required GeneratedCatalogPlanSkeleton Skeleton { get; init; }
}

/// <summary>
/// Backend Integration Phase 4F.2 — transforms an already-resolved catalog
/// stage sequence and plan timing/layout inputs into a complete,
/// plan-relative week skeleton (numbered weeks, week date boundaries, stage
/// assignment per week, structural session-slot skeletons only). Does not
/// generate workout prescriptions, target distances/durations, pace,
/// segments, preferred-day calendar assignments, or planned volume — those
/// belong to a later materialization phase.
/// </summary>
public interface ICatalogStageToWeekMaterializer
{
    GeneratedCatalogWeekSkeletonResult Materialize(CatalogStageToWeekMaterializationContext context);
}

/// <inheritdoc cref="ICatalogStageToWeekMaterializer"/>
/// <remarks>
/// Deterministic and fully dependency-free by construction: this class has
/// no constructor dependencies at all (no database, clock, HTTP/request,
/// route-decider, resolver, or catalog-loader access is even possible — none
/// is injected). <see cref="Materialize"/> is a pure function of its
/// <see cref="CatalogStageToWeekMaterializationContext"/> argument.
/// </remarks>
public sealed class CatalogStageToWeekMaterializer : ICatalogStageToWeekMaterializer
{
    public GeneratedCatalogWeekSkeletonResult Materialize(CatalogStageToWeekMaterializationContext context)
    {
        ValidateStageSequence(context.SelectedStageSequence);
        ValidateStageAllocation(context.SelectedStageSequence, context.StageWeekAllocations, context.PlannedWeekCount);
        ValidateRunLayout(context.RunLayoutSlotRoles, context.DaysPerWeek);

        var weeks = BuildWeeks(context);

        var planEndDate = context.StartDate.AddDays(context.PlannedWeekCount * 7 - 1);

        var skeleton = new GeneratedCatalogPlanSkeleton
        {
            SchemaVersion = GeneratedCatalogPlanSkeleton.CurrentSchemaVersion,
            StartDate = context.StartDate,
            EndDate = planEndDate,
            PlannedWeekCount = context.PlannedWeekCount,
            DaysPerWeek = context.DaysPerWeek,
            CanonicalDistanceFamily = context.CanonicalDistanceFamily,
            CandidateKey = context.CandidateKey,
            CandidateVersion = context.CandidateVersion,
            DependencyVersions = context.DependencyVersions,
            Weeks = weeks,
            Provenance = new GeneratedCatalogPlanSkeletonProvenance
            {
                CandidateKey = context.CandidateKey,
                CandidateVersion = context.CandidateVersion,
                DependencyVersions = context.DependencyVersions,
                AsOfDate = context.AsOfDate,
                MaterializerVersion = CatalogStageToWeekMaterializerVersion.V1,
            },
        };

        return new GeneratedCatalogWeekSkeletonResult { Skeleton = skeleton };
    }

    /// <summary>Decision 2 (partial): the selected-stage-sequence input itself must be structurally sound before any allocation is checked against it.</summary>
    private static void ValidateStageSequence(IReadOnlyList<string> selectedStageSequence)
    {
        if (selectedStageSequence.Count == 0)
        {
            throw new CatalogStageSequenceInvalidException(
                "SelectedStageSequence must not be empty.");
        }

        if (selectedStageSequence.Any(string.IsNullOrWhiteSpace))
        {
            throw new CatalogStageSequenceInvalidException(
                "SelectedStageSequence must not contain a null/blank stage key.");
        }

        if (selectedStageSequence.Distinct().Count() != selectedStageSequence.Count)
        {
            throw new CatalogStageSequenceInvalidException(
                "SelectedStageSequence must not contain a duplicate stage key: " +
                string.Join(", ", selectedStageSequence) + ".");
        }
    }

    /// <summary>
    /// Decision 2: every planned week must belong to exactly one selected
    /// stage. Rejects: allocation order not matching the authoritative
    /// selected sequence (also catches "fallback stage not present in the
    /// selected sequence" and "out-of-order allocation" and "unknown stage
    /// key" — any allocation whose stage-key list isn't identical, in order,
    /// to <paramref name="selectedStageSequence"/> fails here); zero/negative
    /// stage allocation; duplicate stage key in the allocation list; and a
    /// week-count sum that does not exactly equal
    /// <paramref name="plannedWeekCount"/> (fewer or more). Never silently
    /// rebalances, rounds, or redistributes.
    /// </summary>
    private static void ValidateStageAllocation(
        IReadOnlyList<string> selectedStageSequence,
        IReadOnlyList<CatalogStageWeekAllocation> stageWeekAllocations,
        int plannedWeekCount)
    {
        if (plannedWeekCount <= 0)
        {
            throw new CatalogStageAllocationInvalidException(
                $"PlannedWeekCount must be positive, but was {plannedWeekCount}.");
        }

        var allocationStageKeys = stageWeekAllocations.Select(a => a.StageKey).ToList();

        if (allocationStageKeys.Distinct().Count() != allocationStageKeys.Count)
        {
            throw new CatalogStageAllocationInvalidException(
                "StageWeekAllocations must not contain a duplicate stage key: " +
                string.Join(", ", allocationStageKeys) + ".");
        }

        if (!allocationStageKeys.SequenceEqual(selectedStageSequence))
        {
            throw new CatalogStageAllocationInvalidException(
                "StageWeekAllocations' stage-key order must exactly match the authoritative " +
                $"SelectedStageSequence. Expected [{string.Join(", ", selectedStageSequence)}], " +
                $"got [{string.Join(", ", allocationStageKeys)}]. This also covers an out-of-order " +
                "allocation, an unknown stage key, and a fallback stage not already present in the " +
                "selected sequence -- the materializer never selects a fallback itself.");
        }

        foreach (var allocation in stageWeekAllocations)
        {
            if (allocation.WeekCount <= 0)
            {
                throw new CatalogStageAllocationInvalidException(
                    $"Stage '{allocation.StageKey}' has a non-positive WeekCount ({allocation.WeekCount}). " +
                    "Zero-length and negative stage allocations are rejected.");
            }
        }

        var allocatedTotal = stageWeekAllocations.Sum(a => a.WeekCount);
        if (allocatedTotal != plannedWeekCount)
        {
            throw new CatalogStageWeekCountMismatchException(
                $"Sum of StageWeekAllocations' WeekCount ({allocatedTotal}) does not equal " +
                $"PlannedWeekCount ({plannedWeekCount}). The materializer never rebalances, rounds, " +
                "or redistributes missing/excess weeks.");
        }
    }

    /// <summary>Decision 4: the run-layout slot-role sequence must be present and its length must exactly equal DaysPerWeek.</summary>
    private static void ValidateRunLayout(IReadOnlyList<string> runLayoutSlotRoles, int daysPerWeek)
    {
        if (runLayoutSlotRoles.Count == 0)
        {
            throw new CatalogRunLayoutInvalidException("RunLayoutSlotRoles must not be empty.");
        }

        if (runLayoutSlotRoles.Any(string.IsNullOrWhiteSpace))
        {
            throw new CatalogRunLayoutInvalidException("RunLayoutSlotRoles must not contain a null/blank role.");
        }

        if (runLayoutSlotRoles.Count != daysPerWeek)
        {
            throw new CatalogRunLayoutInvalidException(
                $"RunLayoutSlotRoles has {runLayoutSlotRoles.Count} entries, but DaysPerWeek is " +
                $"{daysPerWeek}. The run-layout's own slot count is the source of truth and must match.");
        }
    }

    /// <summary>Decision 1 (week dates) + Decision 2 (stage assignment) + Decision 3 (stage-relative indexing) + Decision 4/5 (structural slots, no rest slots).</summary>
    private static IReadOnlyList<GeneratedCatalogWeekSkeleton> BuildWeeks(CatalogStageToWeekMaterializationContext context)
    {
        var weeks = new List<GeneratedCatalogWeekSkeleton>(context.PlannedWeekCount);
        var weekNumber = 1;

        foreach (var allocation in context.StageWeekAllocations)
        {
            for (var stageWeekIndex = 1; stageWeekIndex <= allocation.WeekCount; stageWeekIndex++)
            {
                // Decision 1: Week N start = StartDate + (N-1)*7 days; end = start + 6 days.
                // No calendar-week normalization, no Monday alignment, no partial weeks.
                var weekStart = context.StartDate.AddDays((weekNumber - 1) * 7);
                var weekEnd = weekStart.AddDays(6);

                var slots = BuildSessionSlots(context, allocation.StageKey);

                if (slots.Count != context.DaysPerWeek)
                {
                    // Defensive: cannot occur given ValidateRunLayout already ran, but kept as an
                    // explicit, separately-named internal-consistency check per the error taxonomy.
                    throw new CatalogSessionSlotCountMismatchException(
                        $"Week {weekNumber} produced {slots.Count} session slots, expected {context.DaysPerWeek}.");
                }

                weeks.Add(new GeneratedCatalogWeekSkeleton
                {
                    WeekNumber = weekNumber,
                    StartDate = weekStart,
                    EndDate = weekEnd,
                    StageKey = allocation.StageKey,
                    StageWeekIndex = stageWeekIndex,
                    StageWeekCount = allocation.WeekCount,
                    SessionSlots = slots,
                    Provenance = new GeneratedCatalogWeekSkeletonProvenance
                    {
                        StageKey = allocation.StageKey,
                        SourcePhaseKey = allocation.StageKey,
                    },
                });

                weekNumber++;
            }
        }

        return weeks;
    }

    /// <summary>
    /// Decision 4/5: exactly <c>RunLayoutSlotRoles.Count</c> (== DaysPerWeek)
    /// structural running-session slots, in the layout's own declared order.
    /// No rest slot, no optional/recovery slot, no weekday/date, no final
    /// workout-type prescription — only the catalog's own immutable
    /// structural role string is carried through.
    /// </summary>
    private static IReadOnlyList<GeneratedCatalogSessionSlotSkeleton> BuildSessionSlots(
        CatalogStageToWeekMaterializationContext context, string stageKey)
    {
        var slots = new List<GeneratedCatalogSessionSlotSkeleton>(context.RunLayoutSlotRoles.Count);
        var roleOccurrenceCounts = new Dictionary<string, int>();

        for (var i = 0; i < context.RunLayoutSlotRoles.Count; i++)
        {
            var role = context.RunLayoutSlotRoles[i];
            roleOccurrenceCounts.TryGetValue(role, out var priorCount);
            var occurrenceIndex = priorCount + 1;
            roleOccurrenceCounts[role] = occurrenceIndex;

            slots.Add(new GeneratedCatalogSessionSlotSkeleton
            {
                SlotOrderInWeek = i + 1,
                LayoutSlotKey = $"{role}_{occurrenceIndex}",
                StructuralRole = role,
                Provenance = new GeneratedCatalogSessionSlotSkeletonProvenance
                {
                    SourceStageKey = stageKey,
                    SourceLayout = context.RunLayout,
                },
            });
        }

        return slots;
    }
}
