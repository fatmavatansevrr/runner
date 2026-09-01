using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

public sealed class CatalogLivePilotOptions
{
    public const string SectionName = "CatalogLivePilot";
    public bool Enabled { get; set; } = false;
}

/// <summary>
/// Phase 4F.9.3 — Development-only local-acceptance seam. Lets a developer
/// manually exercise the real HTTP preview→confirm flow against the
/// TEN_K__4D__INTERMEDIATE v10 pilot candidate WITHOUT publishing it: the
/// real catalog artifact on disk stays DRAFT forever (this never writes to
/// it, never adds a publication-ledger entry, and never touches
/// <see cref="CatalogLivePilotOptions"/>'s own default). It only lets
/// <see cref="LivePlanPreviewRoutingService"/> treat the loaded lifecycle
/// status as "PUBLISHED" for the purpose of a single route decision, in
/// Development only.
///
/// All three flags default false. All three must be explicitly true, AND
/// the running environment must be Development
/// (<see cref="IHostEnvironment.IsDevelopment"/>, checked independently of
/// configuration so an environment-variable override in a non-Development
/// environment can never activate this), for the override to take effect.
/// Every use is logged at Warning level naming the override explicitly.
/// </summary>
public sealed class LocalCatalogAcceptanceOptions
{
    public const string SectionName = "LocalCatalogAcceptance";

    /// <summary>Master switch. Must be true for either flag below to have any effect.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>When true (and every other gate is satisfied), the route decision treats the pilot candidate's lifecycle status as PUBLISHED. Never mutates the real candidate artifact or any publication ledger.</summary>
    public bool TreatPilotCandidateAsPublished { get; set; } = false;

    /// <summary>Redundant, independent final gate — must also be true. Defense in depth: no single flag can activate the override alone.</summary>
    public bool EnableCatalogRoute { get; set; } = false;
}

internal enum LivePlanPreviewRoute
{
    CatalogLive,
    Legacy,
    CatalogSupportedButNotPublished,
    CatalogSupportedButActivationDisabled,
    CatalogRequestUnsupported,
    CatalogCoreLengthNotImplemented,
    CatalogGenerationInfeasible,
    CatalogTwoDayCoreEightOrNineWeekFormallyNonSupported,
    RequestInvalid
}

internal enum LivePlanPreviewRouteReason
{
    PilotPublishedAndActivated,
    NotPilotRequest,
    PilotCandidateNotPublished,
    PilotCandidateMissingOrUnreadable,
    PilotActivationDisabled,
    UnsupportedCycleLength,
    CoreLengthRecognizedButNotImplemented,
    KnownInfeasibleEightWeekExplicitZero,
    TwoDayCoreEightOrNineWeekNonSupportFormalizedFinal,
    RequestInvalid
}

internal sealed record LivePlanPreviewRouteDecision(
    string PolicyKey,
    int PolicyVersion,
    GoalType GoalType,
    GoalDistance GoalDistance,
    RunningBackground Level,
    int DaysPerWeek,
    int? CycleLengthWeeks,
    string? CandidateKey,
    int? CandidateVersion,
    string? CandidateLifecycleStatus,
    bool ActivationEnabled,
    LivePlanPreviewRoute Route,
    LivePlanPreviewRouteReason Reason,
    bool FallbackPermitted,
    string Provenance);

internal static class V1LiveCatalogPilotRoutingPolicy
{
    public const string PolicyKey = "V1_LIVE_CATALOG_PILOT_ROUTING_POLICY";
    public const int PolicyVersion = 1;
    public const string CandidateKey = V1CatalogPilotIdentityPolicy.CandidateKey;
    public const int CandidateVersion = V1CatalogPilotIdentityPolicy.CandidateVersion;

