using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;

/// <summary>
/// Production-owned internal composition of the existing 15-20-week TEN_K
/// runway and 12-week Core components. It is deliberately absent from DI,
/// public preview, confirmation, and persistence call graphs.
/// </summary>
internal sealed class TenKPreparationRunwayDarkOrchestrator
{
    private readonly ICatalogWorkoutDefinitionLoader _workoutLoader;
    private readonly ITenKPreparationRunwayProgressionLoader _progressionLoader;
    private readonly ITenKPreparationRunwayCoreGenerator _coreGenerator;
    private readonly ITenKPreparationRunwayNumericStage _numericStage;
    private readonly ITenKPreparationRunwayCalendarStage _calendarStage;
    private readonly ITenKPreparationRunwayPaceStage _paceStage;
    private readonly ITenKPreparationRunwayFinalInvariantValidator _finalValidator;

    public TenKPreparationRunwayDarkOrchestrator(
        ICatalogWorkoutDefinitionLoader workoutLoader,
        ITenKPreparationRunwayProgressionLoader progressionLoader,
        ITenKPreparationRunwayCoreGenerator coreGenerator,
        ITenKPreparationRunwayNumericStage numericStage,
        ITenKPreparationRunwayCalendarStage calendarStage,
        ITenKPreparationRunwayPaceStage paceStage,
        ITenKPreparationRunwayFinalInvariantValidator finalValidator)
    {
        _workoutLoader = workoutLoader;
        _progressionLoader = progressionLoader;
        _coreGenerator = coreGenerator;
        _numericStage = numericStage;
        _calendarStage = calendarStage;
        _paceStage = paceStage;
        _finalValidator = finalValidator;
    }

