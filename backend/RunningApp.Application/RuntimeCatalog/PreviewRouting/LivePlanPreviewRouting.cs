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
        int? cycleLength = request.RaceDate is { } raceDate
            ? RunningApp.Application.Common.RaceHorizonPolicy.CalculateAvailableWeeks(request.StartDate, raceDate)
            : null;
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
        var withinSupportedCycle =
            cycleLength is { } weeks &&
            candidateMinimumWeeks is { } min &&
            weeks >= min &&
            (candidateMaximumWeeks is not { } max || weeks <= max);
        if (!withinSupportedCycle)
        {
            return Decision(request, cycleLength, candidateLifecycleStatus, activationEnabled, LivePlanPreviewRoute.CatalogRequestUnsupported, LivePlanPreviewRouteReason.UnsupportedCycleLength, false);
        }

        // Temporary safety constraint (Phase 4G.2): the candidate's coreCycle
        // bounds nominally advertise 8-14 weeks as "in range", but
        // CatalogPhaseAllocationResolver is not yet horizon-aware — it always
        // emits its fixed default ~12-week allocation regardless of the
        // accepted cycle length. Only the one exact length proven (live) to
        // align with RaceDate is routed to catalog generation; every other
        // in-range horizon fails closed here, one canonical decision shared
        // with PlanServices.GeneratePreviewAsync via RaceHorizonPolicy.Classify
        // so the two layers can never disagree. See
        // PHASE4G_2_EXACT_TWELVE_WEEK_ONLY_SAFETY_CONSTRAINT.md.
        if (cycleLength is { } classifiedWeeks &&
            RunningApp.Application.Common.RaceHorizonPolicy.Classify(classifiedWeeks) ==
                RunningApp.Application.Common.RaceHorizonClassification.CoreLengthRecognizedButNotImplemented)
        {
            return Decision(request, cycleLength, candidateLifecycleStatus, activationEnabled, LivePlanPreviewRoute.CatalogCoreLengthNotImplemented, LivePlanPreviewRouteReason.CoreLengthRecognizedButNotImplemented, false);
        }

        // Unreachable now that CoreLengthRecognizedButNotImplemented is
        // rejected above (an 8-week horizon never reaches this check) —
        // retained so the explicit-zero-at-8-weeks infeasibility case is
        // still documented/typed for whenever 8-week generation is
        // reactivated by a future horizon-aware composition phase.
        if (cycleLength == 8 && request.RecentWeeklyVolumeKm == 0)
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
        bool fallbackPermitted) => new(
        PolicyKey,
        PolicyVersion,
        request.GoalType,
        request.GoalDistance,
        request.Level,
        request.DaysPerWeek,
        cycleLength,
        CandidateKey,
        CandidateVersion,
        candidateLifecycleStatus,
        activationEnabled,
        route,
        reason,
        fallbackPermitted,
        "typed request identity; explicit lifecycle status; CatalogLivePilot.Enabled");
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
                        decision.GoalType != GoalType.Race ||
                        decision.GoalDistance != GoalDistance.TenK ||
                        decision.Level != RunningBackground.Intermediate ||
                        decision.DaysPerWeek != 4 ||
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
        PlanCatalogCandidateSummary candidate;
        try
        {
            candidate = _bundleLoader
                .LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion)
                .GetAwaiter()
                .GetResult();
        }
        catch (PlanCatalogLoadException ex)
        {
            throw new CatalogLivePilotRequestUnsupportedException(
                $"Catalog pilot candidate {V1LiveCatalogPilotRoutingPolicy.CandidateKey} v{V1LiveCatalogPilotRoutingPolicy.CandidateVersion} could not be loaded for live routing.",
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
