using Microsoft.Extensions.Options;
using RunningApp.Application.Exceptions;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Execution;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;

internal interface ITenKPreparationRunwayProgressionLoader
{
    Task<PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType>> LoadValidatedAsync(
        PreparationRunwayBlockType block,
        PlanCatalogReference reference,
        CancellationToken ct);
}

internal interface ITenKPreparationRunwayCoreGenerator
{
    Task<DynamicCoreCalendarMaterializationResult> GenerateAsync(
        TenKPreparationRunwayCoreGenerationRequest request,
        CancellationToken ct);
}

internal interface ITenKPreparationRunwayNumericStage
{
    PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType> Materialize(
        PreparationRunwayNumericMaterializationRequest<PreparationRunwayBlockType> request);
}

internal interface ITenKPreparationRunwayCalendarStage
{
    PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType> Compose(
        PreparationRunwayCalendarCompositionRequest<PreparationRunwayBlockType> request);
}

internal interface ITenKPreparationRunwayPaceStage
{
    PreparationRunwayPaceMaterializationResult<PreparationRunwayBlockType> Materialize(
        PreparationRunwayPaceMaterializationRequest<PreparationRunwayBlockType> request);
}

internal sealed class TenKPreparationRunwayProgressionLoader : ITenKPreparationRunwayProgressionLoader
{
    private readonly string _catalogRoot;
    private readonly ICatalogWorkoutDefinitionLoader _workoutLoader;

    public TenKPreparationRunwayProgressionLoader(string catalogRoot, ICatalogWorkoutDefinitionLoader workoutLoader)
    {
        _catalogRoot = catalogRoot;
        _workoutLoader = workoutLoader;
    }

    public async Task<PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType>> LoadValidatedAsync(
        PreparationRunwayBlockType block,
        PlanCatalogReference reference,
        CancellationToken ct)
    {
        var loaded = await PreparationRunwayBlockProgressionCatalogReader.LoadAsync(
            _catalogRoot, reference.Key, reference.Version, ct);
        var expectedBlock = TenKPreparationRunwayProgressionPolicyFactory.CatalogBlockName(block);
        if (!string.Equals(loaded.BlockKey, expectedBlock, StringComparison.Ordinal))
            throw new PlanCatalogLoadException(
                $"Progression '{reference.Key}' v{reference.Version} declares block '{loaded.BlockKey}', expected '{expectedBlock}'.");

        foreach (var step in loaded.OrderedSteps)
        {
            var failure = await PreparationRunwayBlockWorkoutReferenceValidator.ValidateAsync(
                _workoutLoader, new PreparationRunwayWorkoutReference(step.WorkoutId, step.WorkoutVersion), ct);
            if (failure is not null)
                throw new PlanCatalogLoadException(
                    $"Progression '{reference.Key}' v{reference.Version} step {step.StepNumber} failed workout validation: {failure}.");
        }

        return new PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType>(
            loaded.ProgressionId, loaded.Version, block, loaded.OrderedSteps);
    }
}

internal sealed class TenKPreparationRunwayCoreGenerator : ITenKPreparationRunwayCoreGenerator
{
    private readonly IDynamicCoreCalendarMaterializationOrchestrator _orchestrator;
    private readonly ICatalogWorkoutDefinitionLoader _workoutLoader;
    private readonly ICatalogPeakVolumeBandLoader _peakVolumeLoader;
    private readonly IPublishedTemplateBundleLoader? _publishedBundleLoader;

    public TenKPreparationRunwayCoreGenerator(
        IDynamicCoreCalendarMaterializationOrchestrator orchestrator,
        ICatalogWorkoutDefinitionLoader workoutLoader,
        ICatalogPeakVolumeBandLoader peakVolumeLoader,
        IPublishedTemplateBundleLoader? publishedBundleLoader = null)
    {
        _orchestrator = orchestrator;
        _workoutLoader = workoutLoader;
        _peakVolumeLoader = peakVolumeLoader;
        _publishedBundleLoader = publishedBundleLoader;
    }

