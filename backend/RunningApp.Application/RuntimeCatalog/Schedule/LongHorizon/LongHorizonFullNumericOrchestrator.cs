using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Execution;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.6A — completes the numeric execution Phase 4I.6 deliberately left
/// open: real Preparation Runway numeric/pace/calendar execution and real
/// Core numeric/pace/binding execution for the 21-52 week path, using the
/// EXISTING, UNCHANGED production authorities throughout (never a rewritten
/// algorithm; only the Runway entry EVIDENCE source changes, per the
/// established "adapt input, not algorithm" precedent).
///
/// Design decision (documented, not merely implemented): Core's own Week-1
/// numeric target/boundary is computed from the ORIGINAL, unmodified user
/// evidence -- exactly as the existing 15-20 week
/// <see cref="TenKPreparationRunwayDarkOrchestrator"/> already does -- rather
/// than from the Runway exit. This avoids a circular dependency (Runway needs
/// a boundary to ramp toward; Core's own boundary must therefore be resolved
/// independently of Runway's own output) and preserves the EXACT existing
/// Runway-to-Core transition mechanism and Core's own evidence-derived
/// prescription byte-for-byte, matching this phase's explicit "do not
/// rebuild Runway or Core numeric algorithms" boundary. The only Long-Horizon
/// specific change is Preparation Runway's ENTRY evidence: for 21-52 week
/// plans it is the GE exit state (<see cref="LongHorizonGeExitState"/>)
/// instead of the existing Core-derived
/// <c>PreparationRunwayStartingLoadEvidenceAdapter.FromAuthoritativeCoreContext</c>
/// value the 15-20 week path uses.
/// </summary>
internal static class LongHorizonFullNumericOrchestrator
{
    public const string RunwayEntryContextSourceGeExit = "PrecedingGeneralEnduranceExit";

    /// <summary>Phase 10K-FREQ.6D.14 -- the pinned Process A release whose published bundles carry ProfileBacked execution prescriptions for the real 5D Core Week 1 KEY_SESSION, reusing the exact same version FREQ.6D.13's own dark 5D tests already verify against.
    /// Phase 10K-FREQ.6D.26 -- bumped to 1.2.0, a byte-identical superset of 1.1.0 for every pre-existing document, additionally carrying the TEN_K__6D__INTERMEDIATE candidate.</summary>
    private const string RealPublishedBundleReleaseVersion = "1.2.0";

