using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Execution;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.Domain.Enums;

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
/// boundary). It produces the complete typed payload used by the public
/// preview and later confirmation; confirmation, not this generator, owns
/// TrainingPlan/TrainingWeek/TrainingDay persistence.
///
/// Backend Integration Phase 4F.4 introduced a DARK internal invocation of
/// <see cref="ICatalogPlanSkeletonOrchestrator"/> (Phase 4F.3) purely for its
/// validation side effect. As of Phase 4F.5-4F.9 that is no longer the
/// current behavior: this class now runs the full pipeline through to a
/// real <see cref="Schedule.GeneratedCatalogPlanPayload"/>, which IS stored
/// on <see cref="CatalogPreviewSnapshot.GeneratedPreviewPlanPayload"/>, IS
/// included in the snapshot's content hash (see
/// <see cref="CatalogPreviewSnapshotBuilder.Build"/>), and IS returned to the
/// public preview DTO via <see cref="ICatalogPublicPreviewMaterializer"/>.
/// Confirmation/persistence of this payload into a real
/// TrainingPlan/TrainingWeek/TrainingDay schedule is implemented and covered
/// by real PostgreSQL reconciliation tests
/// (<see cref="CatalogPlanConfirmationService"/>, Phase 4F.9+). See
/// PHASE4F_4_DARK_INTERNAL_SKELETON_WIRING_INTO_CATALOG_PREVIEW.md for the
/// original (now superseded) dark-wiring history.
/// </summary>
public interface ICatalogPreviewGenerator
{
    Task<CatalogPreviewSnapshot> GenerateAsync(GeneratePreviewRequest request, DateOnly asOfDate, CancellationToken ct = default);

    /// <summary>
    /// Backend Integration Phase 4G.6B — the scoped 15-20 week TEN_K
    /// Preparation Runway public preview path. Reuses the exact same
    /// candidate-loading, resolver-input-building, and condition-resolution
    /// steps as <see cref="GenerateAsync"/> (never duplicated), then invokes
    /// the existing, unmodified dark <c>TenKPreparationRunwayDarkOrchestrator</c>
    /// exactly once. The returned <see cref="CatalogPreviewSnapshot"/>
    /// always has <c>GeneratedPreviewPlanPayload = null</c> by design — this
    /// is what makes the resulting preview automatically non-persistable at
    /// confirm time via the existing, unmodified <c>CatalogPreviewNotPersistableException</c>
    /// guard, with zero new confirm-path code. The returned weeks list is
    /// built directly from the orchestrator's own final output and is never
    /// recalculated.
    ///
    /// Backend Integration Phase 4G.6B.1: <paramref name="horizonDecision"/>
    /// is the single authoritative <c>CoreHorizonDecision</c> computed once
    /// by <c>PlanServices.GeneratePreviewAsync</c>'s own routing gate. This
    /// method no longer calls <c>RaceHorizonPolicy.Decide</c> itself — it
    /// only validates the carried decision (mode, week range, and
    /// consistency with this candidate's own CoreCycle bounds) and carries
    /// it, unmodified, into <c>TenKPreparationRunwayDarkOrchestrationRequest</c>.
    /// </summary>
    /// <summary>
    /// Backend Integration Phase 4G.6C: <paramref name="confirmationEnabled"/>
    /// additionally builds a real, persistable
    /// <see cref="GeneratedCatalogPlanPayload"/> (via
    /// <c>PreparationRunwayPersistablePlanMapper</c>) and embeds it in the
    /// returned snapshot. Defaults false — behavior is then byte-for-byte
    /// identical to the pre-4G.6C shape (payload stays null, preview stays
    /// non-confirmable).
    /// </summary>
    Task<(CatalogPreviewSnapshot Snapshot, IReadOnlyList<PreviewWeekDto> Weeks)> GeneratePreparationRunwayPreviewAsync(
        GeneratePreviewRequest request, CoreHorizonDecision horizonDecision, DateOnly asOfDate, PlanCatalogOptions catalogOptions,
        bool confirmationEnabled = false, CancellationToken ct = default);
}

/// <inheritdoc cref="ICatalogPreviewGenerator"/>
public sealed class CatalogPreviewGenerator : ICatalogPreviewGenerator
{
    /// <summary>Preview validity window, matching the existing legacy-SQL PlanPreview.ExpiresAt convention (PlanServices: DateTime.UtcNow.AddMinutes(30)).</summary>
    public static readonly TimeSpan PreviewLifetime = TimeSpan.FromMinutes(30);

    private readonly ICatalogCandidateEligibilityGate _gate;
    private readonly RuntimeConditionResolutionService _orchestration;
    private readonly ICatalogPlanSkeletonOrchestrator _skeletonOrchestrator;
    private readonly ICatalogWeekSkeletonCalendarMaterializer _calendarMaterializer;
    private readonly IDatedGeneratedCatalogPlanSkeletonValidator _datedSkeletonValidator;
    private readonly ICatalogWorkoutProgressionLoader _progressionLoader;
    private readonly IProgressionStageAllocator _stageAllocator;
    private readonly IGeneratedCatalogStageScheduleValidator _stageScheduleValidator;
    private readonly ICatalogWorkoutDefinitionLoader _workoutDefinitionLoader;
    private readonly ICatalogWorkoutBinder _workoutBinder;
    private readonly IBoundCatalogPlanValidator _boundPlanValidator;
    private readonly ICatalogPrescriptionContextBuilder _prescriptionContextBuilder;
    private readonly ICatalogPeakVolumeBandLoader _peakVolumeBandLoader;
    private readonly ICatalogVolumeAndLongRunPlanner _volumeAndLongRunPlanner;
    private readonly ICatalogSessionPrescriptionPlanner _sessionPrescriptionPlanner;
    private readonly ICatalogFinalPrescribedPlanFinalizer _finalPrescribedPlanFinalizer;
    private readonly ICatalogPublicPreviewMaterializer _publicPreviewMaterializer;
    private readonly IPublishedTemplateBundleLoader _publishedBundleLoader;

