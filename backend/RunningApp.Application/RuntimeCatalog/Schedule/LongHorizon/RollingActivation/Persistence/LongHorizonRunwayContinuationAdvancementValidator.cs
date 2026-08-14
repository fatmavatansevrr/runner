namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Thrown when a JIT composition claims success but its resulting window
/// does not actually advance the durable lifecycle boundary. Fail-closed:
/// this must never be silently persisted.
/// </summary>
internal sealed class LongHorizonRunwayContinuationAdvancementViolationException(string message)
    : LongHorizonRollingContractException("RunwayContinuationAdvancementViolation", message);

/// <summary>
/// Phase 4L.2C Part 11 -- guards the single invariant this phase exists to
/// resolve: "a committed Runway continuation must advance the durable
/// lifecycle boundary." Invoked in the real restart-continuation path
/// (LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync)
/// immediately before persistence, on every successful composition -- never
/// only in tests. Never recomputes the window itself; only validates the
/// runtime's own already-selected result against the previously-durable
/// window it is meant to supersede.
/// </summary>
internal static class LongHorizonRunwayContinuationAdvancementValidator
{
    public static void Validate(RollingNumericActivationWindow previousWindow, RollingNumericActivationWindow newWindow)
    {
        if (newWindow.StartGlobalWeek == previousWindow.StartGlobalWeek && newWindow.EndGlobalWeek == previousWindow.EndGlobalWeek)
            throw new LongHorizonRunwayContinuationAdvancementViolationException(
                $"Runway continuation did not advance: new window [{newWindow.StartGlobalWeek},{newWindow.EndGlobalWeek}] is identical to the previous window.");

        if (newWindow.StartGlobalWeek <= previousWindow.StartGlobalWeek)
            throw new LongHorizonRunwayContinuationAdvancementViolationException(
                $"Runway continuation regressed: new window start {newWindow.StartGlobalWeek} did not advance past previous start {previousWindow.StartGlobalWeek}.");

        if (newWindow.StartGlobalWeek != previousWindow.EndGlobalWeek + 1)
            throw new LongHorizonRunwayContinuationAdvancementViolationException(
                $"Runway continuation left a gap or overlap: new window starts at {newWindow.StartGlobalWeek}, expected exactly {previousWindow.EndGlobalWeek + 1} (immediately after the previous window's end).");

        foreach (var week in newWindow.Weeks)
        {
            if (previousWindow.Weeks.Any(w => w.GlobalWeekNumber == week.GlobalWeekNumber))
                throw new LongHorizonRunwayContinuationAdvancementViolationException(
                    $"Runway continuation re-activated global week {week.GlobalWeekNumber}, which the previous window already activated.");
        }
    }
}
