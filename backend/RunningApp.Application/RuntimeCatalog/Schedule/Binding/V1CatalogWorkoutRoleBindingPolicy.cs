namespace RunningApp.Application.RuntimeCatalog.Schedule.Binding;

/// <summary>How a structural role's exact workout definition is determined (Section 4).</summary>
internal enum CatalogWorkoutBindingMode
{
    /// <summary>The role resolves to one explicitly configured default workout key, regardless of the eligible-workout list's order or size.</summary>
    FixedDefault,

    /// <summary>The role resolves to whichever workout candidate the assigned fine-grained progression stage explicitly references.</summary>
    StageControlled,
}

/// <summary>
/// Backend Integration Phase 4F.6B — the single, explicit, typed, versioned V1 role-binding
/// policy (D-C07/D-C08/D-C09, Step C). This is the ONE place `EASY_STANDARD`/`LONG_RUN_STANDARD`
/// appear as literal workout keys anywhere in this binding subsystem — every consumer reads
/// them from here, never re-declares them. Both fixed defaults are explicitly documented
/// (Step C) as TEMPORARY_V1_SIMPLIFICATIONS, not permanent architectural restrictions; a
/// future multi-workout role requires an explicit, new, versioned policy — never an
/// unversioned in-place change to this one.
/// </summary>
internal static class V1CatalogWorkoutRoleBindingPolicy
{
    public const string PolicyKey = "V1_CATALOG_WORKOUT_ROLE_BINDING_POLICY";
    public const int PolicyVersion = 1;

    public const string KeySessionRole = "KEY_SESSION";
    public const string EasySupportRole = "EASY_SUPPORT";
    public const string LongRunRole = "LONG_RUN";

    /// <summary>D-C07: EASY_SUPPORT's V1 fixed default.</summary>
    public const string EasySupportFixedDefaultWorkoutKey = "EASY_STANDARD";

    /// <summary>D-C08: LONG_RUN's V1 fixed default.</summary>
    public const string LongRunFixedDefaultWorkoutKey = "LONG_RUN_STANDARD";

    /// <summary>Resolves the binding mode for a given structural role. Unknown roles are rejected by the caller, not defaulted here.</summary>
    public static CatalogWorkoutBindingMode ModeFor(string structuralRole) => structuralRole switch
    {
        EasySupportRole => CatalogWorkoutBindingMode.FixedDefault,
        LongRunRole => CatalogWorkoutBindingMode.FixedDefault,
        KeySessionRole => CatalogWorkoutBindingMode.StageControlled,
        _ => throw new CatalogWorkoutBindingUnknownStructuralRoleException($"'{structuralRole}' has no V1 role-binding policy entry."),
    };

    /// <summary>Resolves the fixed-default workout key for a FixedDefault role. Throws for any role that is not FixedDefault.</summary>
    public static string FixedDefaultWorkoutKeyFor(string structuralRole) => structuralRole switch
    {
        EasySupportRole => EasySupportFixedDefaultWorkoutKey,
        LongRunRole => LongRunFixedDefaultWorkoutKey,
        _ => throw new CatalogWorkoutBindingUnknownStructuralRoleException($"'{structuralRole}' has no fixed-default workout key — it is not a FixedDefault role."),
    };
}