    /// <summary>
    /// Public, DI-facing constructor. Composes default <see cref="ICatalogPlanSkeletonOrchestrator"/>
    /// (Phase 4F.3), <see cref="ICatalogWeekSkeletonCalendarMaterializer"/>
    /// (Phase 4F.5), and <see cref="IDatedGeneratedCatalogPlanSkeletonValidator"/>
    /// (Phase 4F.5.1) instances from their own pure, stateless, dependency-free
    /// collaborators (no DbContext/HttpContext/clock/catalog-loader dependency
    /// among any of them) rather than taking them as constructor parameters:
    /// all three interfaces, and everything built around them, are deliberately
    /// `internal` to this assembly, and RunningApp.Api (the only DI-registration
    /// caller) has no InternalsVisibleTo grant onto RunningApp.Application — a
    /// public constructor cannot expose an internal parameter type (CS0051).
    /// Building them once here, per Scoped <see cref="CatalogPreviewGenerator"/>
    /// instance, is equivalent in effect to a Scoped DI registration without
    /// requiring one.
    ///
    /// Backend Integration Phase 4F.6A — <paramref name="progressionLoader"/> is a genuine,
    /// widened public parameter (not composed internally like the three above), because
    /// unlike them it is NOT dependency-free: <see cref="ICatalogWorkoutProgressionLoader"/>
    /// needs a real, environment-configured <c>PlanCatalog:CatalogRootPath</c> value (see
    /// <see cref="PlanCatalogOptions"/>), the same configuration <see cref="_gate"/>'s own
    /// underlying <c>IPlanCatalogBundleLoader</c> already receives via DI. Composing a
    /// "default" instance internally here would require guessing a catalog root path, which
    /// this class deliberately does not do — <see cref="IProgressionStageAllocator"/> and
    /// <see cref="IGeneratedCatalogStageScheduleValidator"/> remain dependency-free and are
    /// still internally composed, unchanged from the established pattern.
    /// </summary>
    /// <summary>
    /// Backend Integration Phase 4F.6B — <paramref name="workoutDefinitionLoader"/> is a second
    /// genuine widened public parameter for the same reason as <paramref name="progressionLoader"/>
    /// (Phase 4F.6A): <see cref="ICatalogWorkoutDefinitionLoader"/> needs the same real,
    /// environment-configured catalog root. <see cref="ICatalogWorkoutBinder"/> and
    /// <see cref="IBoundCatalogPlanValidator"/> remain dependency-free and are still internally
    /// composed.
    /// </summary>
    public CatalogPreviewGenerator(
        ICatalogCandidateEligibilityGate gate,
        RuntimeConditionResolutionService orchestration,
        ICatalogWorkoutProgressionLoader progressionLoader,
        ICatalogWorkoutDefinitionLoader workoutDefinitionLoader,
        ICatalogPeakVolumeBandLoader peakVolumeBandLoader,
        IPublishedTemplateBundleLoader publishedBundleLoader)
        : this(gate, orchestration, DefaultSkeletonOrchestrator(), DefaultCalendarMaterializer(), DefaultDatedSkeletonValidator(),
            progressionLoader, DefaultStageAllocator(), DefaultStageScheduleValidator(),
            workoutDefinitionLoader, DefaultWorkoutBinder(), DefaultBoundPlanValidator(),
            peakVolumeBandLoader, DefaultVolumeAndLongRunPlanner(), DefaultSessionPrescriptionPlanner(),
            DefaultFinalPrescribedPlanFinalizer(), DefaultPublicPreviewMaterializer(), publishedBundleLoader)
    {
    }

    /// <summary>Test-only seam (Phase 4F.4) letting RunningApp.IntegrationTests substitute a fake/spy <see cref="ICatalogPlanSkeletonOrchestrator"/> without widening this type's public constructor surface. Uses the default Phase 4F.5/4F.5.1 calendar materializer/validator and the Phase 4F.6A test-default progression loader (see <see cref="DefaultProgressionLoader"/>).</summary>
    internal CatalogPreviewGenerator(
        ICatalogCandidateEligibilityGate gate,
        RuntimeConditionResolutionService orchestration,
        ICatalogPlanSkeletonOrchestrator skeletonOrchestrator)
        : this(gate, orchestration, skeletonOrchestrator, DefaultCalendarMaterializer(), DefaultDatedSkeletonValidator())
    {
    }

    /// <summary>Test-only seam (Phase 4F.5) letting RunningApp.IntegrationTests substitute a fake/spy <see cref="ICatalogWeekSkeletonCalendarMaterializer"/> (e.g. an invocation-counting double) without widening this type's public constructor surface. Uses the default Phase 4F.5.1 dated-skeleton validator.</summary>
    internal CatalogPreviewGenerator(
        ICatalogCandidateEligibilityGate gate,
        RuntimeConditionResolutionService orchestration,
        ICatalogPlanSkeletonOrchestrator skeletonOrchestrator,
        ICatalogWeekSkeletonCalendarMaterializer calendarMaterializer)
        : this(gate, orchestration, skeletonOrchestrator, calendarMaterializer, DefaultDatedSkeletonValidator())
    {
    }

    /// <summary>Test-only seam (Phase 4F.5.1) letting RunningApp.IntegrationTests substitute a fake/spy/recording <see cref="IDatedGeneratedCatalogPlanSkeletonValidator"/> without widening this type's public constructor surface. Uses the default Phase 4F.6A workout-progression loader/stage allocator/schedule validator.</summary>
    internal CatalogPreviewGenerator(
        ICatalogCandidateEligibilityGate gate,
        RuntimeConditionResolutionService orchestration,
        ICatalogPlanSkeletonOrchestrator skeletonOrchestrator,
        ICatalogWeekSkeletonCalendarMaterializer calendarMaterializer,
        IDatedGeneratedCatalogPlanSkeletonValidator datedSkeletonValidator)
        : this(gate, orchestration, skeletonOrchestrator, calendarMaterializer, datedSkeletonValidator,
            DefaultProgressionLoader(), DefaultStageAllocator(), DefaultStageScheduleValidator())
    {
    }

    /// <summary>Test-only seam (Phase 4F.6A) letting RunningApp.IntegrationTests substitute a fake/spy <see cref="ICatalogWorkoutProgressionLoader"/>/<see cref="IProgressionStageAllocator"/>/<see cref="IGeneratedCatalogStageScheduleValidator"/> without widening this type's public constructor surface. Uses the default Phase 4F.6B workout-definition loader/binder/bound-plan validator.</summary>
    internal CatalogPreviewGenerator(
        ICatalogCandidateEligibilityGate gate,
        RuntimeConditionResolutionService orchestration,
        ICatalogPlanSkeletonOrchestrator skeletonOrchestrator,
        ICatalogWeekSkeletonCalendarMaterializer calendarMaterializer,
        IDatedGeneratedCatalogPlanSkeletonValidator datedSkeletonValidator,
        ICatalogWorkoutProgressionLoader progressionLoader,
        IProgressionStageAllocator stageAllocator,
        IGeneratedCatalogStageScheduleValidator stageScheduleValidator)
        : this(gate, orchestration, skeletonOrchestrator, calendarMaterializer, datedSkeletonValidator,
            progressionLoader, stageAllocator, stageScheduleValidator,
            DefaultWorkoutDefinitionLoader(), DefaultWorkoutBinder(), DefaultBoundPlanValidator(),
            DefaultPeakVolumeBandLoader(), DefaultVolumeAndLongRunPlanner(), DefaultSessionPrescriptionPlanner(),
            DefaultFinalPrescribedPlanFinalizer(), DefaultPublicPreviewMaterializer(), DefaultPublishedBundleLoader())
    {
    }