    public static LivePlanPreviewRouteDecision Evaluate(
        GeneratePreviewRequest request,
        DateOnly asOfDate,
        int? candidateMinimumWeeks,
        int? candidateMaximumWeeks,
        string? candidateLifecycleStatus,
        bool activationEnabled)
    {
        // The available horizon is StartDate-to-RaceDate, never "now"-to-
        // RaceDate — a request with a StartDate in the past or far future
        // relative to "today" must still be classified by its own real
        // horizon. Uses the single centralized calculation (RaceHorizonPolicy)
        // so this can never disagree with the fail-closed guard in
        // PlanServices.GeneratePreviewAsync or any other horizon decision.
        // asOfDate remains reserved for recency/time-adequacy evaluation
        // elsewhere in the catalog pipeline — unrelated to this calculation.
        var canonicalHorizon = request.RaceDate is { } raceDate
            ? RunningApp.Application.Common.RaceHorizonPolicy.Decide(request.StartDate, raceDate)
            : null;
        int? cycleLength = canonicalHorizon?.AvailableFullWeeks;
        var pilotMatch = V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            request.GoalType,
            request.GoalDistance,
            request.Level,
            request.DaysPerWeek);

        if (!pilotMatch)
        {
            return Decision(request, cycleLength, candidateLifecycleStatus, activationEnabled, LivePlanPreviewRoute.Legacy, LivePlanPreviewRouteReason.NotPilotRequest, true);
        }
        if (request.RaceDate is null)
        {
            return Decision(request, cycleLength, candidateLifecycleStatus, activationEnabled, LivePlanPreviewRoute.RequestInvalid, LivePlanPreviewRouteReason.RequestInvalid, false);
        }
        // Running Background V2.1: bounds are sourced from the real loaded
        // candidate's master-template core-cycle (plan-catalog/catalog/
        // templates/ten-k-master.v6.json's coreCycle.minimumWeeks/
        // maximumWeeks), not a duplicated literal — the prior [8,14]
        // hardcoded range matched the artifact by coincidence, not by
        // construction, and would have silently drifted out of sync with
        // a future master-template version. If bounds are unavailable
        // (candidate not yet loaded — see LivePlanPreviewRoutingService.
        // Decide's short-circuit path for non-pilot/invalid requests), the
        // request is conservatively treated as unsupported rather than
        // guessed.
        var candidateHorizon = candidateMinimumWeeks is { } min &&
            candidateMaximumWeeks is { } max && request.RaceDate is { } candidateRaceDate
            ? RunningApp.Application.Common.RaceHorizonPolicy.Decide(
                request.StartDate, candidateRaceDate, min,
                RunningApp.Application.Common.RaceHorizonPolicy.ExactStandaloneCoreSupportedWeeks, max)
            : null;
        if (candidateHorizon is not null)
        {
            cycleLength = candidateHorizon.AvailableFullWeeks;
        }
        var withinSupportedCycle = candidateHorizon?.Mode is
            RunningApp.Application.RuntimeCatalog.Schedule.Horizon.CoreHorizonMode.CompressedCore or
            RunningApp.Application.RuntimeCatalog.Schedule.Horizon.CoreHorizonMode.PreferredCore or
            RunningApp.Application.RuntimeCatalog.Schedule.Horizon.CoreHorizonMode.ExtendedCore;
        if (!withinSupportedCycle)
        {
            return Decision(request, cycleLength, candidateLifecycleStatus, activationEnabled, LivePlanPreviewRoute.CatalogRequestUnsupported, LivePlanPreviewRouteReason.UnsupportedCycleLength, false);
        }

        // Legacy compatibility branch retained for the old route/result enum.
        // The canonical 4G.5L mapping no longer emits this classification:
        // complete 8-14-week compressed/preferred/extended cores are routed
        // from the same CoreHorizonDecision consumed above.
        if (cycleLength is { } classifiedWeeks &&
            RunningApp.Application.Common.RaceHorizonPolicy.Classify(classifiedWeeks) ==
                RunningApp.Application.Common.RaceHorizonClassification.CoreLengthRecognizedButNotImplemented)
        {
            return Decision(request, cycleLength, candidateLifecycleStatus, activationEnabled, LivePlanPreviewRoute.CatalogCoreLengthNotImplemented, LivePlanPreviewRouteReason.CoreLengthRecognizedButNotImplemented, false);
        }

