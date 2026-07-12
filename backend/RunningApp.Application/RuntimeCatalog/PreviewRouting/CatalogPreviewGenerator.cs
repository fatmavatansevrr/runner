using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.1 — runs the catalog pilot flow for a single
/// request already routed to <see cref="GenerationSource.Catalog"/>: loads
/// the (published-only) candidate via <see cref="ICatalogCandidateEligibilityGate"/>,
/// runs the full <see cref="RuntimeConditionResolutionService"/> pipeline
/// with a single shared <see cref="DateOnly"/> AsOfDate, applies the
/// NotEvaluated governance policy (see <see cref="NotEvaluatedReasonClassifier"/>)
/// to every result, and freezes everything into a <see cref="CatalogPreviewSnapshot"/>.
///
/// Never falls back to SQL — every failure path throws a typed exception
/// that propagates out unchanged. Never persists anything itself (the
/// caller, <c>PlanServices</c>, owns the <c>PlanPreview</c> persistence
/// boundary) and never generates TrainingWeeks/TrainingDays (stage-to-week
/// scheduling remains unimplemented — <see cref="CatalogPreviewSnapshot.SelectedStageKeys"/>/
/// <see cref="CatalogPreviewSnapshot.FallbackStagesUsed"/>/
/// <see cref="CatalogPreviewSnapshot.GeneratedPreviewPlanPayload"/> are always
/// empty/null).
///
/// As of Phase 4E.1, this class's success path is UNREACHABLE for real public
/// requests: TEN_K__4D__INTERMEDIATE v10 (and every one of its directly-loaded
/// dependencies) has status DRAFT in the current catalog source tree — the
/// eligibility gate always throws <see cref="CatalogCandidateNotPublishedException"/>
/// first. The success path is still fully implemented and tested (via the
/// gate's internal-dry-run entry point) so Phase 4E.2 has a working
/// foundation the moment a candidate is actually published.
///
/// Backend Integration Phase 4F.4 — after candidate eligibility and runtime-
/// condition resolution both succeed, this class performs a DARK internal
/// invocation of <see cref="ICatalogPlanSkeletonOrchestrator"/> (Phase 4F.3):
/// the resulting <see cref="GeneratedCatalogPlanSkeleton"/> is built and
/// fully validated for its correctness side effect only (an invalid/failed
/// materialization aborts preview generation) and is then discarded — never
/// stored on <see cref="CatalogPreviewSnapshot"/>, never included in the
/// snapshot's content hash, never returned to any public DTO. See
/// PHASE4F_4_DARK_INTERNAL_SKELETON_WIRING_INTO_CATALOG_PREVIEW.md.
/// </summary>
public interface ICatalogPreviewGenerator
{
    Task<CatalogPreviewSnapshot> GenerateAsync(GeneratePreviewRequest request, DateOnly asOfDate, CancellationToken ct = default);
}

/// <inheritdoc cref="ICatalogPreviewGenerator"/>
public sealed class CatalogPreviewGenerator : ICatalogPreviewGenerator
{
    /// <summary>Preview validity window, matching the existing legacy-SQL PlanPreview.ExpiresAt convention (PlanServices: DateTime.UtcNow.AddMinutes(30)).</summary>
    public static readonly TimeSpan PreviewLifetime = TimeSpan.FromMinutes(30);

    private readonly ICatalogCandidateEligibilityGate _gate;
    private readonly RuntimeConditionResolutionService _orchestration;
    private readonly ICatalogPlanSkeletonOrchestrator _skeletonOrchestrator;

    /// <summary>
    /// Public, DI-facing constructor — signature intentionally unchanged
    /// since Phase 4E.1. Composes a default <see cref="ICatalogPlanSkeletonOrchestrator"/>
    /// from its own pure, stateless, dependency-free Phase 4F.3 collaborators
    /// (no DbContext/HttpContext/clock/catalog-loader dependency among them)
    /// rather than taking it as a fourth constructor parameter: that
    /// interface, and everything Phase 4F.3 built around it, is deliberately
    /// `internal` to this assembly (Phase 4F.3's own boundary decision), and
    /// RunningApp.Api (the only DI-registration caller) has no
    /// InternalsVisibleTo grant onto RunningApp.Application — a public
    /// constructor cannot expose an internal parameter type (CS0051).
    /// Building it once here, per Scoped <see cref="CatalogPreviewGenerator"/>
    /// instance, is equivalent in effect to a Scoped DI registration without
    /// requiring one.
    /// </summary>
    public CatalogPreviewGenerator(ICatalogCandidateEligibilityGate gate, RuntimeConditionResolutionService orchestration)
        : this(gate, orchestration, DefaultSkeletonOrchestrator())
    {
    }

