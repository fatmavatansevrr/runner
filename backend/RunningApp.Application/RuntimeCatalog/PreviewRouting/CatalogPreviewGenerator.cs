using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;

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
/// TrainingPlan/TrainingWeek/TrainingDay schedule is implemented
/// (<see cref="CatalogPlanConfirmationService"/>, Phase 4F.9), but has not
/// yet been relationally verified against a real PostgreSQL database. See
/// PHASE4F_4_DARK_INTERNAL_SKELETON_WIRING_INTO_CATALOG_PREVIEW.md for the
/// original (now superseded) dark-wiring history.
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
        ICatalogPeakVolumeBandLoader peakVolumeBandLoader)
        : this(gate, orchestration, DefaultSkeletonOrchestrator(), DefaultCalendarMaterializer(), DefaultDatedSkeletonValidator(),
            progressionLoader, DefaultStageAllocator(), DefaultStageScheduleValidator(),
            workoutDefinitionLoader, DefaultWorkoutBinder(), DefaultBoundPlanValidator(),
            peakVolumeBandLoader, DefaultVolumeAndLongRunPlanner(), DefaultSessionPrescriptionPlanner(),
            DefaultFinalPrescribedPlanFinalizer(), DefaultPublicPreviewMaterializer())
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
            DefaultFinalPrescribedPlanFinalizer(), DefaultPublicPreviewMaterializer())
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
        ICatalogPublicPreviewMaterializer publicPreviewMaterializer)
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
        var candidate = await _gate.LoadForPublicPreviewAsync(
            V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion, ct);

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
            input, asOfDate, candidate, "PILOT_TEN_K_INTERMEDIATE_4D_MATCH",
            results, trace, createdAtUtc, createdAtUtc.Add(PreviewLifetime), generatedPreviewPlanPayload);
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
    /// never invoked from this reachable dark path). StartDate mirrors
    /// AsOfDate, exactly matching <see cref="BuildInputSnapshot"/>'s own
    /// documented Phase 4E.1 simplification — no new date policy is
    /// introduced. All results are deliberately discarded: this call's only
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
            // Phase 4G.2: now safe to enforce. Every request reaching this
            // method has already passed PlanServices.GeneratePreviewAsync's
            // upstream guard, which only allows RaceHorizonPolicy.Classify ==
            // ExactStandaloneCoreSupported (exactly 12 weeks) through to
            // catalog generation — the one horizon live-verified to produce
            // a schedule ending exactly one day before RaceDate (established
            // convention: StartDate=2026-07-20/RaceDate=2026-10-12 -> final
            // session 2026-10-11). A second, independent check on the actual
            // materialized output, so a future regression anywhere upstream
            // (e.g. a horizon-aware allocator change that doesn't correctly
            // honor this convention) cannot silently reintroduce a
            // misaligned plan. Deliberately NOT in the catch filter below —
            // it must propagate as itself, not be rewrapped into a 500.
            if (request.RaceDate is { } raceDateForAlignment)
            {
                var expectedWeekCount = RunningApp.Application.Common.RaceHorizonPolicy.ExactStandaloneCoreSupportedWeeks;
                var daysBeforeRace = raceDateForAlignment.DayNumber - datedSkeleton.EndDate.DayNumber;
                const int maxAllowedTrailingGapDays = 7;
                if (datedSkeleton.Weeks.Count != expectedWeekCount || daysBeforeRace < 0 || daysBeforeRace > maxAllowedTrailingGapDays)
                {
                    throw new CatalogRaceDateAlignmentInvalidException(
                        $"Generated schedule for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' has " +
                        $"{datedSkeleton.Weeks.Count} week(s) ending {datedSkeleton.EndDate:yyyy-MM-dd}, which is not aligned to " +
                        $"RaceDate {raceDateForAlignment:yyyy-MM-dd} (expected exactly {expectedWeekCount} weeks ending within " +
                        $"{maxAllowedTrailingGapDays} days before the race, never after it).");
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
                candidate, boundPlan, prescriptionContext, volumePlan, definitionMap));

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
        catch (Exception ex) when (ex is PlanCatalogLoadException or CatalogWorkoutBindingException or CatalogPrescriptionContractException or CatalogVolumePlanningException or CatalogSessionPrescriptionException or CatalogPublicMaterializationException)
        {
            throw new PlanPreviewGenerationFailedException(
                $"CATALOG_INTERNAL_WORKOUT_BINDING_FAILED: internal exact workout-definition binding failed " +
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
        RecentLongestRunKm = request.RecentLongestRunKm,
        RecentWeeklyVolumeKm = request.RecentWeeklyVolumeKm,
        RecentRunsPerWeek = request.RecentRunsPerWeek,
        RecentRaceDistanceKm = request.RecentRace is null ? null : GoalDistanceKm.Resolve(request.RecentRace.Distance),
        RecentRaceFinishTimeSeconds = request.RecentRace?.FinishTimeSeconds,
        RecentRaceDate = request.RecentRace?.RaceDate,
    };
}
