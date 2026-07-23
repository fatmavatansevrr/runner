namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

// Backend Integration Phase 4F.5 — typed internal calendar-assignment errors.
// None is registered in GlobalExceptionHandler as of this phase; when wired
// dark into CatalogPreviewGenerator, each is caught by exact type and
// re-thrown wrapped as the pre-existing PlanPreviewGenerationFailedException
// (see CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton), never
// swallowed, never downgraded to a default weekday pattern or a partial
// dated skeleton.
//
// Backend Integration Phase 4F.5.1 (independent-audit gap fix): every type
// below is now production-reachable. CatalogDatedSkeletonInvalidException in
// particular is thrown by CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton
// itself when DatedGeneratedCatalogPlanSkeletonValidator.Validate returns an
// invalid result -- the validator is now actually invoked in the dark path
// (previously it was exercised only by tests). The former generic catch-all
// CatalogCalendarAssignmentFailedException was removed: it had no distinct
// trigger condition not already covered by one of the specific exceptions
// below, and inventing an artificial throw path for it was rejected in favor
// of removing unjustified dead code (see PHASE4F_5_1_PRODUCTION_VALIDATOR_WIRING.md).

/// <summary>Thrown when a race-plan context has no PreferredDays at all. Error code equivalent: CATALOG_PREFERRED_DAYS_REQUIRED.</summary>
internal sealed class CatalogPreferredDaysRequiredException : Exception
{
    public CatalogPreferredDaysRequiredException(string message) : base(message) { }
}

/// <summary>Thrown when PreferredDays does not contain exactly the pilot's required count (4). Error code equivalent: CATALOG_PREFERRED_DAY_COUNT_INVALID.</summary>
internal sealed class CatalogPreferredDayCountInvalidException : Exception
{
    public CatalogPreferredDayCountInvalidException(string message) : base(message) { }
}

/// <summary>Thrown when PreferredDays contains a duplicate weekday (either as a raw duplicate string/abbreviation or as a duplicate normalized DayOfWeek). Error code equivalent: CATALOG_PREFERRED_DAYS_DUPLICATED.</summary>
internal sealed class CatalogPreferredDaysDuplicatedException : Exception
{
    public CatalogPreferredDaysDuplicatedException(string message) : base(message) { }
}

/// <summary>Thrown when a race-plan context has no LongRunDayPreference. Error code equivalent: CATALOG_LONG_RUN_DAY_REQUIRED.</summary>
internal sealed class CatalogLongRunDayRequiredException : Exception
{
    public CatalogLongRunDayRequiredException(string message) : base(message) { }
}

/// <summary>Thrown when LongRunDayPreference does not belong to PreferredDays. Error code equivalent: CATALOG_LONG_RUN_DAY_NOT_PREFERRED.</summary>
internal sealed class CatalogLongRunDayNotPreferredException : Exception
{
    public CatalogLongRunDayNotPreferredException(string message) : base(message) { }
}

/// <summary>Thrown when the source skeleton's structural role composition does not match the pilot's expected shape (wrong DaysPerWeek, wrong per-role counts, a REST/OPTIONAL/unsupported role present, or inconsistent week ranges). Error code equivalent: CATALOG_CALENDAR_ROLE_STRUCTURE_INVALID.</summary>
internal sealed class CatalogCalendarRoleStructureInvalidException : Exception
{
    public CatalogCalendarRoleStructureInvalidException(string message) : base(message) { }
}

/// <summary>Thrown when no deterministic full-plan assignment can satisfy the KEY_SESSION/LONG_RUN separation invariant (same-week or cross-week) for the given PreferredDays/LongRunDayPreference combination. Error code equivalent: CATALOG_PREFERRED_DAY_CONFIGURATION_UNSAFE.</summary>
internal sealed class CatalogPreferredDayConfigurationUnsafeException : Exception
{
    public CatalogPreferredDayConfigurationUnsafeException(string message) : base(message) { }
}

/// <summary>Thrown when a materialized <see cref="DatedGeneratedCatalogPlanSkeleton"/> fails <see cref="DatedGeneratedCatalogPlanSkeletonValidator"/> validation. Error code equivalent: CATALOG_DATED_SKELETON_INVALID.</summary>
internal sealed class CatalogDatedSkeletonInvalidException : Exception
{
    public CatalogDatedSkeletonInvalidException(string message) : base(message) { }
}