        // Phase 10K-GEN.20 -- 2D Core's own formally-final non-support
        // boundary (GEN.18, TWO_D_CORE_EIGHT_AND_NINE_WEEK_NON_SUPPORT_FORMALIZED_FINAL).
        // Unlike the Intermediate-4D-specific explicit-zero-readiness check
        // immediately below (which only rejects one particular readiness
        // input at week 8), 2D's own 8/9-week gap is a real, deterministic,
        // readiness-independent capacity shortfall (TEN_K_MASTER's RACE_SPECIFIC
        // phase-week bound colliding with GEN.16's halved exposure minimums,
        // per GEN.17 §6/GEN.18 §1.1) that fails identically for EVERY readiness
        // input at both levels -- so this check is deliberately unconditional
        // on readiness, and deliberately placed BEFORE the real generation
        // pipeline would otherwise be reached (that pipeline's own internal
        // ProgressionPhaseCapacityInsufficientException is real and correct,
        // but surfaces publicly only as an opaque, generically-worded 500 --
        // this short-circuit instead returns a precise, GEN.18-citing typed
        // rejection). Both candidates' real CoreCycle.MinimumWeeks is 8 (the
        // shared TEN_K_MASTER v11 template, unrelated to 2D specifically), so
        // 8/9-week 2D requests would otherwise pass the withinSupportedCycle
        // check above and reach real generation undetected.
        if (cycleLength is 8 or 9 &&
            request.DaysPerWeek == 2 &&
            (request.Level == RunningBackground.Beginner || request.Level == RunningBackground.Intermediate))
        {
            return Decision(request, cycleLength, candidateLifecycleStatus, activationEnabled, LivePlanPreviewRoute.CatalogTwoDayCoreEightOrNineWeekFormallyNonSupported, LivePlanPreviewRouteReason.TwoDayCoreEightOrNineWeekNonSupportFormalizedFinal, false);
        }

        // Existing readiness-specific fail-closed rule for the activated
        // eight-week boundary; independent of horizon arithmetic. Deliberately
        // scoped to Intermediate only (GEN.4E) -- this short-circuit was
        // validated against Intermediate's own known-infeasible case only,
        // and short-circuiting here would otherwise return the wrong
        // (Intermediate-shared) typed reason for Beginner's real, distinct
        // BEGINNER_FOUR_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT
        // ineligibility. Beginner's own week-8 explicit-zero case instead
        // falls through to the real planner/typed-exception path below,
        // identically to weeks 9-12.
        if (cycleLength == 8 && request.Level == RunningBackground.Intermediate && request.DaysPerWeek == V1CatalogPilotIdentityPolicy.DaysPerWeek && request.RecentWeeklyVolumeKm == 0)
        {
            return Decision(request, cycleLength, candidateLifecycleStatus, activationEnabled, LivePlanPreviewRoute.CatalogGenerationInfeasible, LivePlanPreviewRouteReason.KnownInfeasibleEightWeekExplicitZero, false);
        }
        if (string.IsNullOrWhiteSpace(candidateLifecycleStatus))
        {
            return Decision(request, cycleLength, candidateLifecycleStatus, activationEnabled, LivePlanPreviewRoute.CatalogRequestUnsupported, LivePlanPreviewRouteReason.PilotCandidateMissingOrUnreadable, false);
        }
        if (!string.Equals(candidateLifecycleStatus, "PUBLISHED", StringComparison.Ordinal))
        {
            return Decision(request, cycleLength, candidateLifecycleStatus, activationEnabled, LivePlanPreviewRoute.CatalogSupportedButNotPublished, LivePlanPreviewRouteReason.PilotCandidateNotPublished, true);
        }
        if (!activationEnabled)
        {
            return Decision(request, cycleLength, candidateLifecycleStatus, activationEnabled, LivePlanPreviewRoute.CatalogSupportedButActivationDisabled, LivePlanPreviewRouteReason.PilotActivationDisabled, true);
        }

        return Decision(request, cycleLength, candidateLifecycleStatus, activationEnabled, LivePlanPreviewRoute.CatalogLive, LivePlanPreviewRouteReason.PilotPublishedAndActivated, false);
    }

    private static LivePlanPreviewRouteDecision Decision(
        GeneratePreviewRequest request,
        int? cycleLength,
        string? candidateLifecycleStatus,
        bool activationEnabled,
        LivePlanPreviewRoute route,
        LivePlanPreviewRouteReason reason,
        bool fallbackPermitted)
    {
        var resolvedIdentity = V1CatalogPilotIdentityPolicy.TryResolveCandidate(request.Level, request.DaysPerWeek);
        return new(
        PolicyKey,
        PolicyVersion,
        request.GoalType,
        request.GoalDistance,
        request.Level,
        request.DaysPerWeek,
        cycleLength,
        resolvedIdentity?.CandidateKey ?? CandidateKey,
        resolvedIdentity?.CandidateVersion ?? CandidateVersion,
        candidateLifecycleStatus,
        activationEnabled,
        route,
        reason,
        fallbackPermitted,
        "typed request identity; explicit lifecycle status; CatalogLivePilot.Enabled");
    }
}

