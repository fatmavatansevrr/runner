using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Binding;

/// <summary>
/// Backend Integration Phase 4G.5E typed input to
/// <see cref="IDynamicCoreWorkoutBindingOrchestrator"/>. Generalizes the live
/// 12-week binding pipeline (Phase 4F.5/4F.6A/4F.6B, all reused unmodified)
/// to any mathematically feasible standalone-core target week count (8-14
/// for <c>TEN_K_MASTER v6</c>), the way Phase 4G.5D generalized the
/// phase/week skeleton one step earlier in the same pipeline.
/// The live catalog composition reaches this layer through the dynamic
/// calendar pipeline; it does not independently admit a horizon.
/// </summary>
internal sealed class DynamicCoreWorkoutBindingContext
{
    public required PlanCatalogCandidateSummary Candidate { get; init; }

    /// <summary>The requested standalone-core week count. Consumed exactly as given.</summary>
    public required int TargetWeekCount { get; init; }

    public required DateOnly StartDate { get; init; }
    public required DateOnly AsOfDate { get; init; }

    public required IReadOnlyList<DayOfWeek> PreferredDays { get; init; }
    public required DayOfWeek LongRunDayPreference { get; init; }

    /// <summary>
    /// Already-resolved runtime-condition results (mirrors <see cref="ProgressionStageAllocationContext.ConditionResults"/>)
    /// — this orchestrator never evaluates a condition itself. Determines
    /// whether <c>GOAL_PACE_REHEARSAL</c> resolves to its primary candidate
    /// or its <c>CURRENT_FITNESS_SPECIFIC_REHEARSAL</c> fallback, using the
    /// exact same eligibility logic <see cref="ProgressionStageAllocator"/>
    /// already applies at 12 weeks — never reinterpreted here.
    /// </summary>
    public required IReadOnlyList<RuntimeConditionResolutionResult> ConditionResults { get; init; }

    public required ICatalogWorkoutDefinitionLoader WorkoutDefinitionLoader { get; init; }
}

/// <summary>Backend Integration Phase 4G.5E result — every intermediate artifact plus the final bound plan, for test/inspection purposes.</summary>
internal sealed class DynamicCoreWorkoutBindingResult
{
    public required PhaseAllocationResult PhaseAllocation { get; init; }
    public required GeneratedCatalogPlanSkeleton Skeleton { get; init; }
    public required DatedGeneratedCatalogPlanSkeleton DatedSkeleton { get; init; }
    public required GeneratedCatalogStageSchedule StageSchedule { get; init; }
    public required BoundCatalogPlan BoundPlan { get; init; }
}

/// <summary>Thrown when any stage of the pipeline's own existing output validators reports an invalid result.</summary>
internal sealed class DynamicCoreWorkoutBindingFailedException : Exception
{
    public DynamicCoreWorkoutBindingFailedException(string message) : base(message)
    {
    }

    public DynamicCoreWorkoutBindingFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Backend Integration Phase 4G.5E — generalizes the live 12-week
/// stage-scheduling/calendar/binding pipeline to any mathematically feasible
/// standalone-core target week count, by chaining, in order:
/// <list type="number">
/// <item>Phase 4G.5D's <see cref="IDynamicCoreWeekSkeletonOrchestrator"/> for the 8-14-week phase/week skeleton.</item>
/// <item>The existing, unmodified <see cref="ICatalogWorkoutProgressionLoader"/> to load the workout-progression document.</item>
/// <item>The existing, unmodified <see cref="IProgressionStageAllocator"/> (Phase 4F.6A) for fine-grained KEY_SESSION stage scheduling, including its own compression/extension/fallback logic — never re-derived here.</item>
/// <item>The existing, unmodified <see cref="ICatalogWeekSkeletonCalendarMaterializer"/> (Phase 4F.5) for calendar-day assignment.</item>
/// <item>The existing, unmodified <see cref="ICatalogWorkoutBinder"/> (Phase 4F.6B) for exact workout-definition identity binding.</item>
/// </list>
/// Every step's own existing output validator is reused unmodified. This
/// orchestrator performs no eligibility, compression, extension, fallback,
/// or workout-selection logic of its own — it is pure composition. Consumes
/// phase allocation and stage sequencing exactly as already decided
/// (TD-ALLOCATION-PRIORITY-001, TD-FOUNDATION-COMPRESSION-001, the 4G.5C
/// Foundation-first extension policy) via the unmodified Phase 4G.5D
/// orchestrator — never reorders or reinterprets it.
///
/// <see cref="ICatalogWorkoutBinder"/> is called directly (<c>new
/// CatalogWorkoutBinder().BindAsync(...)</c> in the default composition, or
/// an injected instance) rather than being wrapped, because no
/// dark-reachability test constrains its call sites — it is already
/// <see cref="RunningApp.Application.RuntimeCatalog.PreviewRouting.CatalogPreviewGenerator"/>'s
/// one live call site (see PHASE4G_5E_DYNAMIC_CORE_WORKOUT_BINDING.md
/// section on reuse strategy for the confirming repo-wide grep). This
/// mirrors <see cref="Materialization.GoalPaceReachabilityVerifier"/>'s own
/// established precedent of calling <see cref="ProgressionStageAllocator.Allocate"/>
/// directly rather than through a sibling verifier's public entry point.
///
/// Production reachability is transitive through
/// <c>CatalogPreviewGenerator</c>'s dynamic calendar composition. This layer
/// remains an internal composition step rather than an independent route.
/// </summary>
internal interface IDynamicCoreWorkoutBindingOrchestrator
{
    Task<DynamicCoreWorkoutBindingResult> BindAsync(DynamicCoreWorkoutBindingContext context, CancellationToken ct = default);
}

/// <inheritdoc cref="IDynamicCoreWorkoutBindingOrchestrator"/>
internal sealed class DynamicCoreWorkoutBindingOrchestrator : IDynamicCoreWorkoutBindingOrchestrator
{
    private readonly IDynamicCoreWeekSkeletonOrchestrator _skeletonOrchestrator;
    private readonly ICatalogWorkoutProgressionLoader _progressionLoader;
    private readonly IProgressionStageAllocator _stageAllocator;
    private readonly IGeneratedCatalogStageScheduleValidator _stageScheduleValidator;
    private readonly ICatalogWeekSkeletonCalendarMaterializer _calendarMaterializer;
    private readonly IDatedGeneratedCatalogPlanSkeletonValidator _datedSkeletonValidator;
    private readonly ICatalogWorkoutBinder _workoutBinder;
    private readonly IBoundCatalogPlanValidator _boundPlanValidator;

