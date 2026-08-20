using RunningApp.Application.RuntimeCatalog.Prescription.Execution;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Resolvers;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4G.5H typed input to
/// <see cref="IDynamicCoreCalendarMaterializationOrchestrator"/>. This is the
/// top-level composition of all five dynamic dark layers built across this
/// session's Phase 4G.5D-5H work: phase/week skeleton (4G.5D) -&gt; workout
/// binding (4G.5E, which already performs calendar-day assignment as a
/// prerequisite for binding, via the unmodified Phase 4F.5
/// <see cref="ICatalogWeekSkeletonCalendarMaterializer"/>) -&gt; volume/long-run
/// planning (4G.5F) -&gt; pace/session prescription (4G.5G) -&gt; this phase's own
/// addition: race-date alignment verification against an explicit
/// <see cref="RaceDate"/>, which none of the prior four dark layers threaded
/// through.
/// <c>CatalogPreviewGenerator</c> now uses this composition for admitted
/// compressed, preferred, and extended standalone cores.
/// </summary>
internal sealed class DynamicCoreCalendarMaterializationContext
{
    public required PlanCatalogCandidateSummary Candidate { get; init; }

    /// <summary>The requested standalone-core week count. Consumed exactly as given.</summary>
    public required int TargetWeekCount { get; init; }

    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// The race date the materialized calendar must align to. Not itself
    /// carried by any Phase 4G.5D-5G dark result type -- see this
    /// orchestrator's own doc comment and PHASE4G_5H_DYNAMIC_CORE_CALENDAR_MATERIALIZATION.md
    /// for why it is threaded through explicitly here rather than assumed.
    /// </summary>
    public required DateOnly RaceDate { get; init; }

    public required DateOnly AsOfDate { get; init; }

    public required IReadOnlyList<DayOfWeek> PreferredDays { get; init; }
    public required DayOfWeek LongRunDayPreference { get; init; }

    public required IReadOnlyList<RuntimeConditionResolutionResult> ConditionResults { get; init; }

    public required DTOs.Plan.GeneratePreviewRequest PreviewRequest { get; init; }
    public required ResolverInputSnapshot ResolverInput { get; init; }

    public required Binding.ICatalogWorkoutDefinitionLoader WorkoutDefinitionLoader { get; init; }
    public required ICatalogPeakVolumeBandLoader PeakVolumeBandLoader { get; init; }

    /// <summary>
    /// Phase 10K-FREQ.6D.4D.5G — threaded straight through to
    /// <see cref="DynamicCoreSessionPrescriptionContext.ExecutionIndex"/>; this orchestrator does not
    /// itself construct, cache, or interpret it. See that type's own doc comment.
    /// </summary>
    public ExecutionPrescriptionIndex? ExecutionIndex { get; init; }
}

/// <summary>Backend Integration Phase 4G.5H result — every intermediate artifact from the full five-layer pipeline, plus race-date alignment verification.</summary>
internal sealed class DynamicCoreCalendarMaterializationResult
{
    public required DynamicCoreSessionPrescriptionResult PrescriptionResult { get; init; }
    public required RaceDateAlignmentVerificationResult RaceDateAlignment { get; init; }
}

/// <summary>Thrown when the real, unmodified <see cref="RaceDateAlignmentVerifier"/> reports a Fail outcome.</summary>
internal sealed class DynamicCoreCalendarMaterializationFailedException : Exception
{
    public DynamicCoreCalendarMaterializationFailedException(string message) : base(message)
    {
    }
}