internal static class LivePlanPreviewRouteDecisionValidator
{
    public static void Validate(LivePlanPreviewRouteDecision decision)
    {
        var invalid = decision.PolicyKey != V1LiveCatalogPilotRoutingPolicy.PolicyKey ||
                      decision.PolicyVersion != V1LiveCatalogPilotRoutingPolicy.PolicyVersion ||
                      string.IsNullOrWhiteSpace(decision.Provenance) ||
                      string.IsNullOrWhiteSpace(decision.CandidateKey) ||
                      decision.CandidateVersion is null ||
                      (decision.Route != LivePlanPreviewRoute.Legacy &&
                       decision.Route != LivePlanPreviewRoute.RequestInvalid &&
                       decision.GoalType == GoalType.Race &&
                       decision.CycleLengthWeeks is null) ||
                      (decision.Route == LivePlanPreviewRoute.CatalogLive &&
                       (!decision.ActivationEnabled ||
                        decision.CandidateLifecycleStatus != "PUBLISHED" ||
                        // GEN.4E: delegates to the single canonical allow-list
                        // (V1CatalogPilotIdentityPolicy) instead of a
                        // duplicated enum comparison, so this invariant can
                        // never silently drift out of sync with the real
                        // supported-identity list as new cells are added.
                        !V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
                            decision.GoalType, decision.GoalDistance, decision.Level, decision.DaysPerWeek) ||
                        // Cycle-length bounds are enforced by Evaluate against
                        // the real candidate's core-cycle (Running Background
                        // V2.1) — this invariant only re-asserts that a value
                        // was actually computed, not a duplicated numeric
                        // range, so it can never drift out of sync with the
                        // real artifact bounds.
                        decision.CycleLengthWeeks is null ||
                        decision.Reason != LivePlanPreviewRouteReason.PilotPublishedAndActivated));
        if (invalid)
        {
            throw new CatalogLiveRouteDecisionInvalidException("Live catalog route decision failed validation.");
        }
    }
}

public sealed class LivePlanPreviewRoutingService : IGenerationRouteDecider
{
    private readonly CatalogLivePilotOptions _options;
    private readonly LocalCatalogAcceptanceOptions _localAcceptanceOptions;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IPlanCatalogBundleLoader _bundleLoader;
    private readonly ILogger<LivePlanPreviewRoutingService> _logger;

    public LivePlanPreviewRoutingService(
        IOptions<CatalogLivePilotOptions> options,
        IOptions<LocalCatalogAcceptanceOptions> localAcceptanceOptions,
        IHostEnvironment hostEnvironment,
        IPlanCatalogBundleLoader bundleLoader,
        ILogger<LivePlanPreviewRoutingService> logger)
    {
        _options = options.Value;
        _localAcceptanceOptions = localAcceptanceOptions.Value;
        _hostEnvironment = hostEnvironment;
        _bundleLoader = bundleLoader;
        _logger = logger;
    }

    public GenerationRouteDecision Decide(GeneratePreviewRequest request)
    {
        var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var pilotMatch = V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            request.GoalType,
            request.GoalDistance,
            request.Level,
            request.DaysPerWeek);

        if (!pilotMatch || request.RaceDate is null)
        {
            // Identity mismatch and a missing race date are pure
            // request-shape checks that never need the real candidate's
            // core-cycle bounds, so no bundle I/O is performed here — this
            // preserves the short-circuit behavior of the legacy/non-pilot
            // hot path from before Running Background V2.1.
            var shortCircuitDecision = V1LiveCatalogPilotRoutingPolicy.Evaluate(
                request,
                asOfDate,
                candidateMinimumWeeks: null,
                candidateMaximumWeeks: null,
                candidateLifecycleStatus: request.GoalType == GoalType.Race ? "PENDING_LOAD" : "NOT_APPLICABLE",
                activationEnabled: _options.Enabled);
            LivePlanPreviewRouteDecisionValidator.Validate(shortCircuitDecision);
            return ToGenerationRouteDecision(shortCircuitDecision);
        }