    public DynamicCoreWorkoutBindingOrchestrator(
        IDynamicCoreWeekSkeletonOrchestrator skeletonOrchestrator,
        ICatalogWorkoutProgressionLoader progressionLoader,
        IProgressionStageAllocator stageAllocator,
        IGeneratedCatalogStageScheduleValidator stageScheduleValidator,
        ICatalogWeekSkeletonCalendarMaterializer calendarMaterializer,
        IDatedGeneratedCatalogPlanSkeletonValidator datedSkeletonValidator,
        ICatalogWorkoutBinder workoutBinder,
        IBoundCatalogPlanValidator boundPlanValidator)
    {
        _skeletonOrchestrator = skeletonOrchestrator;
        _progressionLoader = progressionLoader;
        _stageAllocator = stageAllocator;
        _stageScheduleValidator = stageScheduleValidator;
        _calendarMaterializer = calendarMaterializer;
        _datedSkeletonValidator = datedSkeletonValidator;
        _workoutBinder = workoutBinder;
        _boundPlanValidator = boundPlanValidator;
    }

    public async Task<DynamicCoreWorkoutBindingResult> BindAsync(DynamicCoreWorkoutBindingContext context, CancellationToken ct = default)
    {
        var candidate = context.Candidate;

        // Step 1: Phase 4G.5D dynamic phase/week skeleton (8-14 weeks) --
        // reused unmodified, itself consuming CatalogPhaseAllocationResolver's
        // generic overload exactly as given.
        var skeletonResult = _skeletonOrchestrator.Build(new DynamicCoreWeekSkeletonOrchestrationContext
        {
            Candidate = candidate,
            TargetWeekCount = context.TargetWeekCount,
            StartDate = context.StartDate,
            AsOfDate = context.AsOfDate,
        });

        var skeleton = skeletonResult.Skeleton;

        // Step 2: workout-progression document -- loaded once, never reloaded downstream.
        var progression = await _progressionLoader.LoadAsync(candidate.WorkoutProgression);

        // Step 3: fine-grained KEY_SESSION stage scheduling (Phase 4F.6A) --
        // the real allocator's own compression/extension/eligibility/fallback
        // algorithm, entirely unmodified and un-duplicated.
        GeneratedCatalogStageSchedule stageSchedule;
        try
        {
            stageSchedule = _stageAllocator.Allocate(new ProgressionStageAllocationContext
            {
                CandidateKey = candidate.CandidateKey,
                CandidateVersion = candidate.CandidateVersion,
                Progression = progression,
                Skeleton = skeleton,
                ConditionResults = context.ConditionResults,
            });
        }
        catch (Exception ex) when (ex is ProgressionStagePhaseMismatchException
            or ProgressionStageDuplicateOrMissingKeyException
            or ProgressionStageDuplicateRelativeOrderException
            or ProgressionStageIneligibleWithoutFallbackException
            or ProgressionStageFallbackTargetNotFoundException
            or ProgressionStageAmbiguousFallbackException
            or ProgressionStageFallbackCycleException
            or ProgressionStageFallbackBoundsUnreconcilableException
            or ProgressionStageUnknownConditionKeyException
            or ProgressionStageConditionResultMissingException
            or ProgressionPhaseCapacityInsufficientException
            or ProgressionPhaseCapacityExceedsMaximumException)
        {
            throw new DynamicCoreWorkoutBindingFailedException(
                $"Stage scheduling failed for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' " +
                $"at targetWeekCount={context.TargetWeekCount}: {ex.Message}", ex);
        }

        var stageScheduleValidation = _stageScheduleValidator.Validate(stageSchedule, skeleton);
        if (!stageScheduleValidation.IsValid)
        {
            throw new DynamicCoreWorkoutBindingFailedException(
                $"Stage schedule for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' at " +
                $"targetWeekCount={context.TargetWeekCount} failed output validation: " +
                string.Join(", ", stageScheduleValidation.Errors) + ".");
        }

        // Step 4: calendar-day assignment (Phase 4F.5) -- unmodified.
        var calendarProvenance = new CatalogCalendarMaterializationProvenance(
            candidate.CandidateKey, candidate.CandidateVersion, context.AsOfDate, context.StartDate,
            context.PreferredDays, context.LongRunDayPreference, CatalogCalendarDayMaterializerVersion.V1,
            skeleton.SchemaVersion, skeleton.DependencyVersions);

        var calendarContext = new CatalogCalendarAssignmentContext(
            context.StartDate, RunningApp.Domain.Enums.GoalType.Race, context.PreferredDays, context.LongRunDayPreference,
            skeleton, CatalogCalendarAssignmentPolicy.RaceHardConstraint, calendarProvenance);

        DatedGeneratedCatalogPlanSkeleton datedSkeleton;
        try
        {
            datedSkeleton = _calendarMaterializer.Materialize(calendarContext);
        }
        catch (Exception ex) when (ex is CatalogPreferredDaysRequiredException
            or CatalogPreferredDayCountInvalidException
            or CatalogPreferredDaysDuplicatedException
            or CatalogLongRunDayRequiredException
            or CatalogLongRunDayNotPreferredException
            or CatalogCalendarRoleStructureInvalidException
            or CatalogPreferredDayConfigurationUnsafeException)
        {
            throw new DynamicCoreWorkoutBindingFailedException(
                $"Calendar-day assignment failed for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' " +
                $"at targetWeekCount={context.TargetWeekCount}: {ex.Message}", ex);
        }

        var datedValidation = _datedSkeletonValidator.Validate(datedSkeleton, context.PreferredDays, context.LongRunDayPreference);
        if (!datedValidation.IsValid)
        {
            throw new DynamicCoreWorkoutBindingFailedException(
                $"Dated skeleton for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' at " +
                $"targetWeekCount={context.TargetWeekCount} failed output validation: " +
                string.Join(", ", datedValidation.Errors) + ".");
        }

        // Step 5: exact workout-definition binding (Phase 4F.6B) -- the
        // real, unmodified binder, called directly (see this type's own doc
        // comment for why direct-call rather than a wrapper is the correct
        // reuse strategy here).
        var bindingContext = new CatalogWorkoutBindingContext
        {
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            DatedSkeleton = datedSkeleton,
            StageSchedule = stageSchedule,
            Progression = progression,
            ReferencedWorkouts = candidate.ReferencedWorkouts,
            WorkoutDefinitionLoader = context.WorkoutDefinitionLoader,
        };

        BoundCatalogPlan boundPlan;
        try
        {
            boundPlan = await _workoutBinder.BindAsync(bindingContext, ct);
        }
        catch (Exception ex) when (ex is CatalogWorkoutBindingMissingProgressionStageException
            or CatalogWorkoutBindingUnknownProgressionStageException
            or CatalogWorkoutBindingMissingCandidateReferenceException
            or CatalogWorkoutBindingAmbiguousCandidateException
            or CatalogWorkoutBindingCandidateNotFoundException
            or CatalogWorkoutBindingVersionMismatchException
            or CatalogWorkoutBindingOutsideDependencyClosureException
            or CatalogWorkoutBindingDefinitionInvalidException)
        {
            throw new DynamicCoreWorkoutBindingFailedException(
                $"Workout binding failed for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' " +
                $"at targetWeekCount={context.TargetWeekCount}: {ex.Message}", ex);
        }

        var boundPlanValidation = _boundPlanValidator.Validate(boundPlan, datedSkeleton);
        if (!boundPlanValidation.IsValid)
        {
            throw new DynamicCoreWorkoutBindingFailedException(
                $"Bound workout plan for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' at " +
                $"targetWeekCount={context.TargetWeekCount} failed output validation: " +
                string.Join(", ", boundPlanValidation.Errors) + ".");
        }

        return new DynamicCoreWorkoutBindingResult
        {
            PhaseAllocation = skeletonResult.PhaseAllocation,
            Skeleton = skeleton,
            DatedSkeleton = datedSkeleton,
            StageSchedule = stageSchedule,
            BoundPlan = boundPlan,
        };
    }
}
