using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.8C Part 3 — maps checkpoint-validated rolling evidence to the
/// exact legacy <see cref="GeneratePreviewRequest"/>/<see cref="ResolverInputSnapshot"/>
/// shape the real, unchanged Core generation pipeline requires (Phase 4K.5A's
/// approved mapping). Onboarding evidence is never read here -- both
/// <see cref="RecentWeeklyVolumeKm"/>-equivalent fields are always populated
/// from the supplied <see cref="ValidatedSustainableLoad"/>.
/// </summary>
internal static class LongHorizonRollingCoreGenerationInputAdapter
{
    public static (GeneratePreviewRequest PreviewRequest, ResolverInputSnapshot ResolverInput) Build(
        ValidatedSustainableLoad validatedLoad,
        int? exactCompletedFrequency,
        DateOnly startDate,
        DateOnly raceDate,
        int? targetFinishTimeSeconds,
        TargetFinishTimeSource? targetFinishTimeSource,
        RecentRaceInput? recentRace,
        IReadOnlyList<DayOfWeek> preferredDays,
        DayOfWeek longRunDay,
        int daysPerWeek = 4)
    {
        if (validatedLoad.ValidationStatus != LongHorizonValidationStatus.Valid
            || validatedLoad.WeeklyVolumeKm is null || validatedLoad.LongRunKm is null)
        {
            throw new LongHorizonJitContextInvalidException(
                "LongHorizonRollingCoreGenerationInputAdapter.Build requires a Valid ValidatedSustainableLoad.");
        }

        var weekdays = preferredDays.Select(ToWeekday).ToArray();
        var longRunWeekday = ToWeekday(longRunDay);

        var previewRequest = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = daysPerWeek,
            Unit = DistanceUnit.Km,
            StartDate = startDate,
            RaceDate = raceDate,
            TargetFinishTimeSeconds = targetFinishTimeSeconds,
            TargetFinishTimeSource = targetFinishTimeSource,
            PreferredDays = weekdays,
            LongRunDay = longRunWeekday,
            RecentWeeklyVolumeKm = validatedLoad.WeeklyVolumeKm,
            RecentLongestRunKm = validatedLoad.LongRunKm,
            RecentRunsPerWeek = exactCompletedFrequency,
            RecentRace = recentRace,
        };

        var resolverInput = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10,
            CanonicalDistanceFamily = "TEN_K",
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10,
            StartDate = startDate,
            RaceDate = raceDate,
            TargetFinishTimeSeconds = targetFinishTimeSeconds,
            TargetFinishTimeSource = targetFinishTimeSource,
            DaysPerWeek = daysPerWeek,
            PreferredDays = weekdays,
            LongRunDay = longRunWeekday,
            Level = RunningBackground.Intermediate,
            RecentWeeklyVolumeKm = validatedLoad.WeeklyVolumeKm,
            RecentLongestRunKm = validatedLoad.LongRunKm,
            RecentRunsPerWeek = exactCompletedFrequency,
            RecentRaceDistanceKm = recentRace is null ? null : 10,
            RecentRaceFinishTimeSeconds = recentRace?.FinishTimeSeconds,
            RecentRaceDate = recentRace?.RaceDate,
        };

        return (previewRequest, resolverInput);
    }

    private static Weekday ToWeekday(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Weekday.Mon,
        DayOfWeek.Tuesday => Weekday.Tue,
        DayOfWeek.Wednesday => Weekday.Wed,
        DayOfWeek.Thursday => Weekday.Thu,
        DayOfWeek.Friday => Weekday.Fri,
        DayOfWeek.Saturday => Weekday.Sat,
        DayOfWeek.Sunday => Weekday.Sun,
        _ => throw new LongHorizonJitContextInvalidException($"Unsupported DayOfWeek {day}."),
    };
}