/// <summary>
/// Backend Integration Phase 4G.5H — the top-level composition
/// of all five dynamic 8-14-week layers built in this session. Performs
/// NO date-assignment, spacing, or race-alignment calculation of its own:
/// calendar-day assignment (Decision 1-8 of Phase 4F.5's own algorithm --
/// PreferredDays/LongRunDay spacing rules, contiguous 7-day weeks, no Monday
/// alignment requirement) already happens, unmodified, inside the Phase
/// 4G.5E workout-binding dark orchestrator (which must produce a dated
/// skeleton before <see cref="Binding.CatalogWorkoutBinder"/> can bind
/// anything to it) -- this orchestrator does not re-run or duplicate that
/// step. The one genuinely new composition this phase adds is race-date
/// alignment: none of the four prior dark orchestrators (4G.5D-4G.5G) thread
/// a <see cref="DynamicCoreCalendarMaterializationContext.RaceDate"/> through
/// at all, so nothing in the existing dark chain verified it. This
/// orchestrator closes that gap by calling the real, already-existing,
/// already-dark <see cref="RaceDateAlignmentVerifier"/> (Phase 4G.3B.3,
/// verifier 9 of 9) directly, against the dated skeleton Phase 4G.5E's
/// orchestrator already exposes on its own result type (its own
/// <c>DatedSkeleton</c> property) -- no new date-comparison logic was
/// written for this pass.
///
/// Live boundary: <c>CatalogPreviewGenerator</c> constructs this composition
/// only after the canonical classifier/policy admits a standalone core.
/// Controllers do not call it directly, and it cannot activate Preparation
/// Runway or expand the upstream horizon range.
/// </summary>
internal interface IDynamicCoreCalendarMaterializationOrchestrator
{
    Task<DynamicCoreCalendarMaterializationResult> MaterializeAsync(DynamicCoreCalendarMaterializationContext context, CancellationToken ct = default);
}

/// <inheritdoc cref="IDynamicCoreCalendarMaterializationOrchestrator"/>
internal sealed class DynamicCoreCalendarMaterializationOrchestrator : IDynamicCoreCalendarMaterializationOrchestrator
{
    private readonly IDynamicCoreSessionPrescriptionOrchestrator _prescriptionOrchestrator;

    public DynamicCoreCalendarMaterializationOrchestrator(IDynamicCoreSessionPrescriptionOrchestrator prescriptionOrchestrator)
    {
        _prescriptionOrchestrator = prescriptionOrchestrator;
    }

    public async Task<DynamicCoreCalendarMaterializationResult> MaterializeAsync(DynamicCoreCalendarMaterializationContext context, CancellationToken ct = default)
    {
        var candidate = context.Candidate;

        // Step 1: the full Phase 4G.5D-4G.5G dark pipeline -- reused unmodified.
        var prescriptionResult = await _prescriptionOrchestrator.PrescribeAsync(new DynamicCoreSessionPrescriptionContext
        {
            Candidate = candidate,
            TargetWeekCount = context.TargetWeekCount,
            StartDate = context.StartDate,
            AsOfDate = context.AsOfDate,
            PreferredDays = context.PreferredDays,
            LongRunDayPreference = context.LongRunDayPreference,
            ConditionResults = context.ConditionResults,
            PreviewRequest = context.PreviewRequest,
            ResolverInput = context.ResolverInput,
            WorkoutDefinitionLoader = context.WorkoutDefinitionLoader,
            PeakVolumeBandLoader = context.PeakVolumeBandLoader,
            ExecutionIndex = context.ExecutionIndex,
        }, ct);

        // Step 2: race-date alignment (the one genuinely new composition --
        // see this type's own doc comment) -- the real, unmodified dark
        // verifier, called directly against the already-materialized dated
        // skeleton Phase 4G.5E's own result already exposes.
        var datedSkeleton = prescriptionResult.VolumeResult.BindingResult.DatedSkeleton;
        var raceDateAlignment = RaceDateAlignmentVerifier.Verify(datedSkeleton, context.RaceDate);

        if (raceDateAlignment.Outcome != RaceDateAlignmentOutcome.Pass)
        {
            throw new DynamicCoreCalendarMaterializationFailedException(
                $"Race-date alignment failed for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' " +
                $"at targetWeekCount={context.TargetWeekCount}: " + string.Join(", ", raceDateAlignment.Findings) + ".");
        }

        return new DynamicCoreCalendarMaterializationResult
        {
            PrescriptionResult = prescriptionResult,
            RaceDateAlignment = raceDateAlignment,
        };
    }
}