    /// <summary>Test-only seam (Phase 4F.6B) letting RunningApp.IntegrationTests substitute a fake/spy <see cref="ICatalogWorkoutDefinitionLoader"/>/<see cref="ICatalogWorkoutBinder"/>/<see cref="IBoundCatalogPlanValidator"/> without widening this type's public constructor surface. This is the leaf constructor every other overload ultimately delegates to.</summary>
    internal CatalogPreviewGenerator(
        ICatalogCandidateEligibilityGate gate,
        RuntimeConditionResolutionService orchestration,
        ICatalogPlanSkeletonOrchestrator skeletonOrchestrator,
        ICatalogWeekSkeletonCalendarMaterializer calendarMaterializer,
        IDatedGeneratedCatalogPlanSkeletonValidator datedSkeletonValidator,
        ICatalogWorkoutProgressionLoader progressionLoader,
        IProgressionStageAllocator stageAllocator,
        IGeneratedCatalogStageScheduleValidator stageScheduleValidator,
        ICatalogWorkoutDefinitionLoader workoutDefinitionLoader,
        ICatalogWorkoutBinder workoutBinder,
        IBoundCatalogPlanValidator boundPlanValidator,
        ICatalogPeakVolumeBandLoader peakVolumeBandLoader,
        ICatalogVolumeAndLongRunPlanner volumeAndLongRunPlanner,
        ICatalogSessionPrescriptionPlanner sessionPrescriptionPlanner,
        ICatalogFinalPrescribedPlanFinalizer finalPrescribedPlanFinalizer,
        ICatalogPublicPreviewMaterializer publicPreviewMaterializer,
        IPublishedTemplateBundleLoader publishedBundleLoader)
    {
        _gate = gate;
        _orchestration = orchestration;
        _skeletonOrchestrator = skeletonOrchestrator;
        _calendarMaterializer = calendarMaterializer;
        _datedSkeletonValidator = datedSkeletonValidator;
        _progressionLoader = progressionLoader;
        _stageAllocator = stageAllocator;
        _stageScheduleValidator = stageScheduleValidator;
        _workoutDefinitionLoader = workoutDefinitionLoader;
        _workoutBinder = workoutBinder;
        _boundPlanValidator = boundPlanValidator;
        _prescriptionContextBuilder = new CatalogPrescriptionContextBuilder();
        _peakVolumeBandLoader = peakVolumeBandLoader;
        _volumeAndLongRunPlanner = volumeAndLongRunPlanner;
        _sessionPrescriptionPlanner = sessionPrescriptionPlanner;
        _finalPrescribedPlanFinalizer = finalPrescribedPlanFinalizer;
        _publicPreviewMaterializer = publicPreviewMaterializer;
        _publishedBundleLoader = publishedBundleLoader;
    }

    private static IPublishedTemplateBundleLoader DefaultPublishedBundleLoader() => new NullPublishedTemplateBundleLoader();

    /// <summary>Test-only default (reached only via internal constructor overloads, never via the public DI-facing constructor): always resolves to Legacy (no published bundle), matching every internal test seam's pre-Split-E behavior unchanged.</summary>
    private sealed class NullPublishedTemplateBundleLoader : IPublishedTemplateBundleLoader
    {
        public Task<PlanCatalog.Contracts.Bundles.PublishedTemplateBundle?> TryLoadAsync(string combinationKey, int combinationVersion, CancellationToken ct = default)
            => Task.FromResult<PlanCatalog.Contracts.Bundles.PublishedTemplateBundle?>(null);
    }

    private static ICatalogPlanSkeletonOrchestrator DefaultSkeletonOrchestrator() => new CatalogPlanSkeletonOrchestrator(
        new CatalogPhaseAllocationResolver(),
        new CatalogRunLayoutResolver(),
        new CatalogStageToWeekContextFactory(),
        new CatalogStageToWeekMaterializer(),
        new GeneratedCatalogPlanSkeletonValidator());

    private static ICatalogWeekSkeletonCalendarMaterializer DefaultCalendarMaterializer() => new CatalogWeekSkeletonCalendarMaterializer();

    private static IDatedGeneratedCatalogPlanSkeletonValidator DefaultDatedSkeletonValidator() => new DatedGeneratedCatalogPlanSkeletonValidator();

