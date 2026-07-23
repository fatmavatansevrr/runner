using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Binding;

internal sealed class BoundCatalogPlanValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<string> Errors { get; init; }

    public static BoundCatalogPlanValidationResult Valid() => new() { IsValid = true, Errors = Array.Empty<string>() };
    public static BoundCatalogPlanValidationResult Invalid(IReadOnlyList<string> errors) => new() { IsValid = false, Errors = errors };
}

internal interface IBoundCatalogPlanValidator
{
    BoundCatalogPlanValidationResult Validate(BoundCatalogPlan plan, DatedGeneratedCatalogPlanSkeleton datedSkeleton);
}

/// <summary>Backend Integration Phase 4F.6B — output validator for <see cref="BoundCatalogPlan"/> (Section 12). Pure, deterministic re-check against the dated skeleton it was built from — never re-binds, never mutates its input.</summary>
internal sealed class BoundCatalogPlanValidator : IBoundCatalogPlanValidator
{
    public BoundCatalogPlanValidationResult Validate(BoundCatalogPlan plan, DatedGeneratedCatalogPlanSkeleton datedSkeleton)
    {
        var errors = new List<string>();

        var totalSlots = datedSkeleton.Weeks.Sum(w => w.SessionSlots.Count);
        var totalSessions = plan.Weeks.Sum(w => w.Sessions.Count);
        if (totalSlots != totalSessions)
        {
            errors.Add($"Total slot count mismatch: dated skeleton has {totalSlots}, bound plan has {totalSessions}.");
        }

        foreach (var week in plan.Weeks)
        {
            var roleCounts = week.Sessions.GroupBy(s => s.StructuralRole).ToDictionary(g => g.Key, g => g.Count());
            var keySessionCount = roleCounts.GetValueOrDefault("KEY_SESSION");
            var easySupportCount = roleCounts.GetValueOrDefault("EASY_SUPPORT");
            var longRunCount = roleCounts.GetValueOrDefault("LONG_RUN");

            if (keySessionCount != 1 || easySupportCount != 2 || longRunCount != 1)
            {
                errors.Add(
                    $"Week {week.WeekNumber} does not have the expected 1 KEY_SESSION / 2 EASY_SUPPORT / 1 LONG_RUN shape " +
                    $"(found KEY_SESSION={keySessionCount}, EASY_SUPPORT={easySupportCount}, LONG_RUN={longRunCount}).");
            }

            foreach (var session in week.Sessions)
            {
                if (string.IsNullOrWhiteSpace(session.WorkoutDefinitionKey) || session.WorkoutDefinitionVersion <= 0)
                {
                    errors.Add($"Week {week.WeekNumber} session (role={session.StructuralRole}) has no resolved workout key/version.");
                }

                var expectedMode = session.StructuralRole switch
                {
                    "KEY_SESSION" => CatalogWorkoutBindingMode.StageControlled,
                    "EASY_SUPPORT" or "LONG_RUN" => CatalogWorkoutBindingMode.FixedDefault,
                    _ => (CatalogWorkoutBindingMode?)null,
                };

                if (expectedMode is null)
                {
                    errors.Add($"Week {week.WeekNumber} session has an unrecognized structural role '{session.StructuralRole}'.");
                }
                else if (session.BindingMode != expectedMode)
                {
                    errors.Add($"Week {week.WeekNumber} session (role={session.StructuralRole}) has binding mode {session.BindingMode}, expected {expectedMode}.");
                }

                if (session.StructuralRole is "EASY_SUPPORT" or "LONG_RUN")
                {
                    if (session.ProgressionStageKey is not null)
                    {
                        errors.Add($"Week {week.WeekNumber} session (role={session.StructuralRole}) unexpectedly carries a ProgressionStageKey ('{session.ProgressionStageKey}').");
                    }

                    var expectedKey = session.StructuralRole == "EASY_SUPPORT"
                        ? V1CatalogWorkoutRoleBindingPolicy.EasySupportFixedDefaultWorkoutKey
                        : V1CatalogWorkoutRoleBindingPolicy.LongRunFixedDefaultWorkoutKey;

                    if (session.WorkoutDefinitionKey != expectedKey)
                    {
                        errors.Add($"Week {week.WeekNumber} session (role={session.StructuralRole}) is bound to '{session.WorkoutDefinitionKey}', expected fixed default '{expectedKey}'.");
                    }
                }

                if (session.StructuralRole == "KEY_SESSION" && session.ProgressionStageKey is null)
                {
                    errors.Add($"Week {week.WeekNumber} KEY_SESSION session has no ProgressionStageKey.");
                }
            }
        }

        if (plan.Trace.Steps.Count != totalSessions)
        {
            errors.Add($"Trace step count ({plan.Trace.Steps.Count}) does not match bound session count ({totalSessions}).");
        }

        return errors.Count == 0 ? BoundCatalogPlanValidationResult.Valid() : BoundCatalogPlanValidationResult.Invalid(errors);
    }
}