    public async Task<TenKPreparationRunwayDarkOrchestrationResult> OrchestrateAsync(
        TenKPreparationRunwayDarkOrchestrationRequest request,
        CancellationToken ct = default)
    {
        var trace = new List<TenKPreparationRunwayOrchestrationTraceEntry>();
        try
        {
            var invalid = ValidateRequest(request);
            if (invalid is not null)
                return Fail(TenKPreparationRunwayOrchestrationStage.Horizon, invalid.Value.Code,
                    "TenKPreparationRunwayDarkOrchestrator", null, invalid.Value.Reason, trace);

            // Stage 1: canonical day-accurate authority through the one policy call site.
            // Phase 4G.6B.1: when the caller already computed the
            // authoritative CoreHorizonDecision upstream (the public runway
            // preview path always does -- see CatalogPreviewGenerator.
            // GeneratePreparationRunwayPreviewAsync), that carried value is
            // used directly and RaceHorizonPolicy.Decide is NOT called a
            // second time here. Every pre-4G.6B.1 caller (all existing
            // TenKPreparationRunwayDarkOrchestratorTests call sites) leaves
            // HorizonDecision null and gets the original, unchanged
            // self-contained computation below -- this preserves that suite
            // byte-for-byte while eliminating the duplicate call on the one
            // path that now has an authoritative decision to carry.
            var horizonContext = new CoreHorizonContext(
                request.StartDate, request.RaceDate,
                request.Candidate.CoreCycle.MinimumWeeks,
                request.Candidate.CoreCycle.DefaultWeeks,
                request.Candidate.CoreCycle.MaximumWeeks!.Value);
            CoreHorizonDecision horizon;
            if (request.HorizonDecision is { } carriedHorizon)
            {
                if (carriedHorizon.MinimumCoreWeeks != request.Candidate.CoreCycle.MinimumWeeks ||
                    carriedHorizon.PreferredCoreWeeks != request.Candidate.CoreCycle.DefaultWeeks ||
                    carriedHorizon.MaximumCoreWeeks != request.Candidate.CoreCycle.MaximumWeeks)
                {
                    return Fail(TenKPreparationRunwayOrchestrationStage.Horizon,
                        TenKPreparationRunwayOrchestrationFailureCode.InvalidOrchestrationRequest,
                        nameof(RaceHorizonPolicy), null,
                        "Carried HorizonDecision core-cycle bounds do not match the candidate's own CoreCycle -- " +
                        "the authoritative decision must have been computed against this exact candidate.", trace);
                }
                horizon = carriedHorizon;
            }
            else
            {
                horizon = RaceHorizonPolicy.Decide(
                    request.StartDate, request.RaceDate,
                    request.Candidate.CoreCycle.MinimumWeeks, request.Candidate.CoreCycle.DefaultWeeks,
                    request.Candidate.CoreCycle.MaximumWeeks!.Value);
            }
            if (horizon.Mode != CoreHorizonMode.PreparationRunwayPlusCore || horizon.AvailableFullWeeks is < 15 or > 20)
                return Fail(TenKPreparationRunwayOrchestrationStage.Horizon,
                    TenKPreparationRunwayOrchestrationFailureCode.HorizonNotDirectRunway,
                    nameof(RaceHorizonPolicy), horizon.Mode.ToString(),
                    $"Dark direct-runway orchestration accepts exactly 15..20 full weeks; received {horizon.AvailableFullWeeks}.", trace);
            var runwayWeeks = horizon.AvailableFullWeeks - horizon.PreferredCoreWeeks;
            Add(trace, TenKPreparationRunwayOrchestrationStage.Horizon, "canonical_horizon",
                nameof(RaceHorizonPolicy), null, "PASS",
                ("classifierRule", horizon.Rules.FirstOrDefault() ?? "none"),
                ("availableFullWeeks", horizon.AvailableFullWeeks), ("leadingPartialDays", horizon.LeadingPartialDays), ("runwayWeeks", runwayWeeks));

            // Stage 2: consume the already-resolved readiness result; no raw evidence calculation.
            var profile = MapProfile(request.CoreEntryReadinessResult);
            if (profile is null)
                return Fail(TenKPreparationRunwayOrchestrationStage.ReadinessProfile,
                    TenKPreparationRunwayOrchestrationFailureCode.ReadinessProfileUnavailable,
                    nameof(CoreEntryReadinessResolver), request.CoreEntryReadinessResult.OutputValue,
                    "Readiness result must be Evaluated READY, CAUTION, or NOT_READY.", trace);
            Add(trace, TenKPreparationRunwayOrchestrationStage.ReadinessProfile, "resolved_profile",
                CoreEntryReadinessResolver.ConditionTypeValue, null, "PASS",
                ("resolverValue", request.CoreEntryReadinessResult.OutputValue ?? "null"), ("profile", profile.Value));

            // Stages 3-4: exact existing policy and generic allocator.
            var policies = TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(profile.Value);
            if (policies.Count == 0)
                return Fail(TenKPreparationRunwayOrchestrationStage.AllocationPolicy,
                    TenKPreparationRunwayOrchestrationFailureCode.AllocationPolicyUnavailable,
                    nameof(TenKPreparationRunwayAllocationPolicyFactory), null, "No policy metadata was produced.", trace);
            Add(trace, TenKPreparationRunwayOrchestrationStage.AllocationPolicy, "allocation_policy",
                TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyId,
                TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyVersion, "PASS",
                ("policyCount", policies.Count));
            var allocation = PreparationRunwayBlockAllocationEngine.Allocate(runwayWeeks, policies);
            if (!allocation.IsSuccess || allocation.Allocations is null || allocation.Allocations.Sum(a => a.AllocatedWeeks) != runwayWeeks)
                return Fail(TenKPreparationRunwayOrchestrationStage.BlockAllocation,
                    TenKPreparationRunwayOrchestrationFailureCode.BlockAllocationFailed,
                    nameof(PreparationRunwayBlockAllocationEngine), allocation.FailureCode?.ToString(),
                    allocation.FailureReason ?? "Allocation sum differs from authoritative runway weeks.", trace);
            Add(trace, TenKPreparationRunwayOrchestrationStage.BlockAllocation, "block_allocation",
                TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyId,
                TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyVersion, "PASS",
                allocation.Allocations.Select(a => (Key: $"block.{a.BlockKey}", Value: (object?)a.AllocatedWeeks)).ToArray());

            // Stages 5-6: typed reference policy, catalog reader/validator, exact-prefix binder.
            var referencePolicy = TenKPreparationRunwayProgressionPolicyFactory.Build();
            var loaded = new List<TenKPreparationRunwayLoadedProgression>();
            foreach (var block in allocation.Allocations.Where(a => a.AllocatedWeeks > 0).OrderBy(a => a.CanonicalOrder))
            {
                if (!referencePolicy.TryGetValue(block.BlockKey, out var reference))
                    return Fail(TenKPreparationRunwayOrchestrationStage.ProgressionLoading,
                        TenKPreparationRunwayOrchestrationFailureCode.ProgressionLoadingFailed,
                        nameof(TenKPreparationRunwayProgressionPolicyFactory), null,
                        $"No typed progression reference exists for positive block '{block.BlockKey}'.", trace);
                PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType> definition;
                try
                {
                    definition = await _progressionLoader.LoadValidatedAsync(block.BlockKey, reference, ct);
                }
                catch (Exception exception)
                {
                    return Fail(TenKPreparationRunwayOrchestrationStage.ProgressionLoading,
                        TenKPreparationRunwayOrchestrationFailureCode.ProgressionLoadingFailed,
                        nameof(PreparationRunwayBlockProgressionCatalogReader), exception.GetType().Name, exception.Message, trace);
                }
                loaded.Add(new TenKPreparationRunwayLoadedProgression(block.BlockKey, reference, definition));
                Add(trace, TenKPreparationRunwayOrchestrationStage.ProgressionLoading, block.BlockKey.ToString(),
                    reference.Key, reference.Version, "PASS", ("stepCount", definition.OrderedSteps.Count));
            }

            var bindings = new List<TenKPreparationRunwayResolvedBinding>();
            foreach (var block in allocation.Allocations.Where(a => a.AllocatedWeeks > 0).OrderBy(a => a.CanonicalOrder))
            {
                var definition = loaded.Single(p => p.BlockType == block.BlockKey).Definition;
                var bound = PreparationRunwayBlockWorkoutBindingEngine.Bind(
                    new PreparationRunwayBlockWorkoutBindingRequest<PreparationRunwayBlockType>(
                        block.BlockKey, block.AllocatedWeeks, definition));
                if (!bound.IsSuccess || bound.Binding is null)
                    return Fail(TenKPreparationRunwayOrchestrationStage.WorkoutBinding,
                        TenKPreparationRunwayOrchestrationFailureCode.WorkoutBindingFailed,
                        nameof(PreparationRunwayBlockWorkoutBindingEngine), bound.FailureCode?.ToString(),
                        bound.FailureReason ?? "Binding produced no result.", trace);
                var steps = Enumerable.Range(1, block.AllocatedWeeks).ToArray();
                bindings.Add(new TenKPreparationRunwayResolvedBinding(
                    block.BlockKey, bound.Binding, definition.ProgressionId, definition.Version, steps));
                Add(trace, TenKPreparationRunwayOrchestrationStage.WorkoutBinding, block.BlockKey.ToString(),
                    definition.ProgressionId, definition.Version, "PASS", ("anchorCount", bound.Binding.OrderedWorkoutReferences.Count));
            }

            // Stage 7: existing four-slot materializer.
            var structural = await PreparationRunwayWeekMaterializer.MaterializeAsync(
                new PreparationRunwayWeekMaterializationRequest<PreparationRunwayBlockType>(
                    profile.Value.ToString(), request.Candidate.CandidateKey, request.Candidate.CandidateVersion,
                    TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyId,
                    TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyVersion,
                    TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout(request.Candidate.DaysPerWeek),
                    allocation.Allocations,
                    bindings.Select(b => new PreparationRunwayMaterializationBlockBinding<PreparationRunwayBlockType>(
                        b.BlockType, b.Binding, b.ProgressionId, b.ProgressionVersion, b.OrderedProgressionStepNumbers)).ToArray(),
                    TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies(),
                    TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildSupportPolicy()),
                _workoutLoader, ct);
            if (!structural.IsSuccess || structural.Weeks is null)
                return Fail(TenKPreparationRunwayOrchestrationStage.StructuralMaterialization,
                    TenKPreparationRunwayOrchestrationFailureCode.StructuralMaterializationFailed,
                    nameof(PreparationRunwayWeekMaterializer), structural.FailureCode?.ToString(),
                    structural.FailureReason ?? "Structural materializer produced no weeks.", trace);
            Add(trace, TenKPreparationRunwayOrchestrationStage.StructuralMaterialization, "runway_weeks",
                PreparationRunwayWeekMaterializer.MaterializerId, PreparationRunwayWeekMaterializer.MaterializerVersion,
                "PASS", ("weekCount", structural.Weeks.Count));

            // Stage 8: one authoritative existing Core pipeline, using the same evidence objects.
            PreparationRunwayCalendarAuthority authority;
            try
            {
                authority = PreparationRunwayCalendarAuthorityAdapter.Create(horizonContext, horizon);
            }
            catch (InvalidOperationException exception)
            {
                return Fail(TenKPreparationRunwayOrchestrationStage.CoreGeneration,
                    TenKPreparationRunwayOrchestrationFailureCode.CoreGenerationFailed,
                    nameof(PreparationRunwayCalendarAuthorityAdapter), exception.GetType().Name, exception.Message, trace);
            }
            DynamicCoreCalendarMaterializationResult core;
            try
            {
                core = await _coreGenerator.GenerateAsync(new TenKPreparationRunwayCoreGenerationRequest(
                    request.Candidate, authority.RunwayDateDecision.CoreStartDate, request.RaceDate,
                    request.AsOfDate, request.PreferredDays, request.LongRunDayPreference,
                    request.ConditionResults, request.PreviewRequest, request.ResolverInput), ct);
            }
            catch (Exception exception)
            {
                return Fail(TenKPreparationRunwayOrchestrationStage.CoreGeneration,
                    TenKPreparationRunwayOrchestrationFailureCode.CoreGenerationFailed,
                    "DynamicCoreCalendarMaterializationOrchestrator", exception.GetType().Name, exception.Message, trace);
            }
            var coreVolume = core.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
            var coreNumericTarget = PreparationRunwayCoreWeekOneTargetAdapter.FromAuthoritativeCoreBehavior(coreVolume, core.PrescriptionResult.FinalPrescribedPlan);
            var corePaceTarget = PreparationRunwayCoreWeekOnePaceAdapter.FromAuthoritativeCoreBehavior(core.PrescriptionResult.FinalPrescribedPlan);
            var corePrescriptionContext = core.PrescriptionResult.VolumeResult.PrescriptionContext;
            Add(trace, TenKPreparationRunwayOrchestrationStage.CoreGeneration, "authoritative_core",
                request.Candidate.CandidateKey, request.Candidate.CandidateVersion, "PASS",
                ("coreWeeks", core.PrescriptionResult.FinalPrescribedPlan.Weeks.Count),
                ("coreStart", authority.RunwayDateDecision.CoreStartDate),
                ("weekOneVolumeKm", coreNumericTarget.WeeklyVolumeKm),
                ("paceSource", corePrescriptionContext.PaceSource.Source));

            // Stage 9: starting evidence is adapted from that same Core context.
            PreparationRunwayStartingLoadEvidence startingEvidence;
            try
            {
                startingEvidence = PreparationRunwayStartingLoadEvidenceAdapter.FromAuthoritativeCoreContext(corePrescriptionContext);
            }
            catch (InvalidOperationException exception)
            {
                return Fail(TenKPreparationRunwayOrchestrationStage.NumericMaterialization,
                    TenKPreparationRunwayOrchestrationFailureCode.NumericMaterializationFailed,
                    nameof(PreparationRunwayStartingLoadEvidenceAdapter), exception.GetType().Name, exception.Message, trace);
            }
            var numeric = _numericStage.Materialize(new PreparationRunwayNumericMaterializationRequest<PreparationRunwayBlockType>(
                profile.Value, structural.Weeks, startingEvidence, coreNumericTarget,
                TenKPreparationRunwayNumericPolicyFactory.Build(request.Candidate), request.Unit));
            if (!numeric.IsSuccess)
                return Fail(TenKPreparationRunwayOrchestrationStage.NumericMaterialization,
                    TenKPreparationRunwayOrchestrationFailureCode.NumericMaterializationFailed,
                    nameof(PreparationRunwayNumericMaterializer), numeric.FailureCode?.ToString(),
                    numeric.FailureReason ?? "Numeric materialization failed.", trace);
            Add(trace, TenKPreparationRunwayOrchestrationStage.NumericMaterialization, "numeric_runway",
                TenKPreparationRunwayNumericPolicyFactory.PolicyKey, TenKPreparationRunwayNumericPolicyFactory.PolicyVersion,
                "PASS", ("weekCount", numeric.PrescribedWeeks!.Count), ("coreBoundaryKm", coreNumericTarget.WeeklyVolumeKm));

            // Stage 10: the one existing runway/Core calendar composer.
            var calendar = _calendarStage.Compose(new PreparationRunwayCalendarCompositionRequest<PreparationRunwayBlockType>(
                authority, request.PreferredDays, request.LongRunDayPreference, profile.Value,
                request.Candidate.CandidateKey, request.Candidate.CandidateVersion,
                numeric, coreNumericTarget, core.PrescriptionResult.VolumeResult.BindingResult.Skeleton,
                TenKPreparationRunwayCalendarCompositionPolicyFactory.Build()));
            if (!calendar.IsSuccess)
                return Fail(TenKPreparationRunwayOrchestrationStage.CalendarComposition,
                    TenKPreparationRunwayOrchestrationFailureCode.CalendarCompositionFailed,
                    nameof(PreparationRunwayCalendarComposer), calendar.FailureCode?.ToString(),
                    calendar.FailureReason ?? "Calendar composition failed.", trace);
            Add(trace, TenKPreparationRunwayOrchestrationStage.CalendarComposition, "combined_calendar",
                TenKPreparationRunwayCalendarCompositionPolicyFactory.PolicyKey,
                TenKPreparationRunwayCalendarCompositionPolicyFactory.PolicyVersion, "PASS",
                ("runwayStart", authority.RunwayStartDate), ("coreStart", authority.RunwayDateDecision.CoreStartDate),
                ("combinedWeeks", calendar.OrderedCombinedWeeks!.Count));

            // Stage 11: pace context is adapted from the same Core prescription context.
            var paceContext = PreparationRunwayPaceContextAdapter.FromAuthoritativeCoreContext(
                request.Candidate.CandidateKey, request.Candidate.CandidateVersion,
                corePrescriptionContext.PaceSource, request.PreviewRequest.TargetFinishTimeSource,
                "authoritative Core CatalogPlanPrescriptionContext from this orchestration");
            var pace = _paceStage.Materialize(new PreparationRunwayPaceMaterializationRequest<PreparationRunwayBlockType>(
                calendar, paceContext, corePaceTarget, TenKPreparationRunwayPacePolicyFactory.Build()));
            if (!pace.IsSuccess)
                return Fail(TenKPreparationRunwayOrchestrationStage.PaceMaterialization,
                    TenKPreparationRunwayOrchestrationFailureCode.PaceMaterializationFailed,
                    nameof(PreparationRunwayPaceMaterializer), pace.FailureCode?.ToString(),
                    pace.FailureReason ?? "Pace materialization failed.", trace);
            Add(trace, TenKPreparationRunwayOrchestrationStage.PaceMaterialization, "paced_runway",
                TenKPreparationRunwayPacePolicyFactory.PolicyKey, TenKPreparationRunwayPacePolicyFactory.PolicyVersion,
                "PASS", ("evidenceState", paceContext.EvidenceState), ("pacedWeeks", pace.PacedRunwayWeeks!.Count));

            // Stage 12: full cross-component closure.
            var invariants = _finalValidator.Validate(
                request, runwayWeeks, allocation, loaded, bindings, structural, core, numeric, calendar, pace);
            if (!invariants.IsValid)
                return Fail(TenKPreparationRunwayOrchestrationStage.FinalInvariantValidation,
                    TenKPreparationRunwayOrchestrationFailureCode.FinalInvariantValidationFailed,
                    nameof(TenKPreparationRunwayFinalInvariantValidator), "FINAL_INVARIANTS_FALSE",
                    string.Join(", ", invariants.Findings), trace);
            Add(trace, TenKPreparationRunwayOrchestrationStage.FinalInvariantValidation, "end_to_end_invariants",
                "TEN_K_PREPARATION_RUNWAY_FINAL_INVARIANTS", 1, "PASS",
                ("totalWeeks", invariants.TotalWeeks), ("totalSessions", invariants.TotalSessions));

            return TenKPreparationRunwayDarkOrchestrationResult.Success(
                horizon, profile.Value, allocation, loaded, bindings, structural, core, numeric, calendar, pace, invariants, trace);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return Fail(TenKPreparationRunwayOrchestrationStage.FinalInvariantValidation,
                TenKPreparationRunwayOrchestrationFailureCode.OrchestrationInvariantViolation,
                nameof(TenKPreparationRunwayDarkOrchestrator), exception.GetType().Name, exception.Message, trace);
        }
    }