    /// <summary>Test-only seam (Phase 4F.4) letting RunningApp.IntegrationTests substitute a fake/spy <see cref="ICatalogPlanSkeletonOrchestrator"/> without widening this type's public constructor surface.</summary>
    internal CatalogPreviewGenerator(
        ICatalogCandidateEligibilityGate gate,
        RuntimeConditionResolutionService orchestration,
        ICatalogPlanSkeletonOrchestrator skeletonOrchestrator)
    {
        _gate = gate;
        _orchestration = orchestration;
        _skeletonOrchestrator = skeletonOrchestrator;
    }

    private static ICatalogPlanSkeletonOrchestrator DefaultSkeletonOrchestrator() => new CatalogPlanSkeletonOrchestrator(
        new CatalogPhaseAllocationResolver(),
        new CatalogRunLayoutResolver(),
        new CatalogStageToWeekContextFactory(),
        new CatalogStageToWeekMaterializer(),
        new GeneratedCatalogPlanSkeletonValidator());

    public async Task<CatalogPreviewSnapshot> GenerateAsync(GeneratePreviewRequest request, DateOnly asOfDate, CancellationToken ct = default)
    {
        var candidate = await _gate.LoadForPublicPreviewAsync(
            PilotGenerationRouteDecider.PilotCandidateKey, PilotGenerationRouteDecider.PilotCandidateVersion, ct);

        var input = BuildInputSnapshot(request, asOfDate);
        var context = new RuntimeResolverContext
        {
            InputSnapshot = input,
            CoreCycle = candidate.CoreCycle,
            AsOfDate = asOfDate,
        };

        IReadOnlyList<RuntimeConditionResolutionResult> results;
        try
        {
            results = _orchestration.ResolveAllResults(context);
        }
        catch (InvalidOperationException ex)
        {
            // Resolver configuration/context error (e.g. TimeAdequacyResolver's
            // missing-CoreCycle guard). Never swallowed, never converted into a
            // NotEvaluated result or a fallback -- an explicit generation failure.
            throw new PlanPreviewGenerationFailedException(
                $"Runtime condition resolution failed due to a configuration error: {ex.Message}", ex);
        }
        catch (ArgumentException ex)
        {
            // A resolver (e.g. TimeAdequacyResolver, for a Race-goal request
            // missing RaceDate/StartDate) rejected invalid input that should
            // already have been caught by request validation upstream. This is
            // a technical/configuration failure per this phase's governance
            // policy -- never swallowed, never converted into a fallback or a
            // NotEvaluated result.
            throw new PlanPreviewGenerationFailedException(
                $"Runtime condition resolution failed due to invalid input reaching a resolver: {ex.Message}", ex);
        }

        ApplyNotEvaluatedGovernancePolicy(results);

        // ── Backend Integration Phase 4F.4: dark internal skeleton materialization ──
        // Runs only after candidate eligibility (line above the resolver call)
        // and runtime-condition resolution (incl. governance policy) have both
        // succeeded — every input below is already authoritative and frozen;
        // nothing is reloaded, reselected, or rerun. The orchestrator context
        // and result are local to this method only: never attached to the
        // snapshot, never hashed, never returned. This call's only observable
        // effect is: (a) nothing, on success: preview construction continues
        // unchanged; (b) an aborted, typed preview-generation failure if the
        // candidate's live catalog data cannot produce a valid skeleton.
        BuildDarkInternalSkeleton(candidate, asOfDate);

        var trace = BuildDecisionTrace(results);
        var createdAtUtc = DateTime.UtcNow;

        return CatalogPreviewSnapshotBuilder.Build(
            input, asOfDate, candidate, "PILOT_TEN_K_INTERMEDIATE_4D_MATCH",
            results, trace, createdAtUtc, createdAtUtc.Add(PreviewLifetime));
    }