        // Pilot-identity match with a race date present: cycle-length
        // validation now requires the real candidate's core-cycle bounds
        // (Running Background V2.1), so the candidate must be loaded before
        // that check can run — unlike the lifecycle-status-only load this
        // replaced, an out-of-range cycle length no longer short-circuits
        // before this I/O, because "out of range" can only be determined
        // once the real bounds are known.
        var identity = V1CatalogPilotIdentityPolicy.ResolveCandidate(request.Level, request.DaysPerWeek);
        PlanCatalogCandidateSummary candidate;
        try
        {
            candidate = _bundleLoader
                .LoadCandidateAsync(identity.CandidateKey, identity.CandidateVersion)
                .GetAwaiter()
                .GetResult();
        }
        catch (PlanCatalogLoadException ex)
        {
            throw new CatalogLivePilotRequestUnsupportedException(
                $"Catalog pilot candidate {identity.CandidateKey} v{identity.CandidateVersion} could not be loaded for live routing.",
                ex);
        }

        // Phase 4F.9.3 local-acceptance override: the real on-disk candidate
        // status (candidate.CandidateStatus, above) is NEVER mutated. Only
        // the value fed into THIS route decision may be swapped, and only
        // when every independent gate holds: Development environment
        // (checked here, not just via config), LocalCatalogAcceptance:
        // Enabled, :EnableCatalogRoute, and :TreatPilotCandidateAsPublished
        // all true. The candidate's real core-cycle bounds are never
        // overridden by this seam — only lifecycle status is.
        var effectiveCandidateStatus = candidate.CandidateStatus;
        var localAcceptanceOverrideActive =
            _hostEnvironment.IsDevelopment() &&
            _localAcceptanceOptions.Enabled &&
            _localAcceptanceOptions.EnableCatalogRoute &&
            _localAcceptanceOptions.TreatPilotCandidateAsPublished;

        if (localAcceptanceOverrideActive)
        {
            _logger.LogWarning(
                "LocalCatalogAcceptance override ACTIVE (Development only): treating candidate {CandidateKey} v{CandidateVersion} " +
                "real lifecycle status '{RealStatus}' as PUBLISHED for this route decision only. " +
                "The real catalog artifact and any publication ledger are unmodified.",
                V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion, candidate.CandidateStatus);
            effectiveCandidateStatus = "PUBLISHED";
        }