    /// <summary>
    /// Test-only default (reached only via the internal constructor overloads above, never via
    /// the public DI-facing constructor, which always receives a properly DI-configured
    /// <see cref="ICatalogWorkoutProgressionLoader"/> instead). Unlike
    /// <see cref="DefaultSkeletonOrchestrator"/>/<see cref="DefaultCalendarMaterializer"/>/<see cref="DefaultDatedSkeletonValidator"/>,
    /// this cannot be a truly dependency-free default: <see cref="CatalogWorkoutProgressionLoader"/>
    /// needs a real catalog-root path. Resolves it the same way this repository's own test
    /// helpers already do (see <c>TestPlanServicesFactory.RepoRoot()</c>) — walk up from
    /// <see cref="AppContext.BaseDirectory"/> until a directory containing both 'backend' and
    /// 'plan-catalog' is found. Safe here specifically because every internal constructor that
    /// reaches this method is itself only ever called from RunningApp.IntegrationTests (see
    /// each overload's own doc comment) — production always uses the public 3-arg constructor.
    /// </summary>
    private static ICatalogWorkoutProgressionLoader DefaultProgressionLoader()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !(Directory.Exists(Path.Combine(dir.FullName, "backend")) && Directory.Exists(Path.Combine(dir.FullName, "plan-catalog"))))
        {
            dir = dir.Parent;
        }

        var repoRoot = dir?.FullName ?? throw new InvalidOperationException(
            "Could not locate repo root (expected a directory containing both 'backend' and 'plan-catalog') to build a test-default ICatalogWorkoutProgressionLoader.");

        var catalogRoot = Path.Combine(repoRoot, "plan-catalog", "catalog");
        return new CatalogWorkoutProgressionLoader(Microsoft.Extensions.Options.Options.Create(new PlanCatalogOptions { CatalogRootPath = catalogRoot }));
    }

    private static IProgressionStageAllocator DefaultStageAllocator() => new ProgressionStageAllocator();

    private static IGeneratedCatalogStageScheduleValidator DefaultStageScheduleValidator() => new GeneratedCatalogStageScheduleValidator();

    /// <summary>Test-only default (Phase 4F.6B) — same repo-root-walk rationale as <see cref="DefaultProgressionLoader"/>; only ever reached from internal test-seam constructors, never from the public constructor, which always receives a properly DI-configured <see cref="ICatalogWorkoutDefinitionLoader"/> instead.</summary>
    private static ICatalogWorkoutDefinitionLoader DefaultWorkoutDefinitionLoader() =>
        new CatalogWorkoutDefinitionLoader(Microsoft.Extensions.Options.Options.Create(new PlanCatalogOptions { CatalogRootPath = TestOnlyRepoRelativeCatalogRoot() }));

    private static ICatalogWorkoutBinder DefaultWorkoutBinder() => new CatalogWorkoutBinder();

    private static IBoundCatalogPlanValidator DefaultBoundPlanValidator() => new BoundCatalogPlanValidator();

    private static ICatalogPeakVolumeBandLoader DefaultPeakVolumeBandLoader() =>
        new CatalogPeakVolumeBandLoader(Microsoft.Extensions.Options.Options.Create(new PlanCatalogOptions { CatalogRootPath = TestOnlyRepoRelativeCatalogRoot() }));

    private static ICatalogVolumeAndLongRunPlanner DefaultVolumeAndLongRunPlanner() => new CatalogVolumeAndLongRunPlanner();

    private static ICatalogSessionPrescriptionPlanner DefaultSessionPrescriptionPlanner() => new CatalogSessionPrescriptionPlanner();

    private static ICatalogFinalPrescribedPlanFinalizer DefaultFinalPrescribedPlanFinalizer() => new CatalogFinalPrescribedPlanFinalizer();

    private static ICatalogPublicPreviewMaterializer DefaultPublicPreviewMaterializer() => new CatalogPublicPreviewMaterializer();

    private static string TestOnlyRepoRelativeCatalogRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !(Directory.Exists(Path.Combine(dir.FullName, "backend")) && Directory.Exists(Path.Combine(dir.FullName, "plan-catalog"))))
        {
            dir = dir.Parent;
        }

        var repoRoot = dir?.FullName ?? throw new InvalidOperationException(
            "Could not locate repo root (expected a directory containing both 'backend' and 'plan-catalog') to build a test-default catalog loader.");

        return Path.Combine(repoRoot, "plan-catalog", "catalog");
    }

    public async Task<CatalogPreviewSnapshot> GenerateAsync(GeneratePreviewRequest request, DateOnly asOfDate, CancellationToken ct = default)
    {
        var identity = V1CatalogPilotIdentityPolicy.ResolveCandidate(request.Level, request.DaysPerWeek);
        var candidate = await _gate.LoadForPublicPreviewAsync(identity.CandidateKey, identity.CandidateVersion, ct);

        var input = BuildInputSnapshot(request, asOfDate, candidate);
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

        // ── Backend Integration Phase 4F.4/4F.5: dark internal skeleton + ──
        // ── calendar-day materialization ─────────────────────────────────
        // Runs only after candidate eligibility (line above the resolver call)
        // and runtime-condition resolution (incl. governance policy) have both
        // succeeded — every input below is already authoritative and frozen;
        // nothing is reloaded, reselected, or rerun. The orchestrator/materializer
        // contexts and results are local to this method only: never attached
        // to the snapshot, never hashed, never returned. This call's only
        // observable effect is: (a) nothing, on success: preview construction
        // continues unchanged; (b) an aborted, typed preview-generation
        // failure if the candidate's live catalog data, or the request's own
        // PreferredDays/LongRunDay, cannot produce a valid dated skeleton.
        var generatedPreviewPlanPayload = BuildDarkInternalDatedSkeleton(candidate, asOfDate, request, input, results);

        var trace = BuildDecisionTrace(results);
        var createdAtUtc = DateTime.UtcNow;

        return CatalogPreviewSnapshotBuilder.Build(
            input, asOfDate, candidate, $"PILOT_TEN_K_INTERMEDIATE_{request.DaysPerWeek}D_MATCH",
            results, trace, createdAtUtc, createdAtUtc.Add(PreviewLifetime), generatedPreviewPlanPayload);
    }

    /// <summary>
    /// Feature-gate identifier — a narrow, candidate-scoped routing policy
    /// name (not a runtime toggle infrastructure; this repository has none),
    /// used only in logs/trace. Disabling the 15-20 week route requires
    /// removing/bypassing the single call site in <c>PlanServices</c> that
    /// checks <see cref="Schedule.Horizon.CoreHorizonMode.PreparationRunwayPlusCore"/>
    /// scope eligibility — no other component needs to change.
    /// </summary>
    public const string PreparationRunwayPreviewGateName = "TEN_K_4D_INTERMEDIATE_PREPARATION_RUNWAY_PREVIEW";

    public async Task<(CatalogPreviewSnapshot Snapshot, IReadOnlyList<PreviewWeekDto> Weeks)> GeneratePreparationRunwayPreviewAsync(
        GeneratePreviewRequest request, CoreHorizonDecision horizonDecision, DateOnly asOfDate, PlanCatalogOptions catalogOptions,
        bool confirmationEnabled = false, CancellationToken ct = default)
    {
        var (runwayCandidateKey, runwayCandidateVersion) = V1CatalogPilotIdentityPolicy.ResolveCandidate(request.Level, request.DaysPerWeek);
        var candidate = await _gate.LoadForPublicPreviewAsync(runwayCandidateKey, runwayCandidateVersion, ct);

        var input = BuildInputSnapshot(request, asOfDate, candidate);
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
            throw new PlanPreviewGenerationFailedException(
                $"Runtime condition resolution failed due to a configuration error: {ex.Message}", ex);
        }
        catch (ArgumentException ex)
        {
            throw new PlanPreviewGenerationFailedException(
                $"Runtime condition resolution failed due to invalid input reaching a resolver: {ex.Message}", ex);
        }

        ApplyNotEvaluatedGovernancePolicy(results);

        if (request.RaceDate is not { } raceDate)
        {
            throw new PreparationRunwayPreviewNotEnabledException(
                "Preparation Runway preview requires a RaceDate.");
        }

        // Phase 4G.6B.1: no RaceHorizonPolicy.Decide call here -- horizonDecision
        // is the single authoritative decision PlanServices already computed
        // once for this request. Only value-consistency validation happens
        // here, never a second, independent classification.
        var horizon = horizonDecision;

        if (candidate.CoreCycle.MaximumWeeks is null)
        {
            throw new PlanPreviewGenerationFailedException(
                "Catalog core maximum is required for Preparation Runway composition.");
        }

        if (horizon.MinimumCoreWeeks != candidate.CoreCycle.MinimumWeeks ||
            horizon.PreferredCoreWeeks != candidate.CoreCycle.DefaultWeeks ||
            horizon.MaximumCoreWeeks != candidate.CoreCycle.MaximumWeeks)
        {
            // The carried decision was computed against a different core-cycle
            // bounds set than this resolved candidate declares -- reject
            // rather than silently trusting a possibly-stale decision.
            throw new PreparationRunwayPreviewNotEnabledException(
                "Carried horizon decision's core-cycle bounds do not match the resolved candidate's own CoreCycle.");
        }

        if (horizon.Mode != Schedule.Horizon.CoreHorizonMode.PreparationRunwayPlusCore || horizon.AvailableFullWeeks is < 15 or > 20)
        {
            // Defensive: PlanServices' own routing gate is the authoritative
            // scope check and must never let a request reach here otherwise.
            throw new PreparationRunwayPreviewNotEnabledException(
                $"Preparation Runway preview requires a 15-20 week direct-runway horizon; resolved horizon was " +
                $"{horizon.Mode} at {horizon.AvailableFullWeeks} available weeks.");
        }

        var coreEntryReadinessResult = results.Single(r => r.ConditionType == CoreEntryReadinessResolver.ConditionTypeValue);
        var preferredDays = CatalogPreferredDayAdapter.ParsePreferredDays(WeekdayCsv.ToCsv(request.PreferredDays));
        var longRunDay = CatalogPreferredDayAdapter.ParseLongRunDay(WeekdayCsv.ToCsv(request.LongRunDay));

        var orchestrationRequest = new Schedule.PreparationRunwayOrchestration.TenKPreparationRunwayDarkOrchestrationRequest(
            candidate, request.StartDate, raceDate, asOfDate, preferredDays, longRunDay,
            coreEntryReadinessResult, results, request, input,
            Schedule.PreparationRunwayNumericMaterialization.PreparationRunwayQuantityUnit.Kilometers,
            HorizonDecision: horizon);

        var orchestrator = Schedule.PreparationRunwayOrchestration.TenKPreparationRunwayDarkOrchestratorFactory.Create(catalogOptions);
        var orchestrationResult = await orchestrator.OrchestrateAsync(orchestrationRequest, ct);

        if (!orchestrationResult.IsSuccess)
        {
            var failure = orchestrationResult.Failure!;
            // Internal stage/failure detail is preserved only in the trace
            // (never returned to a public caller) -- see
            // TenKPreparationRunwayDarkOrchestrationResult.Trace for the full
            // deterministic step-by-step record up to this failure.
            throw new PreparationRunwayPreviewGenerationFailedException(
                $"Preparation Runway preview generation failed at stage {failure.Stage} ({failure.Code}).");
        }

        var weeks = PreparationRunwayPublicPreviewMapper.MapCombinedWeeks(orchestrationResult);

        // Phase 4G.6C: when the scoped confirmation gate is enabled, build the
        // real persistable payload from the SAME completed orchestration
        // result the public weeks were just mapped from -- never a second,
        // independently-regenerated schedule. When disabled, behavior is
        // byte-for-byte identical to Phase 4G.6B/4G.6B.1/4G.6B.2 (payload
        // stays null, preview stays non-persistable).
        var persistablePayload = confirmationEnabled
            ? PreparationRunwayPersistablePlanMapper.Map(orchestrationResult, candidate, request.StartDate, asOfDate)
            : null;

        var trace = BuildDecisionTrace(results);
        var createdAtUtc = DateTime.UtcNow;
        var snapshot = CatalogPreviewSnapshotBuilder.Build(
            input, asOfDate, candidate, $"PILOT_TEN_K_INTERMEDIATE_{request.DaysPerWeek}D_PREPARATION_RUNWAY_MATCH",
            results, trace, createdAtUtc, createdAtUtc.Add(PreviewLifetime), generatedPreviewPlanPayload: persistablePayload);

        return (snapshot, weeks);
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
    /// Backend Integration Phase 4F.4/4F.5/4F.5.1 — builds the Phase 4F.3
    /// orchestration context exclusively from <paramref name="candidate"/>
    /// (already loaded and PUBLISHED-gated by <see cref="_gate"/>) and
    /// <paramref name="asOfDate"/> (already fixed by the caller), invokes
    /// <see cref="_skeletonOrchestrator"/>, and then — using the already-
    /// in-scope <paramref name="request"/>'s own <c>PreferredDays</c>/
    /// <c>LongRunDay</c> fields (never reloaded from the database; these are
    /// the same fields already available on the request object this method's
    /// caller received) — invokes <see cref="_calendarMaterializer"/> to
    /// produce a dated skeleton, then <see cref="_datedSkeletonValidator"/>
    /// to validate it (Phase 4F.5.1: an independent-audit-identified gap fix
    /// — this validation step previously existed only as test-exercised code,
    /// never invoked from this path). StartDate is the request's explicit
    /// plan start and remains distinct from AsOfDate. The exact-12 preferred
    /// path uses this fixed pipeline; compressed/extended modes use the
    /// dynamic orchestration branch below. This call's contract is
    /// contract is "throw a typed failure if the candidate's live catalog
    /// data, the request's own scheduling preferences, or the resulting dated
    /// skeleton fail validation," never "return usable content." <paramref name="request"/>'s
    /// GoalType is always <c>Race</c> here in production — <see cref="PilotGenerationRouteDecider"/>
    /// only ever routes Race-goal requests to this class — so the race-plan
    /// hard-constraint policy (Phase 4F.5) is always the one in effect;
    /// this method does not special-case or accept a Habit goal.
    /// Every typed Phase 4F.3/4F.5 exception is mapped onto the pre-existing
    /// <see cref="PlanPreviewGenerationFailedException"/> (never a new public
    /// error code — the existing preview-generation failure taxonomy already
    /// preserves the distinction) with the original exception preserved as
    /// <c>InnerException</c> and no internal phase/layout/scheduling detail
    /// included in any message a client could ever see (see
    /// GlobalExceptionHandler: 500-status messages are never echoed to the
    /// client).
    /// </summary>
    private GeneratedCatalogPlanPayload BuildDarkInternalDatedSkeleton(
        PlanCatalogCandidateSummary candidate, DateOnly asOfDate, GeneratePreviewRequest request, ResolverInputSnapshot input,
        IReadOnlyList<RuntimeConditionResolutionResult> conditionResults)
    {
        // Phase 10K-FREQ.6D.4D.5G — the same published-bundle execution index every Core horizon
        // strategy below must consume, loaded exactly once per request (never per-week, never
        // per-session, never re-derived independently by CompressedCore/ExtendedCore) against the
        // exact candidate identity — no "latest", no horizon-specific bundle, no 4D fallback. This
        // was previously computed only inline in the exact-12-week "preferred" pipeline further
        // down; the CompressedCore/ExtendedCore branch never received it at all (FREQ.6D.4D.5F's
        // disclosed blocker) — now both consume this single instance.
        var executionIndex = LoadExecutionIndex(candidate);

        if (request.RaceDate is { } activatedRaceDate)
        {
            var horizon = RaceHorizonPolicy.Decide(
                request.StartDate, activatedRaceDate,
                candidate.CoreCycle.MinimumWeeks, candidate.CoreCycle.DefaultWeeks,
                candidate.CoreCycle.MaximumWeeks ?? throw new PlanPreviewGenerationFailedException("Catalog core maximum is required for dynamic standalone-core generation."));
            if (horizon.Mode is CoreHorizonMode.CompressedCore or CoreHorizonMode.ExtendedCore)
            {
                var preferredDays = CatalogPreferredDayAdapter.ParsePreferredDays(WeekdayCsv.ToCsv(request.PreferredDays));
                var longRunDay = CatalogPreferredDayAdapter.ParseLongRunDay(WeekdayCsv.ToCsv(request.LongRunDay));
                var dynamicOrchestrator = new DynamicCoreCalendarMaterializationOrchestrator(
                    new DynamicCoreSessionPrescriptionOrchestrator(
                        new DynamicCoreVolumeAndLongRunOrchestrator(
                            new DynamicCoreWorkoutBindingOrchestrator(
                                new DynamicCoreWeekSkeletonOrchestrator(
                                    new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
                                    new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
                                _progressionLoader, _stageAllocator, _stageScheduleValidator,
                                _calendarMaterializer, _datedSkeletonValidator, _workoutBinder, _boundPlanValidator),
                            _prescriptionContextBuilder, _volumeAndLongRunPlanner),
                        _sessionPrescriptionPlanner, _finalPrescribedPlanFinalizer));
                DynamicCoreCalendarMaterializationResult dynamicResult;
                try
                {
                    dynamicResult = dynamicOrchestrator.MaterializeAsync(new DynamicCoreCalendarMaterializationContext
                    {
                        Candidate = candidate, TargetWeekCount = horizon.AvailableFullWeeks,
                        StartDate = request.StartDate, RaceDate = activatedRaceDate, AsOfDate = asOfDate,
                        PreferredDays = preferredDays, LongRunDayPreference = longRunDay,
                        ConditionResults = conditionResults, PreviewRequest = request, ResolverInput = input,
                        WorkoutDefinitionLoader = _workoutDefinitionLoader, PeakVolumeBandLoader = _peakVolumeBandLoader,
                        ExecutionIndex = executionIndex,
                    }).GetAwaiter().GetResult();
                }
                catch (DynamicCoreVolumeAndLongRunFailedException ex) when (ex.InnerException is CatalogProductIneligibleException ineligible)
                {
                    throw new PlanProductIneligibleException(ineligible.Code, ineligible.Message, ineligible);
                }
                var volumeResult = dynamicResult.PrescriptionResult.VolumeResult;
                return _publicPreviewMaterializer.Materialize(new CatalogPublicPreviewMaterializationRequest(
                    request, candidate, request.StartDate, asOfDate, volumeResult.VolumeAndLongRunPlan,
                    dynamicResult.PrescriptionResult.FinalPrescribedPlan)).Payload;
            }
        }

        var skeletonContext = new CatalogPlanSkeletonOrchestrationContext
        {
            Candidate = candidate,
            ExpectedCandidateKey = candidate.CandidateKey,
            ExpectedCandidateVersion = candidate.CandidateVersion,
            ExpectedMasterTemplate = candidate.MasterTemplate,
            ExpectedRunLayout = candidate.Layout,
            StartDate = request.StartDate,
            AsOfDate = asOfDate,
        };

        GeneratedCatalogPlanSkeleton skeleton;
        try
        {
            skeleton = _skeletonOrchestrator.Build(skeletonContext).Skeleton;
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

        // ── Backend Integration Phase 4F.6A: dark internal fine-grained ──
        // ── KEY_SESSION stage scheduling ──────────────────────────────────
        // Runs after the phase/week skeleton (above) and before calendar-day
        // assignment (below) — the exact placement Section 16 of the phase's
        // own specification calls for. Loads the workout-progression document
        // body (never done by any earlier phase — PlanCatalogCandidateSummary
        // only ever carried a bare (key, version) reference to it), then
        // allocates each phase's declared stages onto that phase's already-
        // resolved weeks. Binds no workout identity (D-C06/D-C09; Phase
        // 4F.6B's responsibility) and is discarded immediately after
        // validation — never stored, hashed, or returned. See
        // PHASE4F_6A_FINE_GRAINED_KEY_SESSION_STAGE_SCHEDULER.md.
        GeneratedCatalogStageSchedule stageSchedule;
        CatalogWorkoutProgressionDefinition progression;
        try
        {
            progression = _progressionLoader.LoadAsync(candidate.WorkoutProgression).GetAwaiter().GetResult();

            var allocationContext = new ProgressionStageAllocationContext
            {
                CandidateKey = candidate.CandidateKey,
                CandidateVersion = candidate.CandidateVersion,
                Progression = progression,
                Skeleton = skeleton,
                ConditionResults = conditionResults,
            };

            stageSchedule = _stageAllocator.Allocate(allocationContext);

            var stageScheduleValidation = _stageScheduleValidator.Validate(stageSchedule, skeleton);
            if (!stageScheduleValidation.IsValid)
            {
                throw new ProgressionStageScheduleInvalidException(
                    $"Stage schedule for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' failed output " +
                    "validation: " + string.Join(", ", stageScheduleValidation.Errors) + ".");
            }

            _ = stageSchedule; // Dark result: validated, then discarded -- never stored, hashed, or returned.
        }
        catch (Exception ex) when (ex is PlanCatalogLoadException
            or ProgressionStageSchedulingException
            or ProgressionStageScheduleInvalidException)
        {
            throw new PlanPreviewGenerationFailedException(
                $"CATALOG_INTERNAL_STAGE_SCHEDULING_FAILED: internal fine-grained stage scheduling failed " +
                $"for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}': {ex.Message}", ex);
        }

        DatedGeneratedCatalogPlanSkeleton datedSkeleton;
        try
        {
            var preferredDays = CatalogPreferredDayAdapter.ParsePreferredDays(WeekdayCsv.ToCsv(request.PreferredDays));
            var longRunDay = CatalogPreferredDayAdapter.ParseLongRunDay(WeekdayCsv.ToCsv(request.LongRunDay));

            var calendarProvenance = new CatalogCalendarMaterializationProvenance(
                candidate.CandidateKey, candidate.CandidateVersion, asOfDate, request.StartDate,
                preferredDays, longRunDay, CatalogCalendarDayMaterializerVersion.V1,
                skeleton.SchemaVersion, skeleton.DependencyVersions);

            var calendarContext = new CatalogCalendarAssignmentContext(
                request.StartDate, request.GoalType, preferredDays, longRunDay, skeleton,
                CatalogCalendarAssignmentPolicy.RaceHardConstraint, calendarProvenance);

            // Backend Integration Phase 4F.5.1: materialize, then validate --
            // an independent-audit-identified gap fix. The Phase 4F.3
            // orchestrator's own "materialize, then validate before
            // returning" step (see CatalogPlanSkeletonOrchestrator.Build)
            // is the established repository precedent this mirrors.
            // Orchestration-level validation (here, not inside the
            // materializer) matches that precedent: the materializer stays a
            // pure transform, and this method -- already the place that owns
            // the Phase 4F.3 skeleton-then-validate sequence -- owns the
            // Phase 4F.5 dated-skeleton-then-validate sequence too.
            datedSkeleton = _calendarMaterializer.Materialize(calendarContext);

            var validation = _datedSkeletonValidator.Validate(datedSkeleton, preferredDays, longRunDay);
            if (!validation.IsValid)
            {
                throw new CatalogDatedSkeletonInvalidException(
                    $"Dated skeleton for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' " +
                    "failed output validation: " + string.Join(", ", validation.Errors) + ".");
            }

            // ── Defensive race-date alignment invariant (fail-closed backstop) ──
            // Phase 4G.3B.4a: this guard validates date-tolerance alignment
            // ONLY. Whether a given standalone week count is publicly
            // supported at all is owned exclusively by RaceHorizonPolicy /
            // the preview-routing layer (PlanServices.GeneratePreviewAsync's
            // upstream horizon guard, mirrored independently by
            // LivePlanPreviewRouting's route decision) — this method never
            // duplicates that decision. Production call-path audit (Phase
            // 4G.3B.4a) confirmed the only week count that can ever reach
            // this method in production is exactly
            // RaceHorizonPolicy.ExactStandaloneCoreSupportedWeeks (12): every
            // other classification (BelowMinimum, CoreLengthRecognizedButNotImplemented,
            // CompositionRequired) is rejected upstream, before catalog
            // generation, by either the PlanServices guard or the route
            // decider's independent cycle-length check — so removing the
            // week-count comparison here does not change any public result.
            // What remains is a second, independent check on the actual
            // materialized output's date alignment, so a future regression
            // anywhere upstream (e.g. a horizon-aware allocator change that
            // doesn't correctly honor the RaceDate convention) cannot
            // silently reintroduce a misaligned plan. Deliberately NOT in the
            // catch filter below — it must propagate as itself, not be
            // rewrapped into a 500. See
            // PHASE4G_3B_4A_HORIZON_GATE_ALIGNMENT_GUARD_SEPARATION.md and
            // TD-RACEDATE-CHECK-NOT-HORIZON-AGNOSTIC-001.
            if (request.RaceDate is { } raceDateForAlignment)
            {
                var daysBeforeRace = raceDateForAlignment.DayNumber - datedSkeleton.EndDate.DayNumber;
                const int maxAllowedTrailingGapDays = 7;
                if (daysBeforeRace < 0 || daysBeforeRace > maxAllowedTrailingGapDays)
                {
                    throw new CatalogRaceDateAlignmentInvalidException(
                        $"Generated schedule for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' has " +
                        $"{datedSkeleton.Weeks.Count} week(s) ending {datedSkeleton.EndDate:yyyy-MM-dd}, which is not aligned to " +
                        $"RaceDate {raceDateForAlignment:yyyy-MM-dd} (must end within {maxAllowedTrailingGapDays} days before the " +
                        "race, never after it).");
                }
            }
        }
        catch (Exception ex) when (ex is CatalogPreferredDaysRequiredException
            or CatalogPreferredDayCountInvalidException
            or CatalogPreferredDaysDuplicatedException
            or CatalogLongRunDayRequiredException
            or CatalogLongRunDayNotPreferredException
            or CatalogCalendarRoleStructureInvalidException
            or CatalogPreferredDayConfigurationUnsafeException
            or CatalogDatedSkeletonInvalidException)
        {
            throw new PlanPreviewGenerationFailedException(
                $"CATALOG_INTERNAL_SKELETON_MATERIALIZATION_FAILED: internal calendar-day assignment failed " +
                $"for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}': {ex.Message}", ex);
        }

        // ── Backend Integration Phase 4F.6B: dark internal exact ─────────
        // ── workout-definition binding ────────────────────────────────────
        // Runs after the dated structural schedule (above) is validated —
        // the exact placement Section 14 of the phase's own specification
        // calls for (fine-grained stage schedule and dated schedule are both
        // already available at this point; binding needs both). Reuses the
        // already-loaded workout-progression definition and already-computed
        // stage schedule from the Phase 4F.6A step above -- never reloads or
        // recomputes either. Binds EASY_SUPPORT/LONG_RUN fixed defaults and
        // the KEY_SESSION stage-controlled candidate per
        // V1CatalogWorkoutRoleBindingPolicy; assigns workout IDENTITY only
        // (no pace/distance/duration/volume/prescription). Discarded
        // immediately after validation -- never stored, hashed, or returned.
        // See PHASE4F_6B_V1_EXACT_WORKOUT_BINDING.md.
        try
        {
            var bindingContext = new CatalogWorkoutBindingContext
            {
                CandidateKey = candidate.CandidateKey,
                CandidateVersion = candidate.CandidateVersion,
                DatedSkeleton = datedSkeleton,
                StageSchedule = stageSchedule,
                Progression = progression,
                ReferencedWorkouts = candidate.ReferencedWorkouts,
                WorkoutDefinitionLoader = _workoutDefinitionLoader,
            };

            var boundPlan = _workoutBinder.BindAsync(bindingContext).GetAwaiter().GetResult();

            var boundPlanValidation = _boundPlanValidator.Validate(boundPlan, datedSkeleton);
            if (!boundPlanValidation.IsValid)
            {
                throw new CatalogWorkoutBindingPlanInvalidException(
                    $"Bound workout plan for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' failed output " +
                    "validation: " + string.Join(", ", boundPlanValidation.Errors) + ".");
            }

            var definitionMap = new Dictionary<string, CatalogWorkoutDefinitionSummary>(StringComparer.Ordinal);
            foreach (var workout in candidate.ReferencedWorkouts)
            {
                definitionMap[workout.Key] = _workoutDefinitionLoader.LoadAsync(workout).GetAwaiter().GetResult();
            }

            var prescriptionContext = _prescriptionContextBuilder.Build(new CatalogPrescriptionContextBuildRequest(
                request, asOfDate, candidate, input, conditionResults, boundPlan, definitionMap));

            if (!prescriptionContext.ValidationResult.IsValid)
            {
                throw new CatalogWorkoutBindingPlanInvalidException(
                    "Prescription context validation failed: " + string.Join(", ", prescriptionContext.ValidationResult.Errors) + ".");
            }

            var peakVolumeBand = _peakVolumeBandLoader.LoadAsync(
                candidate.PeakVolumeBandPolicy,
                candidate.CanonicalDistanceFamily,
                candidate.Level,
                candidate.DaysPerWeek).GetAwaiter().GetResult();

            var volumePlan = _volumeAndLongRunPlanner.Build(new CatalogVolumePlanningRequest(
                candidate, boundPlan, prescriptionContext, peakVolumeBand));

            if (!volumePlan.WeeklyVolumePlan.ValidationResult.IsValid || !volumePlan.LongRunProgression.ValidationResult.IsValid)
            {
                throw new CatalogVolumePlanInvalidException(
                    "Volume/long-run validation failed: " +
                    string.Join(", ", volumePlan.WeeklyVolumePlan.ValidationResult.Errors.Concat(volumePlan.LongRunProgression.ValidationResult.Errors)) + ".");
            }

            var prescribedPlan = _sessionPrescriptionPlanner.Build(new CatalogSessionPrescriptionRequest(
                candidate, boundPlan, prescriptionContext, volumePlan, definitionMap, executionIndex));

            if (!prescribedPlan.ValidationResult.IsValid)
            {
                throw new CatalogSessionPrescriptionInvalidException(
                    "Session prescription validation failed: " + string.Join(", ", prescribedPlan.ValidationResult.Errors) + ".");
            }

            var finalPrescribedPlan = _finalPrescribedPlanFinalizer.Complete(new CatalogFinalPrescriptionRequest(
                candidate, boundPlan, volumePlan, prescribedPlan));

            if (!finalPrescribedPlan.ValidationResult.IsValid)
            {
                throw new CatalogFinalPrescribedPlanInvalidException(
                    "Final prescribed-plan validation failed: " + string.Join(", ", finalPrescribedPlan.ValidationResult.Errors) + ".");
            }

            var publicPayload = _publicPreviewMaterializer.Materialize(new CatalogPublicPreviewMaterializationRequest(
                request, candidate, datedSkeleton.StartDate, asOfDate, volumePlan, finalPrescribedPlan));

            _ = prescriptionContext;
            _ = prescribedPlan;
            return publicPayload.Payload;
        }
        catch (DynamicCoreVolumeAndLongRunFailedException ex) when (ex.InnerException is CatalogProductIneligibleException)
        {
            var ineligible = (CatalogProductIneligibleException)ex.InnerException!;
            throw new PlanProductIneligibleException(ineligible.Code, ineligible.Message, ineligible);
        }
        catch (CatalogProductIneligibleException ex)
        {
            throw new PlanProductIneligibleException(ex.Code, ex.Message, ex);
        }
        catch (Exception ex) when (ex is PlanCatalogLoadException or CatalogWorkoutBindingException or CatalogPrescriptionContractException or CatalogVolumePlanningException or CatalogSessionPrescriptionException or CatalogPublicMaterializationException)
        {
            throw new PlanPreviewGenerationFailedException(
                $"CATALOG_INTERNAL_WORKOUT_BINDING_FAILED: internal exact workout-definition binding failed " +
                $"for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Phase 10K-FREQ.6D.4D.5G — the single, canonical, once-per-request execution-context load,
    /// shared by every Core horizon strategy (<see cref="BuildDarkInternalDatedSkeleton"/>'s own
    /// exact-12-week body and its CompressedCore/ExtendedCore branch alike). Exact candidate
    /// identity only — never "latest," never horizon-specific, never a 4D fallback.
    /// </summary>
    private ExecutionPrescriptionIndex? LoadExecutionIndex(PlanCatalogCandidateSummary candidate)
    {
        var publishedBundle = _publishedBundleLoader.TryLoadAsync(candidate.CandidateKey, candidate.CandidateVersion).GetAwaiter().GetResult();
        return publishedBundle is not null ? ExecutionPrescriptionIndex.Build(publishedBundle) : null;
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
    /// Maps a GeneratePreviewRequest onto a ResolverInputSnapshot. StartDate
    /// is the request's own <see cref="GeneratePreviewRequest.StartDate"/>
    /// (Backend Integration contract alignment — no longer the AsOfDate
    /// simplification this comment previously documented; the request now
    /// carries a real, required plan-start-date field).
    ///
    /// GoalDistanceKm is derived from the loaded catalog candidate's distance
    /// family and checked against the request's typed goal distance. The V1
    /// pilot still resolves to 10 km for TEN_K, but no unexplained literal is
    /// constructed in this resolver input path.
    ///
    /// RecentRace fields come from <see cref="GeneratePreviewRequest.RecentRace"/>
    /// verbatim when present (distance converted to km via the shared
    /// <see cref="GoalDistanceKm"/> table — distinct from the target-race
    /// distance resolution above, which uses the catalog candidate's family)
    /// and are all null when RecentRace is null. Never touches Level,
    /// RaceDate, or any target-race field.
    /// </summary>
    private static ResolverInputSnapshot BuildInputSnapshot(GeneratePreviewRequest request, DateOnly asOfDate, PlanCatalogCandidateSummary candidate) => new()
    {
        CanonicalDistanceFamily = candidate.CanonicalDistanceFamily,
        RequestedTargetDistanceKm = CatalogGoalDistanceResolver.Resolve(candidate.CanonicalDistanceFamily, request.GoalDistance),
        GoalType = request.GoalType,
        GoalDistance = request.GoalDistance,
        GoalDistanceKm = CatalogGoalDistanceResolver.Resolve(candidate.CanonicalDistanceFamily, request.GoalDistance),
        TargetFinishTimeSeconds = request.TargetFinishTimeSeconds,
        TargetFinishTimeSource = request.TargetFinishTimeSource,
        DaysPerWeek = request.DaysPerWeek,
        Level = request.Level,
        StartDate = request.StartDate,
        RaceDate = request.RaceDate,
        RaceName = request.RaceName,
        PreferredDays = request.PreferredDays,
        LongRunDay = request.LongRunDay,
        WeeklyAvailability = request.WeeklyAvailability,
        PreferredPace = request.PreferredPace,
        RecentLongestRunKm = request.RecentLongestRunKm,
        RecentWeeklyVolumeKm = request.RecentWeeklyVolumeKm,
        RecentRunsPerWeek = request.RecentRunsPerWeek,
        RecentRaceDistanceKm = request.RecentRace is null ? null : GoalDistanceKm.Resolve(request.RecentRace.Distance),
        RecentRaceFinishTimeSeconds = request.RecentRace?.FinishTimeSeconds,
        RecentRaceDate = request.RecentRace?.RaceDate,
    };
}
