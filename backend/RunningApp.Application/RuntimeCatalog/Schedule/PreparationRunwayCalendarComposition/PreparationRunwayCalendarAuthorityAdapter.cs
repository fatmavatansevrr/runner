using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;

/// <summary>
/// Captures the existing classifier decision and existing runway date
/// derivation in one typed value. The composer accepts no independently
/// supplied runway/core durations or boundary dates.
/// </summary>
internal static class PreparationRunwayCalendarAuthorityAdapter
{
    public const string PolicyKey = "CORE_HORIZON_PLUS_PREPARATION_RUNWAY_DATE_AUTHORITY";
    public const int PolicyVersion = 1;

    public static PreparationRunwayCalendarAuthority Create(
        CoreHorizonContext context,
        CoreHorizonDecision decision)
    {
        if (decision.MinimumCoreWeeks != context.MinimumCoreWeeks ||
            decision.PreferredCoreWeeks != context.PreferredCoreWeeks ||
            decision.MaximumCoreWeeks != context.MaximumCoreWeeks ||
            decision.AvailableDays != decision.AvailableFullWeeks * 7 + decision.LeadingPartialDays ||
            decision.LeadingPartialDays is < 0 or > 6 ||
            decision.Mode != CoreHorizonMode.PreparationRunwayPlusCore)
        {
            throw new InvalidOperationException("Supplied canonical horizon decision is inconsistent with its context or runway composition mode.");
        }

        var runway = PreparationRunwayDateAuthority.Derive(
            context.StartDate,
            decision.AvailableFullWeeks,
            decision.LeadingPartialDays,
            decision.PreferredCoreWeeks);
        var runwayStart = context.StartDate.AddDays(decision.LeadingPartialDays);
        if (runway.CoreStartDate.AddDays(decision.PreferredCoreWeeks * 7) != context.RaceDate)
        {
            throw new InvalidOperationException("Supplied horizon decision does not close exactly at RaceDate.");
        }

        return new PreparationRunwayCalendarAuthority(
            context, decision, runway, runwayStart, PolicyKey, PolicyVersion);
    }
}
