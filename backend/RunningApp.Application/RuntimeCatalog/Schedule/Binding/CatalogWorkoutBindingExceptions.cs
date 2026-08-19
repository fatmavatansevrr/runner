namespace RunningApp.Application.RuntimeCatalog.Schedule.Binding;

/// <summary>
/// Backend Integration Phase 4F.6B — typed failures for the exact workout-definition binder.
/// None is caught/swallowed by the binder itself — the dark wiring caller
/// (<see cref="PreviewRouting.CatalogPreviewGenerator"/>) maps all of them onto the existing
/// <see cref="Exceptions.PlanPreviewGenerationFailedException"/> taxonomy, exactly as it
/// already does for the Phase 4F.3/4F.5/4F.6A exception sets.
/// </summary>
internal abstract class CatalogWorkoutBindingException : Exception
{
    protected CatalogWorkoutBindingException(string message) : base(message) { }
}

/// <summary>A structural role string has no entry in <see cref="V1CatalogWorkoutRoleBindingPolicy"/>.</summary>
internal sealed class CatalogWorkoutBindingUnknownStructuralRoleException : CatalogWorkoutBindingException
{
    public CatalogWorkoutBindingUnknownStructuralRoleException(string message) : base(message) { }
}

/// <summary>A KEY_SESSION session's dated week has no matching entry (by WeekNumber) in the Phase 4F.6A stage schedule, or that entry has a missing/blank ProgressionStageKey.</summary>
internal sealed class CatalogWorkoutBindingMissingProgressionStageException : CatalogWorkoutBindingException
{
    public CatalogWorkoutBindingMissingProgressionStageException(string message) : base(message) { }
}

/// <summary>The effective ProgressionStageKey assigned by Phase 4F.6A does not match any stage in the loaded workout-progression artifact.</summary>
internal sealed class CatalogWorkoutBindingUnknownProgressionStageException : CatalogWorkoutBindingException
{
    public CatalogWorkoutBindingUnknownProgressionStageException(string message) : base(message) { }
}

/// <summary>The resolved progression stage declares zero workout-candidate references.</summary>
internal sealed class CatalogWorkoutBindingMissingCandidateReferenceException : CatalogWorkoutBindingException
{
    public CatalogWorkoutBindingMissingCandidateReferenceException(string message) : base(message) { }
}

/// <summary>The resolved progression stage declares more than one workout-candidate reference (V1 does not implement a multi-workout selection policy — Section 10).</summary>
internal sealed class CatalogWorkoutBindingAmbiguousCandidateException : CatalogWorkoutBindingException
{
    public CatalogWorkoutBindingAmbiguousCandidateException(string message) : base(message) { }
}

/// <summary>The referenced (or fixed-default) workout key/version could not be loaded from the resolved catalog bundle.</summary>
internal sealed class CatalogWorkoutBindingCandidateNotFoundException : CatalogWorkoutBindingException
{
    public CatalogWorkoutBindingCandidateNotFoundException(string message) : base(message) { }
}

/// <summary>The resolved workout definition's own (key, version) does not match the version requested by the candidate reference or fixed default.</summary>
internal sealed class CatalogWorkoutBindingVersionMismatchException : CatalogWorkoutBindingException
{
    public CatalogWorkoutBindingVersionMismatchException(string message) : base(message) { }
}

/// <summary>The resolved workout definition is not present in the candidate's resolved dependency closure (level-modifier eligible workouts ∪ progression-stage candidates).</summary>
internal sealed class CatalogWorkoutBindingOutsideDependencyClosureException : CatalogWorkoutBindingException
{
    public CatalogWorkoutBindingOutsideDependencyClosureException(string message) : base(message) { }
}

/// <summary>The workout definition fails a content-level validation check (eligible phase, allowed prescription modes, lifecycle status).</summary>
internal sealed class CatalogWorkoutBindingDefinitionInvalidException : CatalogWorkoutBindingException
{
    public CatalogWorkoutBindingDefinitionInvalidException(string message) : base(message) { }
}

/// <summary>The generated <see cref="BoundCatalogPlan"/> failed <see cref="IBoundCatalogPlanValidator"/>'s own output validation.</summary>
internal sealed class CatalogWorkoutBindingPlanInvalidException : CatalogWorkoutBindingException
{
    public CatalogWorkoutBindingPlanInvalidException(string message) : base(message) { }
}

/// <summary>Backend Integration Phase 10K-FREQ.6D.4D Split A: the resolved progression-stage schedule assigns more than one stage to the same (WeekNumber, LaneOrdinal) pair.</summary>
internal sealed class CatalogWorkoutBindingDuplicateLaneStageAssignmentException : CatalogWorkoutBindingException
{
    public CatalogWorkoutBindingDuplicateLaneStageAssignmentException(string message) : base(message) { }
}

/// <summary>Backend Integration Phase 10K-FREQ.6D.4D Split A: a catalog progression declares more lanes for a week than that week has StageControlled structural slots — RunLayout, not a catalog lane declaration, is the structural-cardinality authority.</summary>
internal sealed class CatalogWorkoutBindingLaneCountMismatchException : CatalogWorkoutBindingException
{
    public CatalogWorkoutBindingLaneCountMismatchException(string message) : base(message) { }
}

/// <summary>Backend Integration Phase 10K-FREQ.6D.4D Split B: the resolved progression stage declares more than one prescription-profile candidate — no multi-profile selection policy exists; a stage becomes ProfileBacked only with exactly one candidate.</summary>
internal sealed class CatalogWorkoutBindingAmbiguousPrescriptionProfileCandidateException : CatalogWorkoutBindingException
{
    public CatalogWorkoutBindingAmbiguousPrescriptionProfileCandidateException(string message) : base(message) { }
}
