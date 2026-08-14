using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;

internal static class TenKPreparationRunwayCalendarCompositionPolicyFactory
{
    public const string PolicyKey = "TEN_K_PREPARATION_RUNWAY_CALENDAR_COMPOSITION_POLICY";
    public const int PolicyVersion = 1;
    public const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
    public const int CandidateVersion = 10;

    public static PreparationRunwayCalendarCompositionPolicy Build() => new(
        PolicyKey,
        PolicyVersion,
        CatalogCalendarDayMaterializerVersion.V1,
        CatalogCalendarAssignmentPolicy.RaceHardConstraint);
}