    /// <param name="daysPerWeek">
    /// Phase 10K-FREQ.6D.14 -- the resolved session cardinality. Defaults to
    /// 4, reproducing every pre-FREQ.6D.14 caller byte-for-byte (GE structure/
    /// numeric policy, candidate identity, and Runway entry evidence all
    /// resolve to the exact same 4D values as before). Only 4 and 5 are
    /// recognized; any other value fails closed rather than silently
    /// falling back to 4D.
    /// </param>
    public static async Task<LongHorizonExecutedSchedule> ExecuteAsync(
        LongHorizonCompositionDecision decision,
        DateOnly startDate,
        DateOnly raceDate,
        LongHorizonGeEntryBaselineInput geBaseline,
        IReadOnlyList<DayOfWeek> preferredDays,
        DayOfWeek longRunDay,
        string catalogRoot,
        ICatalogWorkoutDefinitionLoader workoutLoader,
        CancellationToken ct = default,
        int daysPerWeek = 4)
    {
        if (decision.HorizonPath != LongHorizonPath.LongHorizonGeneralEnduranceRunwayAndCore)
            throw new InvalidOperationException(
                $"LongHorizonFullNumericOrchestrator only accepts HorizonPath={LongHorizonPath.LongHorizonGeneralEnduranceRunwayAndCore}; received {decision.HorizonPath}.");
        if (daysPerWeek is not (4 or 5))
            throw new InvalidOperationException($"LongHorizonFullNumericOrchestrator only recognizes daysPerWeek 4 or 5; received {daysPerWeek}.");
        var geWeeks = decision.GeneralEnduranceWeeks ?? throw new InvalidOperationException("GeneralEnduranceWeeks is required.");
        var profile = decision.ReadinessProfile ?? throw new InvalidOperationException("ReadinessProfile is required.");
        if (decision.PreparationRunwayWeeks != 8) throw new InvalidOperationException("PreparationRunwayWeeks must be exactly 8.");
        if (decision.CoreWeeks != 12) throw new InvalidOperationException("CoreWeeks must be exactly 12.");

        // ── Stage 1: GE (fully reused from Phase 4I.6; Phase 10K-FREQ.6D.14 threads the resolved EASY cardinality and, for 5D/6D, the already-approved policy + target cap; Phase 10K-FREQ.6D.26 generalized the dispatch off DaysPerWeek-2 / VolumeSafetyPolicy.ForIntermediateDaysPerWeek) ──
        var easySupportCount = daysPerWeek - 2;
        var geVolumePolicy = VolumeSafetyPolicy.ForIntermediateDaysPerWeek(daysPerWeek);
        var geDescriptors = LongHorizonGeStructuralSelector.Select(geWeeks, profile, easySupportCount);
        var geNumeric = LongHorizonGeNumericExecutor.Execute(geDescriptors, geBaseline, geVolumePolicy, applyTargetCap: daysPerWeek is 5 or 6);
        var geExit = LongHorizonGeExitState.From(geDescriptors, geNumeric, profile);

        // Phase 10K-FREQ.6D.14: real 5D Core Week 1 KEY_SESSION is ProfileBacked
        // and requires a real ExecutionPrescriptionIndex to resolve -- the exact
        // same missing-context gap FREQ.6D.13 found and fixed for the rolling-
        // activation JIT path (LongHorizonRollingJitCompositionOrchestrator),
        // reused verbatim here for this orchestrator's own separately-built Core
        // pipeline. Every existing 4D caller has no published bundle for this
        // release, so PublishedBundleReleaseVersion resolves to a no-op there.
        var planCatalogOptions = new PlanCatalogOptions { CatalogRootPath = catalogRoot, PublishedBundleReleaseVersion = RealPublishedBundleReleaseVersion };
        var options = Options.Create(planCatalogOptions);
        var bundleLoader = new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        var candidate = daysPerWeek == 5
            ? await gate.LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.FiveDayCandidateKey, V1CatalogPilotIdentityPolicy.FiveDayCandidateVersion)
            : await gate.LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);

        // ── Stage 2: real horizon decision + pure-date-arithmetic calendar authority ──
        // The reused Preparation Runway calendar authority represents ONLY the
        // Runway->Core span (8+12=20 weeks) -- exactly the same shape the existing
        // 15-20 week path already uses -- anchored at GE's own end date, not the
        // overall plan StartDate. This keeps its internal "AvailableFullWeeks -
        // PreferredCoreWeeks = RunwayWeeks" invariant consistent with this
        // orchestrator's actual 8-week Runway numeric output.
        var geEndDate = LongHorizonCalendarAssigner.WeekStartDate(startDate, geWeeks + 1);
        var horizonContext = new CoreHorizonContext(
            geEndDate, raceDate, candidate.CoreCycle.MinimumWeeks, candidate.CoreCycle.DefaultWeeks, candidate.CoreCycle.MaximumWeeks!.Value);
        var coreHorizon = RaceHorizonPolicy.Decide(
            geEndDate, raceDate, candidate.CoreCycle.MinimumWeeks, candidate.CoreCycle.DefaultWeeks, candidate.CoreCycle.MaximumWeeks!.Value);
        var authority = PreparationRunwayCalendarAuthorityAdapter.Create(horizonContext, coreHorizon);
        var coreStartDate = authority.RunwayDateDecision.CoreStartDate;

        // ── Stage 3: real GeneratePreviewRequest/ResolverInputSnapshot + REAL runtime-condition resolution (no fabricated stand-in) ──
        var previewRequest = BuildPreviewRequest(startDate, raceDate, preferredDays, longRunDay, geBaseline);
        var resolverInput = BuildResolverInputSnapshot(previewRequest);
        var resolutionService = new RuntimeConditionResolutionService(
            new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver());
        var conditionResults = resolutionService.ResolveAllResults(new RuntimeResolverContext
        {
            InputSnapshot = resolverInput,
            CoreCycle = candidate.CoreCycle,
            AsOfDate = startDate,
        });

        // ── Stage 4: real Core generation (unchanged pipeline, ORIGINAL evidence) -- also supplies Runway's exit boundary ──
        var workoutLoaderForCore = new CatalogWorkoutDefinitionLoader(options);
        var peakLoader = new CatalogPeakVolumeBandLoader(options);
        var corePipeline = BuildCorePipeline(options);
        var publishedBundleLoader = new PublishedTemplateBundleLoader(planCatalogOptions, catalogRoot);
        var coreGenerator = new TenKPreparationRunwayCoreGenerator(corePipeline, workoutLoaderForCore, peakLoader, publishedBundleLoader);
        var coreGenerationRequest = new TenKPreparationRunwayCoreGenerationRequest(
            candidate, coreStartDate, raceDate, startDate, preferredDays, longRunDay, conditionResults, previewRequest, resolverInput);
        var core = await coreGenerator.GenerateAsync(coreGenerationRequest, ct);

        var coreVolume = core.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var coreNumericTarget = PreparationRunwayCoreWeekOneTargetAdapter.FromAuthoritativeCoreBehavior(coreVolume, core.PrescriptionResult.FinalPrescribedPlan);
        var corePaceTarget = PreparationRunwayCoreWeekOnePaceAdapter.FromAuthoritativeCoreBehavior(core.PrescriptionResult.FinalPrescribedPlan);
        var corePrescriptionContext = core.PrescriptionResult.VolumeResult.PrescriptionContext;

        // ── Stage 5: Runway structural (allocation + binding + week materialization, unchanged from Phase 4I.5) ──
        var allocationProfile = profile == ReadinessProfile.ConsistencyNeeded
            ? PreparationRunwayAllocationProfile.ConsistencyNeeded
            : PreparationRunwayAllocationProfile.CoreEntryReady;
        var (structuralRunwayWeeks, allocationResult) = await MaterializeRunwayStructuralAsync(allocationProfile, candidate, catalogRoot, workoutLoader, ct);

        // ── Stage 6: Runway numeric -- GE-derived entry evidence, existing algorithm/policy unchanged ──
        var startingEvidence = new PreparationRunwayStartingLoadEvidence(
            PreparationRunwayLoadEvidenceState.Provided, geExit.FinalWeeklyVolumeKm,
            PreparationRunwayLoadEvidenceState.Provided, geExit.FinalLongRunKm,
            daysPerWeek, RunwayEntryContextSourceGeExit);

        var numeric = PreparationRunwayNumericMaterializer.Materialize(
            new PreparationRunwayNumericMaterializationRequest<PreparationRunwayBlockType>(
                allocationProfile, structuralRunwayWeeks, startingEvidence, coreNumericTarget,
                TenKPreparationRunwayNumericPolicyFactory.Build(candidate), PreparationRunwayQuantityUnit.Kilometers));
        if (!numeric.IsSuccess)
            throw new InvalidOperationException($"Preparation Runway numeric materialization failed: {numeric.FailureReason}");

        // ── Stage 7: Runway calendar (existing composer, unchanged) ──
        var calendarComposer = new PreparationRunwayCalendarComposer(new CatalogWeekSkeletonCalendarMaterializer(), new DatedGeneratedCatalogPlanSkeletonValidator());
        var calendar = calendarComposer.Compose(new PreparationRunwayCalendarCompositionRequest<PreparationRunwayBlockType>(
            authority, preferredDays, longRunDay, allocationProfile, candidate.CandidateKey, candidate.CandidateVersion,
            numeric, coreNumericTarget, core.PrescriptionResult.VolumeResult.BindingResult.Skeleton,
            TenKPreparationRunwayCalendarCompositionPolicyFactory.Build()));
        if (!calendar.IsSuccess)
            throw new InvalidOperationException($"Preparation Runway calendar composition failed: {calendar.FailureReason}");

        // ── Stage 8: Runway pace (existing materializer, unchanged) ──
        var paceContext = PreparationRunwayPaceContextAdapter.FromAuthoritativeCoreContext(
            candidate.CandidateKey, candidate.CandidateVersion, corePrescriptionContext.PaceSource,
            previewRequest.TargetFinishTimeSource, "authoritative Core CatalogPlanPrescriptionContext from this Long-Horizon orchestration");
        var pace = PreparationRunwayPaceMaterializer.Materialize(
            new PreparationRunwayPaceMaterializationRequest<PreparationRunwayBlockType>(
                calendar, paceContext, corePaceTarget, TenKPreparationRunwayPacePolicyFactory.Build()));
        if (!pace.IsSuccess)
            throw new InvalidOperationException($"Preparation Runway pace materialization failed: {pace.FailureReason}");

        // ── Stage 9: merge GE + Runway + Core into the final schedule ───────
        var weeks = new List<LongHorizonExecutedWeek>(geWeeks + 8 + 12);
        var globalWeekNumber = 1;

        foreach (var week in geNumeric)
        {
            var descriptor = geDescriptors[week.WeekIndex - 1];
            weeks.Add(BuildGeExecutedWeek(descriptor, week, globalWeekNumber++, startDate, preferredDays, longRunDay));
        }

        foreach (var pacedWeek in pace.PacedRunwayWeeks!)
            weeks.Add(BuildRunwayExecutedWeek(pacedWeek, globalWeekNumber++));

        foreach (var coreWeek in core.PrescriptionResult.FinalPrescribedPlan.Weeks)
            weeks.Add(BuildCoreExecutedWeek(coreWeek, globalWeekNumber++));

        var executed = new LongHorizonExecutedSchedule(
            Structural: await LongHorizonStructuralMaterializer.MaterializeAsync(decision, catalogRoot, workoutLoader, ct, daysPerWeek),
            StartDate: startDate,
            GeNumericExecutionComplete: true,
            RunwayNumericExecutionComplete: true,
            CoreNumericExecutionComplete: true,
            Weeks: weeks);

        var validation = LongHorizonFullExecutionValidator.Validate(executed, geExit, numeric, calendar, pace, core);
        if (!validation.IsValid)
            throw new InvalidOperationException(
                "LongHorizonFullNumericOrchestrator produced an invalid full-numeric schedule: " + string.Join("; ", validation.Findings));

        return executed;
    }

    // ── Runway structural reuse (allocation + binding, mirrors Phase 4I.5) ──

    private static async Task<(IReadOnlyList<PreparationRunwayMaterializedWeek<PreparationRunwayBlockType>> Weeks, PreparationRunwayAllocationEngineResult<PreparationRunwayBlockType> Allocation)>
        MaterializeRunwayStructuralAsync(
            PreparationRunwayAllocationProfile allocationProfile, PlanCatalogCandidateSummary candidate,
            string catalogRoot, ICatalogWorkoutDefinitionLoader workoutLoader, CancellationToken ct)
    {
        var policies = TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(allocationProfile);
        var allocationResult = PreparationRunwayBlockAllocationEngine.Allocate(8, policies);
        if (!allocationResult.IsSuccess || allocationResult.Allocations is null)
            throw new InvalidOperationException($"Preparation Runway allocation failed: {allocationResult.FailureReason}");

        var referencePolicy = TenKPreparationRunwayProgressionPolicyFactory.Build();
        var progressionLoader = new TenKPreparationRunwayProgressionLoader(catalogRoot, workoutLoader);

        var loadedByBlock = new Dictionary<PreparationRunwayBlockType, PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType>>();
        foreach (var block in allocationResult.Allocations.Where(a => a.AllocatedWeeks > 0).OrderBy(a => a.CanonicalOrder))
        {
            if (!referencePolicy.TryGetValue(block.BlockKey, out var reference))
                throw new InvalidOperationException($"No typed progression reference exists for positive block '{block.BlockKey}'.");
            loadedByBlock[block.BlockKey] = await progressionLoader.LoadValidatedAsync(block.BlockKey, reference, ct);
        }

        var bindings = new List<PreparationRunwayMaterializationBlockBinding<PreparationRunwayBlockType>>();
        foreach (var block in allocationResult.Allocations.Where(a => a.AllocatedWeeks > 0).OrderBy(a => a.CanonicalOrder))
        {
            var definition = loadedByBlock[block.BlockKey];
            var bound = PreparationRunwayBlockWorkoutBindingEngine.Bind(
                new PreparationRunwayBlockWorkoutBindingRequest<PreparationRunwayBlockType>(block.BlockKey, block.AllocatedWeeks, definition));
            if (!bound.IsSuccess || bound.Binding is null)
                throw new InvalidOperationException($"Preparation Runway workout binding failed for block '{block.BlockKey}': {bound.FailureReason}");
            var steps = Enumerable.Range(1, block.AllocatedWeeks).ToArray();
            bindings.Add(new PreparationRunwayMaterializationBlockBinding<PreparationRunwayBlockType>(
                block.BlockKey, bound.Binding, definition.ProgressionId, definition.Version, steps));
        }

        var structural = await PreparationRunwayWeekMaterializer.MaterializeAsync(
            new PreparationRunwayWeekMaterializationRequest<PreparationRunwayBlockType>(
                allocationProfile.ToString(), candidate.CandidateKey, candidate.CandidateVersion,
                TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyId,
                TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyVersion,
                TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout(candidate.DaysPerWeek),
                allocationResult.Allocations, bindings,
                TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies(),
                TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildSupportPolicy()),
            workoutLoader, ct);

        if (!structural.IsSuccess || structural.Weeks is null)
            throw new InvalidOperationException($"Preparation Runway structural materialization failed: {structural.FailureReason}");

        return (structural.Weeks, allocationResult);
    }

    private static IDynamicCoreCalendarMaterializationOrchestrator BuildCorePipeline(IOptions<PlanCatalogOptions> options) =>
        new DynamicCoreCalendarMaterializationOrchestrator(
            new DynamicCoreSessionPrescriptionOrchestrator(
                new DynamicCoreVolumeAndLongRunOrchestrator(
                    new DynamicCoreWorkoutBindingOrchestrator(
                        new DynamicCoreWeekSkeletonOrchestrator(
                            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
                            new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
                        new CatalogWorkoutProgressionLoader(options), new ProgressionStageAllocator(),
                        new GeneratedCatalogStageScheduleValidator(), new CatalogWeekSkeletonCalendarMaterializer(),
                        new DatedGeneratedCatalogPlanSkeletonValidator(), new CatalogWorkoutBinder(), new BoundCatalogPlanValidator()),
                    new CatalogPrescriptionContextBuilder(), new CatalogVolumeAndLongRunPlanner()),
                new CatalogSessionPrescriptionPlanner(), new CatalogFinalPrescribedPlanFinalizer()));

    private static GeneratePreviewRequest BuildPreviewRequest(
        DateOnly startDate, DateOnly raceDate, IReadOnlyList<DayOfWeek> preferredDays, DayOfWeek longRunDay,
        LongHorizonGeEntryBaselineInput geBaseline) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = preferredDays.Count,
        Unit = DistanceUnit.Km,
        StartDate = startDate,
        RaceDate = raceDate,
        TargetFinishTimeSeconds = 3480,
        TargetFinishTimeSource = TargetFinishTimeSource.ProductAverage,
        PreferredDays = preferredDays.Select(ToWeekday).ToArray(),
        LongRunDay = ToWeekday(longRunDay),
        RecentWeeklyVolumeKm = geBaseline.RecentWeeklyVolumeKm,
        RecentLongestRunKm = geBaseline.RecentLongestRunKm,
        RecentRunsPerWeek = geBaseline.RecentRunsPerWeek,
        RecentRace = null,
    };

    private static ResolverInputSnapshot BuildResolverInputSnapshot(GeneratePreviewRequest request) => new()
    {
        RequestedTargetDistanceKm = 10, CanonicalDistanceFamily = "TEN_K",
        GoalType = request.GoalType, GoalDistance = request.GoalDistance, GoalDistanceKm = 10,
        TargetFinishTimeSeconds = request.TargetFinishTimeSeconds, TargetFinishTimeSource = request.TargetFinishTimeSource,
        DaysPerWeek = request.DaysPerWeek, Level = request.Level,
        StartDate = request.StartDate, RaceDate = request.RaceDate, RaceName = request.RaceName,
        PreferredDays = request.PreferredDays, LongRunDay = request.LongRunDay,
        RecentLongestRunKm = request.RecentLongestRunKm, RecentWeeklyVolumeKm = request.RecentWeeklyVolumeKm,
        RecentRunsPerWeek = request.RecentRunsPerWeek,
        RecentRaceDistanceKm = null, RecentRaceFinishTimeSeconds = null, RecentRaceDate = null,
    };

    private static Weekday ToWeekday(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Weekday.Mon, DayOfWeek.Tuesday => Weekday.Tue, DayOfWeek.Wednesday => Weekday.Wed,
        DayOfWeek.Thursday => Weekday.Thu, DayOfWeek.Friday => Weekday.Fri, DayOfWeek.Saturday => Weekday.Sat,
        DayOfWeek.Sunday => Weekday.Sun, _ => throw new ArgumentOutOfRangeException(nameof(day)),
    };

    // ── Week builders ─────────────────────────────────────────────────────

    private static LongHorizonExecutedWeek BuildGeExecutedWeek(
        LongHorizonGeWeekDescriptor descriptor, LongHorizonGeWeekNumericResult numeric, int globalWeekNumber,
        DateOnly startDate, IReadOnlyList<DayOfWeek> preferredDays, DayOfWeek longRunDay)
    {
        var weekStartDate = LongHorizonCalendarAssigner.WeekStartDate(startDate, globalWeekNumber);
        var weekdayAssignments = LongHorizonCalendarAssigner.AssignWeekdays(preferredDays, longRunDay, preferredDays.Count);

        var slots = new List<LongHorizonExecutedWorkoutSlot>(2 + descriptor.EasySupportWorkouts.Count)
        {
            BuildGeSlot(1, "KEY_SESSION", descriptor.KeySessionWorkout, numeric.KeySessionDistanceKm, weekStartDate, weekdayAssignments),
        };
        var slotIndex = 2;
        for (var easyOrdinal = 0; easyOrdinal < descriptor.EasySupportWorkouts.Count; easyOrdinal++)
        {
            slots.Add(BuildGeSlot(
                slotIndex++, "EASY_SUPPORT", descriptor.EasySupportWorkouts[easyOrdinal], numeric.EasySupportDistancesKm[easyOrdinal],
                weekStartDate, weekdayAssignments, $"EASY_SUPPORT_{easyOrdinal + 1}"));
        }
        slots.Add(BuildGeSlot(slotIndex, "LONG_RUN", descriptor.LongRunWorkout, numeric.LongRunDistanceKm, weekStartDate, weekdayAssignments));

        var structuralWeek = new LongHorizonStructuralWeek(
            globalWeekNumber, descriptor.WeekIndex, LongHorizonSegmentType.LongHorizonGeneralEndurance,
            LongHorizonGeWeekDescriptor.LongHorizonGeneralEnduranceSegmentType, null, null,
            descriptor.Classification, descriptor.StageFamily, descriptor.MesocycleIndex, descriptor.MesocyclePosition,
            descriptor.IsRecoveryWeek, descriptor.IsTerminalAlignment,
            slots.Select(s => s.Structural).ToList());

        return new LongHorizonExecutedWeek(
            structuralWeek, numeric.TotalVolumeKm, numeric.LongRunDistanceKm,
            LongHorizonGeNumericExecutor.PolicyId, LongHorizonGeNumericExecutor.PolicyVersion, weekStartDate, slots);
    }

    private static LongHorizonExecutedWorkoutSlot BuildGeSlot(
        int index, string role, LongHorizonGeWorkoutReference reference, double distanceKm, DateOnly weekStartDate,
        IReadOnlyDictionary<string, DayOfWeek> weekdayAssignments, string? roleKeyOverride = null)
    {
        var roleKey = roleKeyOverride ?? role;
        var weekday = weekdayAssignments[roleKey];
        var structural = new LongHorizonStructuralWorkoutSlot(index, role, reference.Key, reference.Version, LongHorizonSegmentType.LongHorizonGeneralEndurance);
        return new LongHorizonExecutedWorkoutSlot(
            structural, reference.Key, reference.Version, weekday,
            LongHorizonCalendarAssigner.AssignedDate(weekStartDate, weekday), distanceKm,
            role == "LONG_RUN", "LongHorizonGeNumericExecutor");
    }

    private static LongHorizonExecutedWeek BuildRunwayExecutedWeek(PreparationRunwayPacedWeek<PreparationRunwayBlockType> pacedWeek, int globalWeekNumber)
    {
        var originalWeek = pacedWeek.OriginalWeek;
        var slots = pacedWeek.StructuralOrderedSlots.Select(pacedSlot =>
        {
            var original = pacedSlot.OriginalSlot;
            var structuralSlot = original.PrescribedSlot.StructuralSlot;
            var structural = new LongHorizonStructuralWorkoutSlot(
                structuralSlot.SlotOrdinal, RunwayRoleLabel(structuralSlot.SlotRole),
                structuralSlot.WorkoutId, structuralSlot.WorkoutVersion, LongHorizonSegmentType.PreparationRunway);
            return new LongHorizonExecutedWorkoutSlot(
                structural, structuralSlot.WorkoutId, structuralSlot.WorkoutVersion,
                original.SessionDayOfWeek, original.SessionDate, original.PrescribedSlot.PlannedDistanceKm,
                structuralSlot.SlotRole == PreparationRunwaySlotRole.LongRun, "LongHorizonFullNumericOrchestrator(Runway)");
        }).ToList();

        var structuralWeek = new LongHorizonStructuralWeek(
            globalWeekNumber, originalWeek.SegmentLocalWeekNumber, LongHorizonSegmentType.PreparationRunway,
            "PREPARATION_RUNWAY", originalWeek.PrescribedWeek.StructuralWeek.BlockType.ToString(), null,
            null, null, null, null, null, null, slots.Select(s => s.Structural).ToList());

        return new LongHorizonExecutedWeek(
            structuralWeek, originalWeek.PrescribedWeek.PlannedWeeklyVolumeKm, originalWeek.PrescribedWeek.PlannedLongRunDistanceKm,
            TenKPreparationRunwayNumericPolicyFactory.PolicyKey, TenKPreparationRunwayNumericPolicyFactory.PolicyVersion.ToString(),
            originalWeek.StartDate, slots);
    }

    private static string RunwayRoleLabel(PreparationRunwaySlotRole role) => role switch
    {
        PreparationRunwaySlotRole.KeySession => "KEY_SESSION",
        PreparationRunwaySlotRole.EasySupport => "EASY_SUPPORT",
        PreparationRunwaySlotRole.LongRun => "LONG_RUN",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    private static LongHorizonExecutedWeek BuildCoreExecutedWeek(CatalogPrescribedWeek week, int globalWeekNumber)
    {
        var slots = week.Sessions.Select((session, i) =>
        {
            var structural = new LongHorizonStructuralWorkoutSlot(
                i + 1, session.StructuralRole, session.WorkoutDefinitionKey, session.WorkoutDefinitionVersion, LongHorizonSegmentType.Core);
            return new LongHorizonExecutedWorkoutSlot(
                structural, session.WorkoutDefinitionKey, session.WorkoutDefinitionVersion,
                session.Date.DayOfWeek, session.Date, session.PlannedDistanceKm,
                session.StructuralRole == "LONG_RUN", "LongHorizonFullNumericOrchestrator(Core)");
        }).ToList();

        var structuralWeek = new LongHorizonStructuralWeek(
            globalWeekNumber, week.WeekNumber, LongHorizonSegmentType.Core, week.PhaseKey, null, week.PhaseKey,
            null, null, null, null, null, null, slots.Select(s => s.Structural).ToList());

        var weekStartDate = slots.Min(s => s.AssignedDate!.Value);
        return new LongHorizonExecutedWeek(
            structuralWeek, week.PlannedWeeklyVolumeKm, week.Sessions.Single(s => s.StructuralRole == "LONG_RUN").PlannedDistanceKm,
            "EXISTING_CORE_NUMERIC_AUTHORITY", "UNCHANGED", weekStartDate, slots);
    }
}