    /// <summary>
    /// Phase 10K-FREQ.6D.7: loads the same real, exact-identity published bundle
    /// <see cref="RunningApp.Application.RuntimeCatalog.PreviewRouting.CatalogPreviewGenerator"/>'s
    /// own dynamic (Compressed/Extended) Core paths already load for
    /// <see cref="ExecutionPrescriptionIndex"/> resolution -- closing the same
    /// missing-context gap FREQ.6D.4D.5G fixed there, for this Runway-owned
    /// Core-call site. Every existing Intermediate 4D caller has no published
    /// bundle (Legacy prescription, unaffected); Intermediate 5D's real Core
    /// Week 1 KEY_SESSION is ProfileBacked and requires this index to resolve.
    /// </summary>
    public async Task<DynamicCoreCalendarMaterializationResult> GenerateAsync(
        TenKPreparationRunwayCoreGenerationRequest request,
        CancellationToken ct)
    {
        var corePreview = PreparationRunwayCoreInputAdapter.ForCore(request.PreviewRequest, request.CoreStartDate);
        var coreResolver = PreparationRunwayCoreInputAdapter.ForCore(request.ResolverInput, request.CoreStartDate);
        var executionIndex = await LoadExecutionIndexAsync(request.Candidate, ct);
        return await _orchestrator.MaterializeAsync(new DynamicCoreCalendarMaterializationContext
        {
            Candidate = request.Candidate,
            TargetWeekCount = request.Candidate.CoreCycle.DefaultWeeks,
            StartDate = request.CoreStartDate,
            RaceDate = request.RaceDate,
            AsOfDate = request.AsOfDate,
            PreferredDays = request.PreferredDays,
            LongRunDayPreference = request.LongRunDayPreference,
            ConditionResults = request.ConditionResults,
            PreviewRequest = corePreview,
            ResolverInput = coreResolver,
            WorkoutDefinitionLoader = _workoutLoader,
            PeakVolumeBandLoader = _peakVolumeLoader,
            ExecutionIndex = executionIndex,
        }, ct);
    }

    private async Task<ExecutionPrescriptionIndex?> LoadExecutionIndexAsync(PlanCatalogCandidateSummary candidate, CancellationToken ct)
    {
        if (_publishedBundleLoader is null) return null;
        var bundle = await _publishedBundleLoader.TryLoadAsync(candidate.CandidateKey, candidate.CandidateVersion, ct);
        return bundle is not null ? ExecutionPrescriptionIndex.Build(bundle) : null;
    }
}

internal static class PreparationRunwayCoreInputAdapter
{
    public static GeneratePreviewRequest ForCore(GeneratePreviewRequest source, DateOnly coreStartDate) => new()
    {
        GoalType = source.GoalType,
        GoalDistance = source.GoalDistance,
        Level = source.Level,
        DaysPerWeek = source.DaysPerWeek,
        Unit = source.Unit,
        RaceName = source.RaceName,
        RaceDate = source.RaceDate,
        TargetFinishTimeSeconds = source.TargetFinishTimeSeconds,
        TargetFinishTimeSource = source.TargetFinishTimeSource,
        StartDate = coreStartDate,
        PreferredDays = source.PreferredDays,
        WeeklyAvailability = source.WeeklyAvailability,
        PreferredPace = source.PreferredPace,
        LongRunDay = source.LongRunDay,
        HabitPlanType = source.HabitPlanType,
        CustomGoalType = source.CustomGoalType,
        CustomDurationWeeks = source.CustomDurationWeeks,
        CustomTargetTimeSeconds = source.CustomTargetTimeSeconds,
        RecentLongestRunKm = source.RecentLongestRunKm,
        RecentWeeklyVolumeKm = source.RecentWeeklyVolumeKm,
        RecentRunsPerWeek = source.RecentRunsPerWeek,
        RecentRace = source.RecentRace,
    };

    public static ResolverInputSnapshot ForCore(ResolverInputSnapshot source, DateOnly coreStartDate) => new()
    {
        RequestedTargetDistanceKm = source.RequestedTargetDistanceKm,
        CanonicalDistanceFamily = source.CanonicalDistanceFamily,
        GoalType = source.GoalType,
        GoalDistance = source.GoalDistance,
        GoalDistanceKm = source.GoalDistanceKm,
        StartDate = coreStartDate,
        RaceDate = source.RaceDate,
        RaceName = source.RaceName,
        TargetFinishTimeSeconds = source.TargetFinishTimeSeconds,
        TargetFinishTimeSource = source.TargetFinishTimeSource,
        DaysPerWeek = source.DaysPerWeek,
        PreferredDays = source.PreferredDays,
        LongRunDay = source.LongRunDay,
        WeeklyAvailability = source.WeeklyAvailability,
        PreferredPace = source.PreferredPace,
        Level = source.Level,
        RecentLongestRunKm = source.RecentLongestRunKm,
        RecentWeeklyVolumeKm = source.RecentWeeklyVolumeKm,
        RecentRunsPerWeek = source.RecentRunsPerWeek,
        RecentRaceDistanceKm = source.RecentRaceDistanceKm,
        RecentRaceFinishTimeSeconds = source.RecentRaceFinishTimeSeconds,
        RecentRaceDate = source.RecentRaceDate,
    };
}