    /// <summary>
    /// Applies the governance policy per <see cref="NotEvaluatedReasonCategory"/>
    /// to every result in the pipeline. NotApplicable/UpstreamShortCircuit:
    /// continue (no action). RequiredInputNotProvided/Unsupported/
    /// DependencyUnresolved/TechnicalOrConfigurationFailure: throw the
    /// matching typed exception immediately -- never continue past one of
    /// these. OptionalInputNotProvided: no current resolver reasonCode maps
    /// to this category (see NotEvaluatedReasonClassifier's own table) --
    /// since no catalog evidence exists yet declaring which inputs are safe
    /// to proceed without, this method treats an (currently impossible)
    /// OptionalInputNotProvided classification the same as
    /// TechnicalOrConfigurationFailure (fail loud) rather than silently
    /// continuing without justification, per the governance instruction
    /// "may continue only if the catalog explicitly supports generation
    /// without it" -- no such explicit support exists to check today.
    /// </summary>
    /// <summary>Internal (not private) solely so RunningApp.IntegrationTests can exercise this policy mapping directly — see InternalsVisibleTo in RunningApp.Application.csproj. No production behavior change.</summary>
    internal static void ApplyNotEvaluatedGovernancePolicy(IReadOnlyList<RuntimeConditionResolutionResult> results)
    {
        foreach (var result in results)
        {
            if (result.Status != RuntimeConditionResolutionStatus.NotEvaluated)
            {
                continue;
            }

            var category = NotEvaluatedReasonClassifier.Classify(result.ReasonCode);
            switch (category)
            {
                case NotEvaluatedReasonCategory.NotApplicable:
                case NotEvaluatedReasonCategory.UpstreamShortCircuit:
                    continue;

                case NotEvaluatedReasonCategory.RequiredInputNotProvided:
                    throw new RuntimeConditionRequiredInputMissingException(
                        $"{result.ConditionType} could not be evaluated because a required input was not provided " +
                        $"(reasonCode={result.ReasonCode}).");

                case NotEvaluatedReasonCategory.Unsupported:
                    throw new RuntimeConditionUnsupportedException(
                        $"{result.ConditionType} recognized the input combination but has no approved rule to " +
                        $"classify it (reasonCode={result.ReasonCode}).");

                case NotEvaluatedReasonCategory.DependencyUnresolved:
                    throw new RuntimeConditionDependencyUnresolvedException(
                        $"{result.ConditionType} could not be evaluated because a dependency result was missing " +
                        $"from the pipeline (reasonCode={result.ReasonCode}). This indicates an orchestration " +
                        "defect -- RuntimeConditionResolutionService.ResolveAllResults should never omit a " +
                        "dependency.");

                case NotEvaluatedReasonCategory.OptionalInputNotProvided:
                case NotEvaluatedReasonCategory.TechnicalOrConfigurationFailure:
                default:
                    throw new PlanPreviewGenerationFailedException(
                        $"{result.ConditionType} could not be evaluated (reasonCode={result.ReasonCode}, " +
                        $"category={category}). Catalog preview generation failed.");
            }
        }
    }

    /// <summary>
    /// Backend Integration Phase 4F.4 — builds the Phase 4F.3 orchestration
    /// context exclusively from <paramref name="candidate"/> (already loaded
    /// and PUBLISHED-gated by <see cref="_gate"/>) and <paramref name="asOfDate"/>
    /// (already fixed by the caller) and invokes <see cref="_skeletonOrchestrator"/>.
    /// StartDate mirrors AsOfDate, exactly matching <see cref="BuildInputSnapshot"/>'s
    /// own documented Phase 4E.1 simplification — no new date policy is
    /// introduced. The result is deliberately discarded: this call's only
    /// contract is "throw a typed failure if the candidate's live catalog
    /// data cannot produce a valid skeleton," never "return usable content."
    /// Every one of the 8 typed Phase 4F.3 orchestration exceptions is mapped
    /// onto the pre-existing <see cref="PlanPreviewGenerationFailedException"/>
    /// (never a new public error code — the existing preview-generation
    /// failure taxonomy already preserves the distinction) with the original
    /// exception preserved as <c>InnerException</c> and no internal phase/
    /// layout detail included in any message a client could ever see (see
    /// GlobalExceptionHandler: 500-status messages are never echoed to the
    /// client).
    /// </summary>
    private void BuildDarkInternalSkeleton(PlanCatalogCandidateSummary candidate, DateOnly asOfDate)
    {
        var skeletonContext = new CatalogPlanSkeletonOrchestrationContext
        {
            Candidate = candidate,
            ExpectedCandidateKey = candidate.CandidateKey,
            ExpectedCandidateVersion = candidate.CandidateVersion,
            ExpectedMasterTemplate = candidate.MasterTemplate,
            ExpectedRunLayout = candidate.Layout,
            StartDate = asOfDate,
            AsOfDate = asOfDate,
        };

        try
        {
            _skeletonOrchestrator.Build(skeletonContext);
        }
        catch (Exception ex) when (ex is CatalogPhaseAllocationSourceMissingException
            or CatalogPhaseAllocationInvalidException
            or CatalogPhaseAllocationTotalMismatchException
            or CatalogMasterTemplateReferenceMismatchException
            or CatalogRunLayoutReferenceMismatchException
            or CatalogRunLayoutSlotInvalidException
            or CatalogSkeletonContextInvalidException
            or CatalogPlanSkeletonOrchestrationFailedException)
        {
            throw new PlanPreviewGenerationFailedException(
                $"CATALOG_INTERNAL_SKELETON_MATERIALIZATION_FAILED: internal skeleton materialization failed " +
                $"for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}': {ex.Message}", ex);
        }
    }