        var decision = V1LiveCatalogPilotRoutingPolicy.Evaluate(
            request,
            asOfDate,
            candidateMinimumWeeks: candidate.CoreCycle.MinimumWeeks,
            candidateMaximumWeeks: candidate.CoreCycle.MaximumWeeks,
            candidateLifecycleStatus: effectiveCandidateStatus,
            activationEnabled: _options.Enabled);
        LivePlanPreviewRouteDecisionValidator.Validate(decision);
        return ToGenerationRouteDecision(decision);
    }

    private GenerationRouteDecision ToGenerationRouteDecision(LivePlanPreviewRouteDecision decision)
    {
        _logger.LogInformation(
            "LivePlanPreviewRouting: policyKey={PolicyKey}, policyVersion={PolicyVersion}, route={Route}, candidateKey={CandidateKey}, candidateVersion={CandidateVersion}, cycleLengthWeeks={CycleLengthWeeks}, lifecycleStatus={LifecycleStatus}, activationEnabled={ActivationEnabled}, reason={Reason}, fallbackPermitted={FallbackPermitted}",
            decision.PolicyKey,
            decision.PolicyVersion,
            decision.Route,
            decision.CandidateKey,
            decision.CandidateVersion,
            decision.CycleLengthWeeks,
            decision.CandidateLifecycleStatus,
            decision.ActivationEnabled,
            decision.Reason,
            decision.FallbackPermitted);

        return decision.Route switch
        {
            LivePlanPreviewRoute.CatalogLive =>
                new GenerationRouteDecision(GenerationSource.Catalog, decision.Reason.ToString()),
            LivePlanPreviewRoute.Legacy or
            LivePlanPreviewRoute.CatalogSupportedButNotPublished or
            LivePlanPreviewRoute.CatalogSupportedButActivationDisabled =>
                new GenerationRouteDecision(GenerationSource.LegacySql, decision.Reason.ToString()),
            LivePlanPreviewRoute.RequestInvalid =>
                throw new CatalogLivePilotRequestUnsupportedException("Catalog pilot request is invalid for live routing."),
            LivePlanPreviewRoute.CatalogRequestUnsupported =>
                throw new CatalogLivePilotRequestUnsupportedException("Catalog pilot request is unsupported for live routing."),
            // Same canonical exception PlanServices.GeneratePreviewAsync
            // throws for this classification (RaceHorizonPolicy.Classify ==
            // CoreLengthRecognizedButNotImplemented) -- one error contract
            // regardless of which layer detects it first.
            LivePlanPreviewRoute.CatalogCoreLengthNotImplemented =>
                throw new RunningApp.Application.Exceptions.PlanCoreHorizonUnsupportedException(
                    "The requested race-plan horizon is recognized, but this exact core length is not yet implemented safely."),
            LivePlanPreviewRoute.CatalogGenerationInfeasible =>
                throw new CatalogLivePilotGenerationInfeasibleException("Catalog pilot request is a known generation-infeasible readiness/cycle combination."),
            LivePlanPreviewRoute.CatalogTwoDayCoreEightOrNineWeekFormallyNonSupported =>
                throw new CatalogTwoDayCoreEightOrNineWeekNonSupportedException(
                    $"2-day-per-week TEN_K Core plans are formally, permanently non-representable at {decision.CycleLengthWeeks} weeks " +
                    "(TWO_D_CORE_EIGHT_AND_NINE_WEEK_NON_SUPPORT_FORMALIZED_FINAL, Phase 10K-GEN.18). The supported 2-day-per-week Core " +
                    "range is exactly 10-14 weeks. This is not a temporary or readiness-dependent restriction -- it is a final, evidenced " +
                    "non-representability determination that applies identically to Beginner and Intermediate at both 8 and 9 weeks."),
            _ => throw new CatalogLiveRouteDecisionInvalidException("Unsupported live catalog route decision.")
        };
    }
}

public class CatalogLiveRoutingException : Exception
{
    public CatalogLiveRoutingException(string message) : base(message) { }
    public CatalogLiveRoutingException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class CatalogLivePilotNotPublishedException : CatalogLiveRoutingException
{
    public CatalogLivePilotNotPublishedException(string message) : base(message) { }
}

public sealed class CatalogLivePilotActivationDisabledException : CatalogLiveRoutingException
{
    public CatalogLivePilotActivationDisabledException(string message) : base(message) { }
}

public sealed class CatalogLivePilotRequestUnsupportedException : CatalogLiveRoutingException
{
    public CatalogLivePilotRequestUnsupportedException(string message) : base(message) { }
    public CatalogLivePilotRequestUnsupportedException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class CatalogLivePilotGenerationInfeasibleException : CatalogLiveRoutingException
{
    public CatalogLivePilotGenerationInfeasibleException(string message) : base(message) { }
}

public sealed class CatalogLiveRouteDecisionInvalidException : CatalogLiveRoutingException
{
    public CatalogLiveRouteDecisionInvalidException(string message) : base(message) { }
}

public sealed class CatalogLiveFallbackNotPermittedException : CatalogLiveRoutingException
{
    public CatalogLiveFallbackNotPermittedException(string message) : base(message) { }
}

/// <summary>
/// Phase 10K-GEN.20 -- thrown for a 2-day-per-week TEN_K Core request at
/// exactly 8 or 9 weeks, citing GEN.18's formal, final
/// TWO_D_CORE_EIGHT_AND_NINE_WEEK_NON_SUPPORT_FORMALIZED_FINAL classification.
/// Deliberately a distinct type from <see cref="CatalogLivePilotRequestUnsupportedException"/>
/// (generic "unsupported cycle length") and from <see cref="CatalogLivePilotGenerationInfeasibleException"/>
/// (readiness-specific): this is neither -- it is a known, permanently
/// non-representable structural capacity gap, independent of readiness, that
/// this repository's own governance record (GEN.18) already closed as final,
/// not a routing miss or a request-input problem.
/// </summary>
public sealed class CatalogTwoDayCoreEightOrNineWeekNonSupportedException : CatalogLiveRoutingException
{
    public CatalogTwoDayCoreEightOrNineWeekNonSupportedException(string message) : base(message) { }
}