internal sealed class TenKPreparationRunwayNumericStage : ITenKPreparationRunwayNumericStage
{
    public PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType> Materialize(
        PreparationRunwayNumericMaterializationRequest<PreparationRunwayBlockType> request) =>
        PreparationRunwayNumericMaterializer.Materialize(request);
}

internal sealed class TenKPreparationRunwayCalendarStage : ITenKPreparationRunwayCalendarStage
{
    private readonly PreparationRunwayCalendarComposer _composer;

    public TenKPreparationRunwayCalendarStage(PreparationRunwayCalendarComposer composer) => _composer = composer;

    public PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType> Compose(
        PreparationRunwayCalendarCompositionRequest<PreparationRunwayBlockType> request) => _composer.Compose(request);
}

internal sealed class TenKPreparationRunwayPaceStage : ITenKPreparationRunwayPaceStage
{
    public PreparationRunwayPaceMaterializationResult<PreparationRunwayBlockType> Materialize(
        PreparationRunwayPaceMaterializationRequest<PreparationRunwayBlockType> request) =>
        PreparationRunwayPaceMaterializer.Materialize(request);
}

internal static class PreparationRunwayStartingLoadEvidenceAdapter
{
    public static PreparationRunwayStartingLoadEvidence FromAuthoritativeCoreContext(
        CatalogPlanPrescriptionContext context)
    {
        var readiness = context.PlanLevelReadiness;
        return new PreparationRunwayStartingLoadEvidence(
            State(readiness.WeeklyVolume.State, readiness.WeeklyVolume.Kilometers),
            readiness.WeeklyVolume.Kilometers,
            State(readiness.LongestRun.State, readiness.LongestRun.Kilometers),
            readiness.LongestRun.Kilometers,
            readiness.RecentRunsPerWeek,
            "CatalogPrescriptionContextBuilder normalized readiness; shared with authoritative Core generation");
    }

    private static PreparationRunwayLoadEvidenceState State(PrescriptionInputState state, double? value) => state switch
    {
        PrescriptionInputState.Available when value == 0 => PreparationRunwayLoadEvidenceState.NoRecentRunningBase,
        PrescriptionInputState.Available when value is > 0 => PreparationRunwayLoadEvidenceState.Provided,
        PrescriptionInputState.NotProvided => PreparationRunwayLoadEvidenceState.Missing,
        _ => throw new InvalidOperationException($"Core readiness input state '{state}' is not usable for runway numeric materialization."),
    };
}

internal static class TenKPreparationRunwayDarkOrchestratorFactory
{
    public static TenKPreparationRunwayDarkOrchestrator Create(PlanCatalogOptions options)
    {
        var configured = Options.Create(options);
        var workoutLoader = new CatalogWorkoutDefinitionLoader(configured);
        var peakLoader = new CatalogPeakVolumeBandLoader(configured);
        var core = new DynamicCoreCalendarMaterializationOrchestrator(
            new DynamicCoreSessionPrescriptionOrchestrator(
                new DynamicCoreVolumeAndLongRunOrchestrator(
                    new DynamicCoreWorkoutBindingOrchestrator(
                        new DynamicCoreWeekSkeletonOrchestrator(
                            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
                            new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
                        new CatalogWorkoutProgressionLoader(configured),
                        new ProgressionStageAllocator(),
                        new GeneratedCatalogStageScheduleValidator(),
                        new CatalogWeekSkeletonCalendarMaterializer(),
                        new DatedGeneratedCatalogPlanSkeletonValidator(),
                        new CatalogWorkoutBinder(),
                        new BoundCatalogPlanValidator()),
                    new CatalogPrescriptionContextBuilder(),
                    new CatalogVolumeAndLongRunPlanner()),
                new CatalogSessionPrescriptionPlanner(),
                new CatalogFinalPrescribedPlanFinalizer()));
        var calendar = new PreparationRunwayCalendarComposer(
            new CatalogWeekSkeletonCalendarMaterializer(),
            new DatedGeneratedCatalogPlanSkeletonValidator());
        var publishedBundleLoader = new PublishedTemplateBundleLoader(options, options.CatalogRootPath);

        return new TenKPreparationRunwayDarkOrchestrator(
            workoutLoader,
            new TenKPreparationRunwayProgressionLoader(options.CatalogRootPath, workoutLoader),
            new TenKPreparationRunwayCoreGenerator(core, workoutLoader, peakLoader, publishedBundleLoader),
            new TenKPreparationRunwayNumericStage(),
            new TenKPreparationRunwayCalendarStage(calendar),
            new TenKPreparationRunwayPaceStage(),
            new TenKPreparationRunwayFinalInvariantValidator());
    }
}