    private static (TenKPreparationRunwayOrchestrationFailureCode Code, string Reason)? ValidateRequest(
        TenKPreparationRunwayDarkOrchestrationRequest? request)
    {
        if (request is null || request.Candidate is null || request.CoreEntryReadinessResult is null ||
            request.ConditionResults is null || request.PreviewRequest is null || request.ResolverInput is null ||
            request.PreferredDays is null)
            return (TenKPreparationRunwayOrchestrationFailureCode.InvalidOrchestrationRequest, "Request and all authoritative contexts are required.");
        if (!PreviewRouting.V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayCandidate(request.Candidate.CandidateKey, request.Candidate.CandidateVersion) ||
            request.Candidate.CanonicalDistanceFamily != "TEN_K" || request.Candidate.Level != "INTERMEDIATE" ||
            request.Candidate.CoreCycle.DefaultWeeks != 12 || request.Candidate.CoreCycle.MaximumWeeks is null)
            return (TenKPreparationRunwayOrchestrationFailureCode.CandidateNotSupported, "Only the approved TEN_K Intermediate 4D/5D Preparation Runway candidates are supported.");
        if (!request.ConditionResults.Any(r => ReferenceEquals(r, request.CoreEntryReadinessResult)))
            return (TenKPreparationRunwayOrchestrationFailureCode.InvalidOrchestrationRequest, "The readiness result must be the same resolved object carried into Core ConditionResults.");
        if (request.PreviewRequest.StartDate != request.StartDate || request.PreviewRequest.RaceDate != request.RaceDate ||
            request.ResolverInput.StartDate != request.StartDate || request.ResolverInput.RaceDate != request.RaceDate ||
            request.ResolverInput.CanonicalDistanceFamily != request.Candidate.CanonicalDistanceFamily ||
            request.ResolverInput.RequestedTargetDistanceKm != 10d ||
            request.PreviewRequest.DaysPerWeek != request.Candidate.DaysPerWeek ||
            request.ResolverInput.DaysPerWeek != request.Candidate.DaysPerWeek ||
            request.PreviewRequest.Level != RunningBackground.Intermediate ||
            request.ResolverInput.Level != RunningBackground.Intermediate ||
            !SharedInputAuthorityMatches(request))
            return (TenKPreparationRunwayOrchestrationFailureCode.InvalidOrchestrationRequest, "Preview, resolver, candidate, and orchestration date/distance contexts must be value-identical.");
        if (request.Unit != PreparationRunwayQuantityUnit.Kilometers)
            return (TenKPreparationRunwayOrchestrationFailureCode.InvalidOrchestrationRequest, "Pilot runway quantity unit must be kilometers.");
        return null;
    }

