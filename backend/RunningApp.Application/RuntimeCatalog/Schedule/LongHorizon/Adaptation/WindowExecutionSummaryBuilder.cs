using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.1 -- the single canonical authority for adaptation execution
/// evidence (Rev3.1 §6, §13.2). No other adaptation service may
/// independently compute completed/adherence counts with its own naive
/// query -- everything downstream (NextWindowLoadDecisionPolicy included)
/// consumes only the <see cref="WindowExecutionSummary"/> this type builds.
///
/// Rev3.1 §5 lineage rule: a replacement session and its AdaptedFrom chain
/// represent ONE logical expected session. Rev3.1 §6/Rev3.1-note
/// denominator rule: a Superseded session remains part of the ORIGINAL
/// expectation denominator -- it is neither Completed nor an unrecovered
/// NotToday, it simply sits in its own neutral category
/// (SupersededByAdaptationCount), and that count is informational-only and
/// must never influence NextWindowLoadDecision on its own.
/// </summary>
internal static class WindowExecutionSummaryBuilder
{
    public static WindowExecutionSummary Build(IReadOnlyList<LogicalSessionEvidence> sessions)
    {
        var byId = new Dictionary<Guid, LogicalSessionEvidence>(sessions.Count);
        foreach (var session in sessions)
        {
            if (!byId.TryAdd(session.Id, session))
                throw new AdaptationLineageInvalidException($"Duplicate session id '{session.Id}' in window evidence.");
        }

        // Fail-fast: at most one direct replacement child per source session.
        var childrenBySource = new Dictionary<Guid, List<LogicalSessionEvidence>>();
        foreach (var session in sessions)
        {
            if (session.AdaptedFromId is not { } sourceId)
                continue;

            if (!byId.ContainsKey(sourceId))
                throw new AdaptationLineageInvalidException($"Session '{session.Id}' has AdaptedFromId '{sourceId}', which is not present in the supplied evidence.");

            if (!childrenBySource.TryGetValue(sourceId, out var children))
            {
                children = [];
                childrenBySource[sourceId] = children;
            }
            children.Add(session);

            if (children.Count > 1)
            {
                throw new AdaptationLineageInvalidException(
                    $"Session '{sourceId}' has more than one direct replacement child ({string.Join(", ", children.Select(c => c.Id))}). " +
                    "One trigger logical session may have at most one active committed replacement lineage.");
            }
        }

        // Fail-fast: no cycle in the AdaptedFrom graph.
        foreach (var session in sessions)
        {
            var visited = new HashSet<Guid> { session.Id };
            var cursor = session;
            while (cursor.AdaptedFromId is { } sourceId)
            {
                if (!visited.Add(sourceId))
                    throw new AdaptationLineageInvalidException($"AdaptedFrom lineage cycle detected involving session '{sourceId}'.");
                cursor = byId[sourceId];
            }
        }

        // Roots = original logical expectations. A root is any session that
        // is not itself a replacement (AdaptedFromId == null); its
        // PlanningStatus (Active/Superseded) and ExecutionOutcome do not
        // affect whether it counts as a root.
        var roots = sessions.Where(s => s.AdaptedFromId is null).ToList();

        var expectedCount = roots.Count;
        var effectiveCompleted = 0;
        var supersededCount = 0;
        var unrecoveredNotToday = 0;
        // Phase 10K-FREQ.4: count-based, mirroring EasyExpectedCount/
        // EasyCompletedCount below (already the correct N-role pattern) --
        // replaces the pre-FREQ.4 bool keyExpected/AND-accumulated
        // keyCompleted, which collapsed a multi-KEY week into a single
        // lossy flag (FREQ.3 §H.1).
        var keyExpectedCount = 0;
        var keyCompletedCount = 0;
        var longExpected = false;
        var longCompleted = true;
        var easyExpected = 0;
        var easyCompleted = 0;
        var hasSafetyFlag = sessions.Any(s =>
            s.ExecutionOutcome == LongHorizonRollingSessionOutcomeStatus.NotToday &&
            s.NotTodayReason is { } reason &&
            ReasonClassificationPolicy.TriggersSafetyFlag(reason));

        foreach (var root in roots)
        {
            if (root.PlanningStatus == SessionPlanningStatus.Superseded)
            {
                supersededCount++;
                if (root.Role == PreparationRunwaySlotRole.EasySupport)
                    easyExpected++;
                else if (root.Role == PreparationRunwaySlotRole.KeySession)
                    keyExpectedCount++;
                else if (root.Role == PreparationRunwaySlotRole.LongRun)
                    longExpected = true;
                // Superseded roots contribute to their role's expected
                // count/denominator but are neither Completed nor
                // UnrecoveredNotToday -- their own neutral category.
                continue;
            }

            var terminalOutcome = FollowLineageToTerminalOutcome(root, byId);
            var isEffectivelyCompleted = terminalOutcome == LongHorizonRollingSessionOutcomeStatus.Completed;
            var isUnrecoveredNotToday = terminalOutcome == LongHorizonRollingSessionOutcomeStatus.NotToday;

            if (isEffectivelyCompleted)
                effectiveCompleted++;
            if (isUnrecoveredNotToday)
                unrecoveredNotToday++;

            switch (root.Role)
            {
                case PreparationRunwaySlotRole.KeySession:
                    keyExpectedCount++;
                    if (isEffectivelyCompleted)
                        keyCompletedCount++;
                    break;
                case PreparationRunwaySlotRole.LongRun:
                    longExpected = true;
                    longCompleted &= isEffectivelyCompleted;
                    break;
                case PreparationRunwaySlotRole.EasySupport:
                    easyExpected++;
                    if (isEffectivelyCompleted)
                        easyCompleted++;
                    break;
            }
        }

        return new WindowExecutionSummary(
            ExpectedSessionCount: expectedCount,
            EffectiveCompletedCount: effectiveCompleted,
            KeySessionExpectedCount: keyExpectedCount,
            KeySessionCompletedCount: keyCompletedCount,
            LongRunExpected: longExpected,
            LongRunCompleted: longExpected && longCompleted,
            EasyExpectedCount: easyExpected,
            EasyCompletedCount: easyCompleted,
            UnrecoveredNotTodayCount: unrecoveredNotToday,
            SupersededByAdaptationCount: supersededCount,
            HasSafetyFlag: hasSafetyFlag);
    }

    /// <summary>Walks a root session forward through its (validated, acyclic,
    /// single-child) replacement chain to the final leaf and returns that
    /// leaf's ExecutionOutcome -- the "final effective execution" Rev3.1 §6
    /// describes. A leaf still Planned (not yet due) is neither Completed
    /// nor an unrecovered NotToday; it is simply pending.</summary>
    private static LongHorizonRollingSessionOutcomeStatus FollowLineageToTerminalOutcome(
        LogicalSessionEvidence root, IReadOnlyDictionary<Guid, LogicalSessionEvidence> byId)
    {
        var childrenBySource = byId.Values
            .Where(s => s.AdaptedFromId is not null)
            .GroupBy(s => s.AdaptedFromId!.Value)
            .ToDictionary(g => g.Key, g => g.Single());

        var cursor = root;
        while (childrenBySource.TryGetValue(cursor.Id, out var child))
            cursor = child;

        return cursor.ExecutionOutcome;
    }
}
