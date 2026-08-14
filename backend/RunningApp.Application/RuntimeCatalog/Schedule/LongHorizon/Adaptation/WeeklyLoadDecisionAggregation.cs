using RunningApp.Domain.Entities;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.5C -- Rev5 §7a "Multi-Week Window Aggregation" (B-weekly-summary
/// + B1 worst-week-wins + original-week lineage attribution). Two seams,
/// deliberately kept together in one small file since neither has any
/// reason to exist independently of the other:
///
/// <see cref="WeeklyWindowPartitioner"/> -- splits a rolling activation
/// window's flat session set back into its real, already-persisted
/// structural weeks (<see cref="LongHorizonRollingWeekState"/>), WITHOUT
/// reimplementing any completion/lineage/Superseded rule -- it only decides
/// which week's evidence bucket each row belongs to, per the frozen
/// original-week lineage rule (Rev5 §7a "Weekly Lineage Attribution Rule"):
/// a replacement session's evidence is always attributed to its ORIGINAL
/// root's structural week, never to the replacement's own physical week.
/// <see cref="WindowExecutionSummaryBuilder"/> and
/// <see cref="NextWindowLoadDecisionPolicy"/> remain completely unchanged
/// and unaware this partitioning exists -- they are simply invoked once per
/// resulting bucket instead of once against the whole window.
///
/// <see cref="WeeklyLoadDecisionAggregator"/> -- the single canonical
/// ordering authority for collapsing N per-week
/// <see cref="NextWindowAdaptationResult"/> values into one final window-
/// level result (B1 worst-week-wins, Rev4.1's frozen
/// Reduce &lt; Maintain &lt; ProgressAsPlanned severity ordering, applied
/// here and nowhere else -- see Rev5 §7a "Neden B1").
/// </summary>
internal static class WeeklyWindowPartitioner
{
    /// <summary>
    /// Groups the sessions of every real structural week in
    /// <paramref name="weeksInWindow"/> by which week OWNS each session's
    /// logical expectation (Rev5 §7a): a root session (AdaptedFromId ==
    /// null) always belongs to its own persisted week
    /// (<see cref="LongHorizonRollingSessionState.WeekStateId"/>); every
    /// descendant in its replacement chain -- regardless of which
    /// structural week that descendant's own row physically lives in --
    /// is attributed to that SAME root week. Returns one evidence bucket
    /// per week in <paramref name="weeksInWindow"/>, ordered by
    /// <see cref="LongHorizonRollingWeekState.GlobalWeek"/> ascending (the
    /// real, persisted structural-week identity -- no calendar/weekday
    /// arithmetic is used anywhere in this method).
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<LongHorizonRollingSessionState>> PartitionByStructuralWeekLineage(
        IReadOnlyList<LongHorizonRollingWeekState> weeksInWindow)
    {
        var allSessions = weeksInWindow.SelectMany(w => w.Sessions).ToList();
        var byId = new Dictionary<Guid, LongHorizonRollingSessionState>(allSessions.Count);
        foreach (var session in allSessions)
        {
            if (!byId.TryAdd(session.Id, session))
                throw new AdaptationLineageInvalidException($"Duplicate session id '{session.Id}' in window evidence.");
        }

        var buckets = weeksInWindow.ToDictionary(w => w.Id, _ => new List<LongHorizonRollingSessionState>());
        foreach (var session in allSessions)
        {
            var rootWeekStateId = ResolveOwningWeekStateId(session, byId);
            if (!buckets.TryGetValue(rootWeekStateId, out var bucket))
                throw new AdaptationLineageInvalidException(
                    $"Session '{session.Id}' resolves to owning week '{rootWeekStateId}', which is not one of the structural weeks supplied for this window.");
            bucket.Add(session);
        }

        return weeksInWindow
            .OrderBy(w => w.GlobalWeek)
            .Select(w => (IReadOnlyList<LongHorizonRollingSessionState>)buckets[w.Id])
            .ToList();
    }

    /// <summary>Walks AdaptedFromSessionId back to the original root (the
    /// same lineage concept WindowExecutionSummaryBuilder already walks
    /// forward -- Rev3.1 §5) and returns that root's OWN WeekStateId. A
    /// root session (AdaptedFromSessionId == null, including a Superseded
    /// one -- Superseded sessions are never themselves replacements) simply
    /// returns its own WeekStateId.</summary>
    private static Guid ResolveOwningWeekStateId(
        LongHorizonRollingSessionState session, IReadOnlyDictionary<Guid, LongHorizonRollingSessionState> byId)
    {
        var visited = new HashSet<Guid> { session.Id };
        var cursor = session;
        while (cursor.AdaptedFromSessionId is { } sourceId)
        {
            if (!visited.Add(sourceId))
                throw new AdaptationLineageInvalidException($"AdaptedFrom lineage cycle detected involving session '{sourceId}'.");
            if (!byId.TryGetValue(sourceId, out var source))
                throw new AdaptationLineageInvalidException($"Session '{cursor.Id}' has AdaptedFromSessionId '{sourceId}', which is not present in the supplied window.");
            cursor = source;
        }
        return cursor.WeekStateId;
    }
}

internal static class WeeklyLoadDecisionAggregator
{
    /// <summary>Rev4.1's frozen severity ordering (Rev5 §7a), and the ONLY
    /// place in the codebase this ordering is encoded as a comparable value
    /// -- see Rev5 §7a Section G: "There must be one authority if a new
    /// authority is needed." Not reused from
    /// <see cref="NextWindowNumericAnchorSelector"/>, which has no severity
    /// concept of its own (it only ever compares two numeric loads by
    /// WeeklyVolumeKm, an unrelated notion of "lower").</summary>
    private static int Severity(NextWindowLoadDecision decision) => decision switch
    {
        NextWindowLoadDecision.Reduce => 0,
        NextWindowLoadDecision.Maintain => 1,
        NextWindowLoadDecision.ProgressAsPlanned => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(decision), decision, "Unrecognized next-window load decision."),
    };

    /// <summary>B1 worst-week-wins (Rev5 §7a): the final window-level
    /// LoadDecision is the least-severe-ordering (i.e. numerically worst)
    /// of every real structural week's own, unmodified
    /// NextWindowLoadDecisionPolicy result; SafetyReviewRequired is a
    /// separate OR-aggregation across the same per-week results
    /// (mathematically identical to the existing single-window
    /// <c>HasSafetyFlag</c> Any(...) computation -- not a new decision, see
    /// Rev5 §7a). Deliberately recency-blind by construction: the ordering
    /// depends only on each week's own decision, never on which week is
    /// most recent (Rev5 §7a "Recency-blindness kasıtlı, kusur değil").</summary>
    public static NextWindowAdaptationResult AggregateWorstWeekWins(IReadOnlyList<NextWindowAdaptationResult> weeklyResults)
    {
        if (weeklyResults.Count == 0)
            throw new ArgumentException("At least one weekly decision is required for B1 aggregation.", nameof(weeklyResults));

        var worst = weeklyResults.Select(r => r.LoadDecision).MinBy(Severity);
        var anySafety = weeklyResults.Any(r => r.SafetyReviewRequired);
        return new NextWindowAdaptationResult(worst, anySafety);
    }
}