    private static bool SharedInputAuthorityMatches(TenKPreparationRunwayDarkOrchestrationRequest request)
    {
        var preferred = request.PreferredDays.Select(ToWeekday).ToHashSet();
        var preview = request.PreviewRequest;
        var resolver = request.ResolverInput;
        var recentRaceMatches = preview.RecentRace is null
            ? resolver.RecentRaceDistanceKm is null && resolver.RecentRaceFinishTimeSeconds is null && resolver.RecentRaceDate is null
            : resolver.RecentRaceDistanceKm == 10d &&
              resolver.RecentRaceFinishTimeSeconds == preview.RecentRace.FinishTimeSeconds &&
              resolver.RecentRaceDate == preview.RecentRace.RaceDate;

        return preview.PreferredDays.ToHashSet().SetEquals(preferred) &&
               resolver.PreferredDays is not null && resolver.PreferredDays.ToHashSet().SetEquals(preferred) &&
               preview.LongRunDay == ToWeekday(request.LongRunDayPreference) &&
               resolver.LongRunDay == ToWeekday(request.LongRunDayPreference) &&
               preview.TargetFinishTimeSeconds == resolver.TargetFinishTimeSeconds &&
               preview.TargetFinishTimeSource == resolver.TargetFinishTimeSource &&
               preview.RecentWeeklyVolumeKm == resolver.RecentWeeklyVolumeKm &&
               preview.RecentLongestRunKm == resolver.RecentLongestRunKm &&
               preview.RecentRunsPerWeek == resolver.RecentRunsPerWeek &&
               recentRaceMatches;
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
        _ => throw new ArgumentOutOfRangeException(nameof(day), day, null),
    };