    private static ResolverDecisionTrace BuildDecisionTrace(IReadOnlyList<RuntimeConditionResolutionResult> results)
    {
        var resolverKeys = new[] { "TIME_ADEQUACY_RESOLVER", "PACE_SOURCE_RESOLVER", "CORE_ENTRY_READINESS_RESOLVER", "GOAL_FEASIBILITY_RESOLVER" };
        var steps = new List<ResolverDecisionTraceStep>(results.Count);
        for (var i = 0; i < results.Count; i++)
        {
            var resolverKey = i < resolverKeys.Length ? resolverKeys[i] : results[i].ConditionType;
            steps.Add(ResolverDecisionTraceStep.FromResult(i, resolverKey, results[i]));
        }

        return new ResolverDecisionTrace { Steps = steps };
    }

    /// <summary>
    /// Maps a GeneratePreviewRequest onto a ResolverInputSnapshot.
    /// StartDate is set to <paramref name="asOfDate"/> itself (Phase 4E.1
    /// simplification, documented explicitly): GeneratePreviewRequest has no
    /// dedicated plan-start-date field of its own (the legacy SQL flow
    /// computes an implicit "next Monday" internally, only at the point it
    /// builds concrete calendar dates for weeks/days -- a stage-to-week
    /// scheduling concern this phase does not implement). Using AsOfDate as
    /// StartDate is the most direct, non-invented choice available: it makes
    /// TIME_ADEQUACY_IN's available-weeks calculation exact and reproducible
    /// without guessing a scheduling policy. A future phase that implements
    /// real stage-to-week scheduling may need a more precise start date and
    /// should revisit this.
    ///
    /// GoalDistanceKm is hardcoded to 10.0: the pilot route (see
    /// PilotGenerationRouteDecider) only ever reaches this method for
    /// GoalDistance.TenK, so this mirrors PlanServices.GetGoalDistanceInKm's
    /// own existing TenK=10.0 constant rather than reimplementing a general
    /// GoalDistance-to-km mapping for distances this pilot cannot reach.
    /// </summary>
    private static ResolverInputSnapshot BuildInputSnapshot(GeneratePreviewRequest request, DateOnly asOfDate) => new()
    {
        GoalType = request.GoalType,
        GoalDistance = request.GoalDistance,
        GoalDistanceKm = 10.0,
        TargetFinishTimeSeconds = request.TargetFinishTimeSeconds,
        DaysPerWeek = request.DaysPerWeek,
        Level = request.Level,
        StartDate = asOfDate,
        RaceDate = request.RaceDate,
        RecentLongestRunKm = request.RecentLongestRunKm,
        RecentWeeklyVolumeKm = request.RecentWeeklyVolumeKm,
        RecentRunsPerWeek = request.RecentRunsPerWeek,
        RecentRaceDistanceKm = request.RecentRaceDistanceKm,
        RecentRaceFinishTimeSeconds = request.RecentRaceFinishTimeSeconds,
        RecentRaceDate = request.RecentRaceDate,
    };
}