    private static PreparationRunwayAllocationProfile? MapProfile(RuntimeConditionResolutionResult readiness) =>
        readiness.Status != RuntimeConditionResolutionStatus.Evaluated ? null : readiness.OutputValue switch
        {
            "READY" => PreparationRunwayAllocationProfile.CoreEntryReady,
            "CAUTION" or "NOT_READY" => PreparationRunwayAllocationProfile.ConsistencyNeeded,
            _ => null,
        };

    private static TenKPreparationRunwayDarkOrchestrationResult Fail(
        TenKPreparationRunwayOrchestrationStage stage,
        TenKPreparationRunwayOrchestrationFailureCode code,
        string component,
        string? originalCode,
        string reason,
        List<TenKPreparationRunwayOrchestrationTraceEntry> trace)
    {
        Add(trace, stage, "failure", component, null, "FAIL",
            ("topLevelCode", code), ("originalCode", originalCode ?? "none"), ("reason", reason));
        return TenKPreparationRunwayDarkOrchestrationResult.Failed(
            new TenKPreparationRunwayDarkOrchestrationFailure(stage, code, component, originalCode, reason), trace);
    }

    private static void Add(
        List<TenKPreparationRunwayOrchestrationTraceEntry> trace,
        TenKPreparationRunwayOrchestrationStage stage,
        string subject,
        string source,
        int? version,
        string outcome,
        params (string Key, object? Value)[] details) =>
        trace.Add(new TenKPreparationRunwayOrchestrationTraceEntry(
            trace.Count + 1, stage, subject, source, version, outcome,
            details.ToDictionary(d => d.Key, d => d.Value?.ToString() ?? "null", StringComparer.Ordinal)));
}
