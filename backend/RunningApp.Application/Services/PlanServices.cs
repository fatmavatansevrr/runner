// [ignoring loop detection]
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.Application.Commands.Plan;
using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.PlanGeneration;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Application.Validation;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using TrainingDaySource = RunningApp.Domain.Enums.TrainingDaySource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Application.Services;

public class PlanServices : IPlanPreviewService, IPlanConfirmationService, IPlanManagementService
{
    private readonly AppDbContext _context;
    private readonly IPlanGenerationEngine _planGenerationEngine;
    private readonly IGenerationRouteDecider _routeDecider;
    private readonly ICatalogPreviewGenerator _catalogPreviewGenerator;
    private readonly ICatalogPlanConfirmationService _catalogConfirmationService;
    private readonly PlanCatalogOptions _planCatalogOptions;
    private readonly bool _preparationRunwayPilotActivationEnabled;
    private readonly bool _preparationRunwayConfirmationEnabled;

    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    // Lightweight options for peeking at GenerationSource only — does not need
    // full snake_case enum deserialization since GenerationSource is a plain string.
    private static readonly JsonSerializerOptions GenerationSourcePeekOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly ILogger<PlanServices> _logger;

    /// <summary>
    /// Backend Integration Phase 4E.1: adds the route decider and catalog
    /// preview generator. Both are REQUIRED constructor dependencies (this
    /// codebase's established explicit-DI convention, no hidden defaults) —
    /// existing test call sites must now supply them explicitly.
    ///
    /// Backend Integration Phase 4E.2: adds the catalog confirmation service.
    /// This does NOT add any routing/generation dependency to the confirm path:
    /// <see cref="ICatalogPlanConfirmationService"/> has no dependency on
    /// <see cref="IGenerationRouteDecider"/>, <see cref="ICatalogPreviewGenerator"/>,
    /// resolvers, or <c>StageEligibilityEvaluator</c>.
    /// </summary>
    public PlanServices(
        AppDbContext context,
        IPlanGenerationEngine planGenerationEngine,
        ILogger<PlanServices> logger,
        IGenerationRouteDecider routeDecider,
        ICatalogPreviewGenerator catalogPreviewGenerator,
        ICatalogPlanConfirmationService catalogConfirmationService,
        IOptions<PlanCatalogOptions> planCatalogOptions,
        IOptions<PreparationRunwayPilotActivationOptions> preparationRunwayPilotActivationOptions)
    {
        _context = context;
        _planGenerationEngine = planGenerationEngine;
        _logger = logger;
        _routeDecider = routeDecider;
        _catalogPreviewGenerator = catalogPreviewGenerator;
        _catalogConfirmationService = catalogConfirmationService;
        _planCatalogOptions = planCatalogOptions.Value;
        _preparationRunwayPilotActivationEnabled = preparationRunwayPilotActivationOptions.Value.Enabled;
        _preparationRunwayConfirmationEnabled = preparationRunwayPilotActivationOptions.Value.ConfirmationEnabled;
    }

    /// <summary>
    /// Backend Integration Phase 4G.6B — the exact, narrow activation scope
    /// for the 15-20 week TEN_K Preparation Runway public preview. Verified
    /// through typed candidate identity, never inferred from distance/duration
    /// alone.
    /// </summary>
    private static bool IsPreparationRunwayPilotScope(GeneratePreviewRequest request) =>
        request.GoalType == GoalType.Race &&
        request.GoalDistance == GoalDistance.TenK &&
        request.Level == RunningBackground.Intermediate &&
        request.DaysPerWeek == 4;

    /// <summary>
    /// Public entry point for <c>POST /api/v1/plans/generate-preview/race</c>.
    /// <paramref name="command"/> is already validated by
    /// <c>GenerateRacePlanPreviewRequestValidator</c> at the controller
    /// boundary — this just bridges into the unchanged internal pipeline.
    /// </summary>
    public Task<GeneratePreviewResponse> GenerateRacePlanPreviewAsync(Guid internalUserId, RacePlanPreviewCommand command, CancellationToken ct = default) =>
        GeneratePreviewAsync(internalUserId, GeneratePreviewCommandMapper.ToInternalRequest(command), ct);

    /// <summary>
    /// Public entry point for <c>POST /api/v1/plans/generate-preview/habit</c>.
    /// <paramref name="command"/> is already validated by
    /// <c>GenerateHabitPlanPreviewRequestValidator</c> at the controller
    /// boundary — this just bridges into the unchanged internal pipeline.
    /// </summary>
    public Task<GeneratePreviewResponse> GenerateHabitPlanPreviewAsync(Guid internalUserId, HabitPlanPreviewCommand command, CancellationToken ct = default) =>
        GeneratePreviewAsync(internalUserId, GeneratePreviewCommandMapper.ToInternalRequest(command), ct);

    public async Task<GeneratePreviewResponse> GeneratePreviewAsync(Guid internalUserId, GeneratePreviewRequest request, CancellationToken ct = default)
    {
        // ── Input validation ────────────────────────────────────────────────
        GeneratePreviewRequestValidator.Validate(request);

        // Canonical horizon decision and public fail-closed mapping.
        // Runs before template selection, route decision, catalog generation,
        // and any persistence — for BOTH the legacy and catalog paths. See
        // RaceHorizonPolicy — the single source of truth for this
        // calculation and classification, reused by the catalog routing
        // policy below so the two can never disagree.
        if (request.GoalType == GoalType.Race && request.RaceDate is { } raceDateForHorizonCheck)
        {
            var horizonDecision = RaceHorizonPolicy.Decide(request.StartDate, raceDateForHorizonCheck);
            var availableWeeks = horizonDecision.AvailableFullWeeks;
            var classification = RaceHorizonPolicy.Classify(horizonDecision);

            if (classification == RaceHorizonClassification.CompositionRequired)
            {
                // Backend Integration Phase 4G.6B: the ONLY narrowing of this
                // otherwise-unchanged fail-closed gate. Scoped strictly to
                // the exact pilot candidate (typed identity, never inferred
                // from distance/duration alone) and the direct-runway
                // 15-20 week range (PreparationRunwayPlusCore mode with
                // AvailableFullWeeks in [15,20]) -- 21+ weeks and every other
                // candidate/level/days-per-week/goal-type combination fall
                // through unchanged to the PlanHorizonCompositionRequiredException
                // below, exactly as before this phase.
                if (IsPreparationRunwayPilotScope(request) && availableWeeks is >= 15 and <= 20 && _preparationRunwayPilotActivationEnabled)
                {
                    return await GeneratePreparationRunwayPreviewAsync(internalUserId, request, horizonDecision, asOfDate: DateOnly.FromDateTime(DateTime.UtcNow), ct);
                }

                // Long-horizon preparation + race-core composition is not yet
                // implemented; a horizon above the approved standalone
                // maximum must never silently receive a shorter core anchored
                // at StartDate with RaceDate left unreached.
                _logger.LogWarning(
                    "GeneratePreview: race horizon requires not-yet-implemented preparation + race-core composition. " +
                    "StartDate={StartDate}, RaceDate={RaceDate}, AvailableWeeks={AvailableWeeks}, MaxSupportedStandaloneWeeks={MaxSupportedStandaloneWeeks}",
                    request.StartDate, raceDateForHorizonCheck, availableWeeks, RaceHorizonPolicy.MaximumSupportedStandaloneWeeks);
                throw new PlanHorizonCompositionRequiredException(
                    "The available race-plan horizon requires a preparation block before the supported race-training core. " +
                    $"Available horizon is approximately {availableWeeks} weeks; the currently supported standalone maximum is " +
                    $"{RaceHorizonPolicy.MaximumSupportedStandaloneWeeks} weeks.");
            }

            if (classification == RaceHorizonClassification.CoreLengthRecognizedButNotImplemented)
            {
                // Legacy compatibility branch. The current canonical mapping
                // does not emit this value; compressed/extended cores map to
                // StandaloneCoreSupported.
                var reasonCode = RaceHorizonPolicy.GetCoreHorizonUnsupportedReasonCode(availableWeeks);
                _logger.LogWarning(
                    "GeneratePreview: race core horizon recognized but not yet implemented. " +
                    "StartDate={StartDate}, RaceDate={RaceDate}, AvailableWeeks={AvailableWeeks}, " +
                    "ExactSupportedStandaloneWeeks={ExactSupportedStandaloneWeeks}, ReasonCode={ReasonCode}",
                    request.StartDate, raceDateForHorizonCheck, availableWeeks,
                    RaceHorizonPolicy.ExactStandaloneCoreSupportedWeeks, reasonCode);
                throw new PlanCoreHorizonUnsupportedException(
                    "The requested race-plan horizon is recognized, but this exact core length is not yet implemented safely. " +
                    $"Available horizon is approximately {availableWeeks} weeks; the only currently implemented standalone core " +
                    $"length is {RaceHorizonPolicy.ExactStandaloneCoreSupportedWeeks} weeks. Reason: {reasonCode}.");
            }

            // Below-minimum handling continues at routing; compressed,
            // preferred, and extended standalone modes proceed. Exact 12w0d
            // remains the PreferredCore path.
        }

        // Typed command, produced only after validation guarantees every
        // race-only/habit-only field is exactly as required -- see
        // GeneratePreviewCommandMapper's own docs for why this is the only
        // place a null-forgiving operator is allowed for these fields.
        // Used below by ConfirmPlanAsync-equivalent logic in this method's
        // callers and directly by the legacy-path field mapping later in
        // this method.
        var command = GeneratePreviewCommandMapper.ToCommand(request);

        // ── Sanitized input logging (no PII) ────────────────────────────────
        _logger.LogInformation(
            "GeneratePreview: goalType={GoalType}, goalDistance={GoalDistance}, level={Level}, " +
            "daysPerWeek={DaysPerWeek}, preferredDays={PreferredDays}, unit={Unit}",
            command.GoalType, command.GoalDistance, command.Level,
            command.DaysPerWeek, WeekdayCsv.ToCsv(command.PreferredDays) ?? "(null)", command.Unit);

        // ── Backend Integration Phase 4E.1: route decision, computed once, ──
        // before any generation begins. There is no "try catalog, catch,
        // fall back to SQL" anywhere below -- if the route is CATALOG, the
        // legacy SQL path (steps 1-4 below) is never reached, and any
        // catalog-flow failure propagates as a typed exception, final.
        var routeDecision = _routeDecider.Decide(request);
        _logger.LogInformation(
            "GeneratePreview: routeDecision.Source={Source}, routeDecision.RouteReason={RouteReason}",
            routeDecision.Source, routeDecision.RouteReason);

        if (routeDecision.Source == GenerationSource.Catalog)
        {
            return await GenerateCatalogPreviewAsync(internalUserId, request, routeDecision, ct);
        }

        // 1. Select a seeded template for the user's goals (placeholder engine; see IPlanGenerationEngine)
        var selection = await _planGenerationEngine.SelectTemplateAsync(request, ct);
        var template = selection.Template;

        // 2. Parse week/day layout from the template JSON
        var templateData = JsonSerializer.Deserialize<TemplateJsonData>(template.DataJson, SerializerOptions);

        if (templateData == null || templateData.Weeks == null)
        {
            throw new InvalidOperationException("Plan template data is invalid or empty.");
        }

        // 3. Map slot indices to actual dates starting from the request's own StartDate.
        // StartDate is the first day of the plan's first 7-day window and does not
        // need to be a Monday (Backend Integration contract alignment).
        // DateTimeKind.Utc is explicit here (matching every other DateTime.UtcNow-based
        // value in this class) -- DateOnly.ToDateTime defaults to Kind=Unspecified, which
        // Npgsql rejects for "timestamp with time zone" columns (TrainingDay.Date,
        // TrainingWeek.StartDate, TrainingPlan.StartedAt/EstimatedEndDate all map to one).
        var startOfWeek1 = request.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var previewWeeks = new List<PreviewWeekDto>();

        // ── Resolve preferred running days ──────────────────────────────────
        // PreferredDays is required (validated above); bridged to the legacy
        // full-day-name CSV shape this method's own weekday resolution expects.
        var preferredDays = ResolvePreferredDays(WeekdayCsv.ToCsv(request.PreferredDays), request.DaysPerWeek);

        for (int i = 0; i < templateData.Weeks.Count; i++)
        {
            var tempWeek = templateData.Weeks[i];
            var weekStart = startOfWeek1.AddDays(i * 7);
            var previewDays = new List<PreviewDayDto>();
            foreach (var tempDay in tempWeek.Days)
            {
                // Safe access: always use modulo to prevent IndexOutOfRangeException.
                // SlotIndex is 1-based in template data; convert to 0-based.
                var dayIndex = Math.Max(0, tempDay.SlotIndex - 1);
                var dayName = preferredDays[dayIndex % preferredDays.Length];

                // Maps dayName into the unique date inside [weekStart, weekStart + 6]
                // having that weekday — works regardless of which weekday weekStart
                // itself falls on (mirrors CatalogWeekSkeletonCalendarMaterializer.MapWeekdayToDateInWeek).
                var targetDayOfWeek = dayName switch
                {
                    "Monday"    => DayOfWeek.Monday,
                    "Tuesday"   => DayOfWeek.Tuesday,
                    "Wednesday" => DayOfWeek.Wednesday,
                    "Thursday"  => DayOfWeek.Thursday,
                    "Friday"    => DayOfWeek.Friday,
                    "Saturday"  => DayOfWeek.Saturday,
                    "Sunday"    => DayOfWeek.Sunday,
                    _           => DayOfWeek.Monday
                };
                var dayOffset = ((int)targetDayOfWeek - (int)weekStart.DayOfWeek + 7) % 7;

                previewDays.Add(new PreviewDayDto
                {
                    SlotIndex = tempDay.SlotIndex,
                    DayType = tempDay.DayType,
                    DistanceKm = tempDay.DistanceKm,
                    DurationMin = tempDay.DurationMin,
                    Intensity = tempDay.Intensity ?? "z2",
                    Date = weekStart.AddDays(dayOffset)
                });
            }

            previewWeeks.Add(new PreviewWeekDto
            {
                WeekNumber = tempWeek.WeekNumber,
                WeekType = tempWeek.WeekType,
                Days = previewDays
            });
        }

        var previewResponse = new GeneratePreviewResponse
        {
            PreviewId = Guid.NewGuid(),
            TemplateId = template.TemplateId,
            GoalType = request.GoalType,
            GoalDistance = request.GoalDistance,
            Level = request.Level,
            DaysPerWeek = request.DaysPerWeek,
            Unit = request.Unit,
            Weeks = previewWeeks,
            FallbackUsed = selection.FallbackUsed,
            FallbackReason = selection.FallbackReason
        };

        // 4. Save to PlanPreviews table
        var previewEntity = new PlanPreview
        {
            Id = previewResponse.PreviewId,
            InternalUserId = internalUserId,
            TemplateId = template.TemplateId,
            RequestPayloadJson = JsonSerializer.Serialize(request, SerializerOptions),
            PreviewPayloadJson = JsonSerializer.Serialize(previewResponse, SerializerOptions),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow
        };

        _context.PlanPreviews.Add(previewEntity);
        await _context.SaveChangesAsync(ct);

        return previewResponse;
    }

    /// <summary>
    /// Backend Integration Phase 4E.1 — the catalog pilot preview path.
    /// Computes AsOfDate exactly once (UTC-based: the only timezone
    /// convention evidenced anywhere in this class, e.g. the existing
    /// DateTime.UtcNow-based "next Monday" computation in the legacy flow
    /// below -- no per-user timezone policy exists in this backend to defer
    /// to instead) and passes it through the entire resolver pipeline via
    /// <see cref="ICatalogPreviewGenerator"/>. Persists the resulting
    /// <see cref="CatalogPreviewSnapshot"/> into the existing
    /// <c>PlanPreview.PreviewPayloadJson</c> column (per this phase's own
    /// preference for that column over a new persistence structure) --
    /// NOTE this is a structurally different JSON shape than the legacy
    /// flow's <see cref="GeneratePreviewResponse"/> serialization; see the
    /// minimal, explicitly-documented compatibility guard in
    /// <see cref="ConfirmPlanAsync"/> that prevents a catalog-sourced
    /// preview from being mis-processed as a legacy one. Phase 4E.1 does
    /// NOT make a catalog-sourced preview confirmable -- see
    /// PHASE4E_1_CATALOG_PREVIEW_ROUTING_AND_IMMUTABLE_RESOLUTION_SNAPSHOT.md.
    /// </summary>
    private async Task<GeneratePreviewResponse> GenerateCatalogPreviewAsync(
        Guid internalUserId, GeneratePreviewRequest request, GenerationRouteDecision routeDecision, CancellationToken ct)
    {
        var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // No try/catch here: CatalogCandidateNotPublishedException,
        // CatalogDependencyNotRuntimeEligibleException, CatalogPilotNotAvailableException,
        // RuntimeConditionRequiredInputMissingException, RuntimeConditionUnsupportedException,
        // RuntimeConditionDependencyUnresolvedException, and PlanPreviewGenerationFailedException
        // all propagate unchanged -- none is caught and converted into a
        // legacy-SQL fallback or a partially-generated plan.
        var snapshot = await _catalogPreviewGenerator.GenerateAsync(request, asOfDate, ct);

        var previewResponse = new GeneratePreviewResponse
        {
            PreviewId = Guid.NewGuid(),
            TemplateId = snapshot.CandidateKey,
            GoalType = request.GoalType,
            GoalDistance = request.GoalDistance,
            Level = request.Level,
            DaysPerWeek = request.DaysPerWeek,
            Unit = request.Unit,
            Weeks = MapCatalogPreviewWeeks(snapshot.GeneratedPreviewPlanPayload),
            FallbackUsed = false,
            FallbackReason = null,
        };

        var previewEntity = new PlanPreview
        {
            Id = previewResponse.PreviewId,
            InternalUserId = internalUserId,
            TemplateId = snapshot.CandidateKey,
            RequestPayloadJson = JsonSerializer.Serialize(request, SerializerOptions),
            // Deliberately the CatalogPreviewSnapshot's own JSON shape, NOT a
            // GeneratePreviewResponse -- see this method's own doc comment.
            PreviewPayloadJson = JsonSerializer.Serialize(snapshot, SerializerOptions),
            ExpiresAt = snapshot.ExpiresAtUtc,
            CreatedAt = snapshot.CreatedAtUtc,
        };

        _context.PlanPreviews.Add(previewEntity);
        await _context.SaveChangesAsync(ct);

        return previewResponse;
    }

    /// <summary>
    /// Backend Integration Phase 4G.6B — the scoped 15-20 week TEN_K
    /// Preparation Runway public preview path. Invokes
    /// <see cref="ICatalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync"/>
    /// exactly once (no duplicated runway generation logic here); this
    /// method's own job is limited to persisting the resulting (deliberately
    /// non-persistable) <see cref="CatalogPreviewSnapshot"/> and returning
    /// the mapped public response with an explicit
    /// <see cref="PreviewLifecycleClassification.PreparationRunwayPreviewNotConfirmable"/>
    /// classification. On any orchestration failure, no partial preview and
    /// no <c>PlanPreview</c> row are created -- the exception propagates
    /// unchanged.
    /// </summary>
    private async Task<GeneratePreviewResponse> GeneratePreparationRunwayPreviewAsync(
        Guid internalUserId, GeneratePreviewRequest request, CoreHorizonDecision horizonDecision, DateOnly asOfDate, CancellationToken ct)
    {
        var (snapshot, weeks) = await _catalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync(
            request, horizonDecision, asOfDate, _planCatalogOptions, confirmationEnabled: _preparationRunwayConfirmationEnabled, ct: ct);

        var previewResponse = new GeneratePreviewResponse
        {
            PreviewId = Guid.NewGuid(),
            TemplateId = snapshot.CandidateKey,
            GoalType = request.GoalType,
            GoalDistance = request.GoalDistance,
            Level = request.Level,
            DaysPerWeek = request.DaysPerWeek,
            Unit = request.Unit,
            Weeks = weeks.ToList(),
            FallbackUsed = false,
            FallbackReason = null,
            // Phase 4G.6C: lifecycle reflects whether this snapshot actually
            // carries a persistable payload -- never inferred, always set
            // from the same gate state that controlled whether the mapper
            // ran (never from GeneratedPreviewPlanPayload's mere presence).
            Lifecycle = _preparationRunwayConfirmationEnabled
                ? PreviewLifecycleClassification.PreparationRunwayPreviewConfirmable
                : PreviewLifecycleClassification.PreparationRunwayPreviewNotConfirmable,
        };

        var previewEntity = new PlanPreview
        {
            Id = previewResponse.PreviewId,
            InternalUserId = internalUserId,
            TemplateId = snapshot.CandidateKey,
            RequestPayloadJson = JsonSerializer.Serialize(request, SerializerOptions),
            // Same CatalogPreviewSnapshot shape as the 8-14 week catalog path
            // (GenerateCatalogPreviewAsync) -- but GeneratedPreviewPlanPayload
            // is always null here, which is exactly what makes this preview
            // automatically rejected by the existing, unmodified
            // CatalogPreviewNotPersistableException guard at confirm time.
            PreviewPayloadJson = JsonSerializer.Serialize(snapshot, SerializerOptions),
            ExpiresAt = snapshot.ExpiresAtUtc,
            CreatedAt = snapshot.CreatedAtUtc,
        };

        _context.PlanPreviews.Add(previewEntity);
        await _context.SaveChangesAsync(ct);

        return previewResponse;
    }

    private static List<PreviewWeekDto> MapCatalogPreviewWeeks(GeneratedCatalogPlanPayload? payload)
    {
        if (payload is null) return new List<PreviewWeekDto>();

        return payload.Weeks
            .OrderBy(w => w.WeekNumber)
            .Select(w => new PreviewWeekDto
            {
                WeekNumber = w.WeekNumber,
                WeekType = w.Provenance.SourcePhaseKey switch
                {
                    "FOUNDATION" => TrainingWeekType.Base,
                    "BUILD" => TrainingWeekType.Build,
                    "RACE_SPECIFIC" => TrainingWeekType.Peak,
                    "TAPER" => TrainingWeekType.Taper,
                    _ => TrainingWeekType.Build
                },
                Days = w.Sessions
                    .OrderBy(s => s.Date)
                    .Select((s, index) => new PreviewDayDto
                    {
                        SlotIndex = index + 1,
                        DayType = s.WorkoutType switch
                        {
                            GeneratedCatalogWorkoutType.Easy => TrainingDayType.Easy,
                            GeneratedCatalogWorkoutType.Interval => TrainingDayType.Interval,
                            GeneratedCatalogWorkoutType.Tempo => TrainingDayType.Tempo,
                            GeneratedCatalogWorkoutType.LongRun => TrainingDayType.LongRun,
                            GeneratedCatalogWorkoutType.RecoveryEasy => TrainingDayType.RecoveryEasy,
                            _ => TrainingDayType.Easy
                        },
                        DistanceKm = s.TargetDistanceKm ?? s.EstimatedDistanceKm ?? 0,
                        DurationMin = s.TargetDurationMinutes ?? s.EstimatedDurationMinutes ?? 0,
                        Intensity = s.PlannedIntensity ?? s.PacePrescription.EffortLabel ?? string.Empty,
                        Date = s.Date.ToDateTime(TimeOnly.MinValue),
                    })
                    .ToList()
            })
            .ToList();
    }

    public async Task<ConfirmPlanResponse> ConfirmPlanAsync(Guid internalUserId, ConfirmPlanRequest request, CancellationToken ct = default)
    {
        // ── Backend Integration Phase 4E.2: catalog dispatch via stored GenerationSource ──
        // Load preview first (without user filter yet, so we can peek at
        // GenerationSource before deciding the dispatch path). This does NOT
        // re-run IGenerationRouteDecider — dispatch is based solely on what
        // was stored in the preview row at generation time.
        var previewForDispatch = await _context.PlanPreviews
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PreviewId, ct);

        if (previewForDispatch != null && IsCatalogSourcedPreview(previewForDispatch.PreviewPayloadJson))
        {
            // Catalog path: delegate ALL validation, integrity, idempotency,
            // and persistence to CatalogPlanConfirmationService. Nothing below
            // this branch is reached for a catalog-sourced preview.
            // IGenerationRouteDecider is NOT called.
            _logger.LogInformation(
                "ConfirmPlan: preview {PreviewId} is CATALOG-sourced — delegating to CatalogPlanConfirmationService.",
                request.PreviewId);
            return await _catalogConfirmationService.ConfirmAsync(internalUserId, request.PreviewId, ct);
        }

        // ── Legacy SQL confirm path — entirely unchanged from pre-Phase 4E.1 ──
        // Only reached for GenerationSource=LEGACY_SQL previews (or previews
        // that pre-date the GenerationSource field). The catalog dispatch above
        // is the first and only place GenerationSource is consulted.

        // 1. Fetch preview (read-only: confirm never mutates the preview row)
        var preview = await _context.PlanPreviews
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PreviewId && p.InternalUserId == internalUserId, ct);

        if (preview == null)
        {
            throw new NotFoundAppException("Plan preview not found.");
        }

        if (preview.ExpiresAt < DateTime.UtcNow)
        {
            throw new ConflictAppException("Plan preview has expired. Please generate a new preview.");
        }

        // 2. Enforce single active plan rule: confirm never replaces an
        // existing active plan. The caller must cancel it explicitly first
        // (POST /plans/{planId}/cancel). Return the existing plan's info
        // instead of erroring, so a stray confirm is a safe no-op.
        var existingActivePlan = await _context.TrainingPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.InternalUserId == internalUserId && p.Status == TrainingPlanStatus.Active, ct);

        if (existingActivePlan != null)
        {
            return new ConfirmPlanResponse
            {
                PlanId = existingActivePlan.Id,
                Status = "active",
                AlreadyActive = true
            };
        }

        // 3. Deserialize preview payload
        var previewData = JsonSerializer.Deserialize<GeneratePreviewResponse>(preview.PreviewPayloadJson, SerializerOptions);
        var requestData = JsonSerializer.Deserialize<GeneratePreviewRequest>(preview.RequestPayloadJson, SerializerOptions);

        if (previewData == null || requestData == null)
        {
            throw new InvalidOperationException("Failed to load plan preview data.");
        }

        // Defensive guard: a catalog-sourced preview that somehow slipped
        // through the dispatch above would leave Weeks empty and crash below.
        // This should never occur in practice (the dispatch above handles all
        // catalog previews), but if it does, throw a typed error rather than
        // crashing with an unhandled InvalidOperationException.
        if (previewData.Weeks.Count == 0)
        {
            throw new InvalidOperationException(
                $"Plan preview '{request.PreviewId}' has no week data. This may indicate a dispatch error — " +
                "catalog-sourced previews should have been handled by the catalog confirm path.");
        }

        // 4. Create active TrainingPlan.
        // UserProfile is guaranteed to exist at this point because
        // FirebaseAuthMiddleware → UserSynchronizationService runs on every
        // authenticated request before the controller is reached.
        var totalWeeks = previewData.Weeks.Count;
        var planStartDate = previewData.Weeks.First().Days.OrderBy(d => d.Date)
            .First().Date;
        var planEndDate = planStartDate.AddDays(totalWeeks * 7);

        // ── Goal-type-aware field mapping ────────────────────────────────────
        // Race plans: CustomGoalType and HabitPlanType must be null.
        // Habit plans: CustomGoalType is only set for the "custom" habit branch;
        //              normalize empty/whitespace to null.
        // requestData was persisted only after GeneratePreviewRequestValidator
        // already passed on it (GeneratePreviewAsync validates before saving
        // the PlanPreview row), so mapping it through the same command mapper
        // here is safe and replaces manual isRace-ternary null handling with
        // a pattern match against already-guaranteed-non-null command fields.
        var isRace = previewData.GoalType == GoalType.Race;
        var command = GeneratePreviewCommandMapper.ToCommand(requestData);

        string? customGoalType = null;
        string? habitPlanType = null;
        string? raceName = null;
        DateOnly? raceDate = null;
        int? targetFinishTimeSeconds = null;
        Weekday? longRunDayValue = null;

        if (command is RacePlanPreviewCommand raceCommand)
        {
            raceName = raceCommand.RaceName;
            raceDate = raceCommand.RaceDate;
            targetFinishTimeSeconds = raceCommand.TargetFinishTimeSeconds;
            longRunDayValue = raceCommand.LongRunDay;
            // CustomGoalType and HabitPlanType stay null for race plans.
        }
        else if (command is HabitPlanPreviewCommand habitCommand)
        {
            longRunDayValue = habitCommand.LongRunDay;
            habitPlanType = NullIfEmpty(requestData.HabitPlanType);

            // CustomGoalType should only be populated when the habit flow explicitly selected a custom habit goal.
            if (string.Equals(habitPlanType, "custom", StringComparison.OrdinalIgnoreCase))
            {
                customGoalType = MapCustomGoalType(requestData.CustomGoalType);
            }

            // Validate CustomGoalType against the allowed CHECK constraint values.
            var validCustomGoalTypes = new HashSet<string> { "comfort", "steady_pace", "finish_under_time" };
            if (customGoalType != null && !validCustomGoalTypes.Contains(customGoalType))
            {
                throw new ArgumentException(
                    $"CustomGoalType must be one of: {string.Join(", ", validCustomGoalTypes)}, but was '{requestData.CustomGoalType}'.");
            }
        }

        var longRunDay = WeekdayCsv.ToCsv(longRunDayValue);

        var plan = new TrainingPlan
        {
            Id = Guid.NewGuid(),
            InternalUserId = internalUserId,
            TemplateId = preview.TemplateId,
            Status = TrainingPlanStatus.Active,
            GoalType = previewData.GoalType,
            GoalDistance = previewData.GoalDistance,
            GoalDistanceKm = GetGoalDistanceInKm(previewData.GoalDistance),
            Level = previewData.Level,
            DaysPerWeek = previewData.DaysPerWeek,
            Unit = previewData.Unit,
            RaceName = raceName,
            RaceDate = raceDate,
            TargetFinishTimeSeconds = targetFinishTimeSeconds,
            // Snapshot onboarding answers — frozen at confirm time so template
            // changes never alter historical plan data.
            PreferredDays      = RunningDay.NormalizeList(WeekdayCsv.ToCsv(requestData.PreferredDays)),
            WeeklyAvailability = requestData.WeeklyAvailability,
            PreferredPace      = requestData.PreferredPace,
            LongRunDay              = longRunDay,
            HabitPlanType           = habitPlanType,
            CustomGoalType          = customGoalType,
            CustomDurationWeeks     = isRace ? null : requestData.CustomDurationWeeks,
            CustomTargetTimeSeconds = isRace ? null : requestData.CustomTargetTimeSeconds,
            StartedAt = planStartDate,
            EstimatedEndDate = planEndDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TrainingPlans.Add(plan);

        // 6. Map preview weeks and days to database entities
        foreach (var previewWeek in previewData.Weeks)
        {
            var plannedVol = previewWeek.Days.Sum(d => d.DistanceKm);
            var weekEntity = new TrainingWeek
            {
                Id = Guid.NewGuid(),
                PlanId = plan.Id,
                WeekNumber = previewWeek.WeekNumber,
                WeekType = previewWeek.WeekType,
                PlannedVolumeKm = plannedVol,
                ActualVolumeKm = 0,
                IsRecoveryWeek = previewWeek.WeekType == TrainingWeekType.Recovery,
                StartDate = previewWeek.Days.First().Date,
                CreatedAt = DateTime.UtcNow,
                Plan = plan
            };

            _context.TrainingWeeks.Add(weekEntity);

            foreach (var previewDay in previewWeek.Days)
            {
                var dayEntity = new TrainingDay
                {
                    Id = Guid.NewGuid(),
                    PlanId = plan.Id,
                    WeekId = weekEntity.Id,
                    Date = previewDay.Date,
                    DayType = previewDay.DayType,
                    Status = TrainingDayStatus.Planned,
                    Title = GetTitleForDay(previewDay.DayType, previewDay.DistanceKm),
                    Description = GetDescriptionForDay(previewDay.DayType, previewDay.DistanceKm),
                    PlannedDistanceKm = previewDay.DistanceKm,
                    PlannedDurationMin = previewDay.DurationMin,
                    PlannedPaceMinKm = previewDay.DistanceKm > 0 ? (previewDay.DurationMin / previewDay.DistanceKm) : null,
                    Intensity = previewDay.Intensity,
                    IsLongRun = previewDay.DayType == TrainingDayType.LongRun,
                    OriginalDate = previewDay.Date,
                    OriginalType = previewDay.DayType,
                    CanMarkComplete = true,
                    CanMarkNotToday = true,
                    Source = TrainingDaySource.Template,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Plan = plan,
                    Week = weekEntity
                };

                _context.TrainingDays.Add(dayEntity);
            }
        }

        // Log PlanGenerated event
        var planEvent = new PlanEvent
        {
            Id = Guid.NewGuid(),
            InternalUserId = internalUserId,
            PlanId = plan.Id,
            EventType = "PlanGenerated",
            PayloadJson = JsonSerializer.Serialize(new { templateId = plan.TemplateId, startDate = planStartDate }),
            CreatedAt = DateTime.UtcNow
        };
        _context.PlanEvents.Add(planEvent);

        await _context.SaveChangesAsync(ct);

        return new ConfirmPlanResponse
        {
            PlanId = plan.Id,
            Status = "active"
        };
    }

    public async Task<CancelPlanResponse> CancelPlanAsync(Guid internalUserId, Guid planId, CancelPlanRequest request, CancellationToken ct = default)
    {
        var plan = await _context.TrainingPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.InternalUserId == internalUserId && p.Status == TrainingPlanStatus.Active, ct);

        if (plan == null)
        {
            throw new NotFoundAppException("Active training plan not found.");
        }

        plan.Status = TrainingPlanStatus.Cancelled;
        plan.CancelledAt = DateTime.UtcNow;

        // Phase 4L.6C: structured operational event, distinct from the
        // durable PlanEvent audit row below -- this is the log-platform-
        // queryable signal (Part 15 "Recovery: regenerate/cancel").
        _logger.LogInformation(
            "Plan cancelled. UserId={UserId} PlanId={PlanId} ScheduleStrategy={ScheduleStrategy}",
            internalUserId, plan.Id, plan.ScheduleStrategy);

        // Log PlanCancelled event
        var planEvent = new PlanEvent
        {
            Id = Guid.NewGuid(),
            InternalUserId = internalUserId,
            PlanId = plan.Id,
            EventType = "PlanCancelled",
            PayloadJson = JsonSerializer.Serialize(new { reason = request.Reason }),
            CreatedAt = DateTime.UtcNow
        };
        _context.PlanEvents.Add(planEvent);

        await _context.SaveChangesAsync(ct);

        return new CancelPlanResponse
        {
            PlanId = plan.Id,
            Status = "cancelled"
        };
    }

    public async Task<PlanDetailsResponse> GetActivePlanDetailsAsync(Guid internalUserId, CancellationToken ct = default)
    {
        var plan = await _context.TrainingPlans
            .AsNoTracking()
            .Include(p => p.Weeks)
                .ThenInclude(w => w.Days)
            .FirstOrDefaultAsync(p => p.InternalUserId == internalUserId && p.Status == TrainingPlanStatus.Active, ct);

        if (plan == null)
        {
            // No active plan: deterministic empty response (never 500/null)
            // so the client can rely on a stable shape either way.
            return new PlanDetailsResponse
            {
                HasActivePlan = false,
                Status = "none",
                Weeks = new List<PlanWeekDetailDto>()
            };
        }

        if (plan.ScheduleStrategy == PlanScheduleStrategy.RollingLongHorizon)
        {
            if (!plan.LongHorizonRollingPlanStateId.HasValue)
                throw new LongHorizonReadStateCorruptException("Active rolling plan has no rolling-state authority.");
            var aggregate = await _context.LongHorizonRollingPlanStates.AsNoTracking()
                .Include(p => p.Weeks).ThenInclude(w => w.Sessions)
                .SingleOrDefaultAsync(p => p.Id == plan.LongHorizonRollingPlanStateId.Value, ct)
                ?? throw new LongHorizonReadStateCorruptException("Active rolling plan state is unavailable.");
            var activeSessions = aggregate.Weeks
                .Where(w => w.LifecycleState is LongHorizonPersistedLifecycleState.NumericActivated or LongHorizonPersistedLifecycleState.Completed)
                .SelectMany(w => w.Sessions).ToList();
            var completedWeeks = aggregate.Weeks.Count(w => w.LifecycleState == LongHorizonPersistedLifecycleState.Completed);
            return new PlanDetailsResponse
            {
                PlanId = plan.Id, Status = EnumSnakeCase.ToSnakeCase(plan.Status), GoalType = plan.GoalType,
                GoalDistance = plan.GoalDistance, Level = plan.Level, DaysPerWeek = plan.DaysPerWeek, Unit = plan.Unit,
                RaceName = plan.RaceName, RaceDate = plan.RaceDate, TargetFinishTimeSeconds = plan.TargetFinishTimeSeconds,
                StartedAt = plan.StartedAt, EstimatedEndDate = plan.EstimatedEndDate, TotalWeeks = aggregate.TotalWeeks,
                CompletedWeeksCount = completedWeeks, TotalPlannedDistance = activeSessions.Sum(s => s.DistanceKm),
                TotalCompletedDistance = activeSessions.Sum(s => s.ActualDistanceKm ?? 0), LongRunDay = plan.LongRunDay,
                ScheduleStrategy = PlanScheduleStrategy.RollingLongHorizon,
                CurrentWindowStartWeek = aggregate.CurrentWindowStartWeek, CurrentWindowEndWeek = aggregate.CurrentWindowEndWeek,
                NextPendingGlobalWeek = aggregate.Weeks.Where(w => w.LifecycleState == LongHorizonPersistedLifecycleState.NumericPending).OrderBy(w => w.GlobalWeek).Select(w => (int?)w.GlobalWeek).FirstOrDefault(),
                CheckpointReadiness = LongHorizonActiveReadModelProvider.Readiness(aggregate, activeSessions),
                RollingStructuralWeeks = aggregate.Weeks.OrderBy(w => w.GlobalWeek).Select(w => new LongHorizonStructuralWeekDetailDto
                {
                    GlobalWeek = w.GlobalWeek, Phase = EnumSnakeCase.ToSnakeCase(w.SegmentType), Stage = w.Stage,
                    StructuralStartDate = w.StructuralStartDate, StructuralEndDate = w.StructuralEndDate,
                    LifecycleStatus = EnumSnakeCase.ToSnakeCase(w.LifecycleState),
                    NumericDetailsAvailable = w.LifecycleState is LongHorizonPersistedLifecycleState.NumericActivated or LongHorizonPersistedLifecycleState.Completed
                }).ToList(),
                Weeks = []
            };
        }

        var totalWeeks = plan.Weeks.Count;
        var completedWeeksCount = plan.Weeks.Count(w => w.Days.All(d => d.Status == TrainingDayStatus.Completed || d.Status == TrainingDayStatus.Missed || d.Status == TrainingDayStatus.Skipped));
        var totalPlannedDistance = plan.Weeks.Sum(w => w.PlannedVolumeKm);
        var totalCompletedDistance = plan.Weeks.Sum(w => w.ActualVolumeKm);

        var response = new PlanDetailsResponse
        {
            PlanId = plan.Id,
            TemplateId = plan.TemplateId,
            Status = EnumSnakeCase.ToSnakeCase(plan.Status),
            GoalType = plan.GoalType,
            GoalDistance = plan.GoalDistance,
            Level = plan.Level,
            DaysPerWeek = plan.DaysPerWeek,
            Unit = plan.Unit,
            RaceName = plan.RaceName,
            RaceDate = plan.RaceDate,
            TargetFinishTimeSeconds = plan.TargetFinishTimeSeconds,
            StartedAt = plan.StartedAt,
            EstimatedEndDate = plan.EstimatedEndDate,
            TotalWeeks = totalWeeks,
            CompletedWeeksCount = completedWeeksCount,
            TotalPlannedDistance = totalPlannedDistance,
            TotalCompletedDistance = totalCompletedDistance,
            // response = new PlanDetailsResponse bloğuna ekle:
            LongRunDay              = plan.LongRunDay,
            HabitPlanType           = plan.HabitPlanType,
            CustomGoalType          = plan.CustomGoalType,
            CustomDurationWeeks     = plan.CustomDurationWeeks,
            CustomTargetTimeSeconds = plan.CustomTargetTimeSeconds,
            
            Weeks = plan.Weeks.OrderBy(w => w.WeekNumber).Select(w => new PlanWeekDetailDto
            {
                WeekId = w.Id,
                WeekNumber = w.WeekNumber,
                WeekType = w.WeekType,
                PlannedVolumeKm = w.PlannedVolumeKm,
                ActualVolumeKm = w.ActualVolumeKm,
                IsRecoveryWeek = w.IsRecoveryWeek,
                StartDate = w.StartDate,
                Days = w.Days.OrderBy(d => d.Date).Select(d => new PlanDayDetailDto
                {
                    DayId = d.Id,
                    Date = d.Date,
                    DayType = d.DayType,
                    Status = d.Status,
                    Title = d.Title,
                    Description = d.Description,
                    PlannedDistanceKm = d.PlannedDistanceKm,
                    PlannedDurationMin = d.PlannedDurationMin,
                    PlannedPaceMinKm = d.PlannedPaceMinKm,
                    Intensity = d.Intensity,
                    ActualDistanceKm = d.ActualDistanceKm,
                    ActualDurationMin = d.ActualDurationMin,
                    IsLongRun = d.IsLongRun,
                    CanMarkComplete = d.CanMarkComplete,
                    CanMarkNotToday = d.CanMarkNotToday
                }).ToList()
            }).ToList()
        };

        return response;
    }

    private static double GetGoalDistanceInKm(GoalDistance distance) => GoalDistanceKm.Resolve(distance);

    private static string GetTitleForDay(TrainingDayType dayType, double distance)
    {
        return dayType switch
        {
            TrainingDayType.Easy => $"Easy {distance:0.#}k Run",
            TrainingDayType.Interval => $"Interval Session",
            TrainingDayType.Tempo => $"Tempo Run",
            TrainingDayType.LongRun => $"Long Run {distance:0.#}k",
            TrainingDayType.Rest => "Rest Day",
            TrainingDayType.RecoveryEasy => "Recovery Run",
            _ => "Training Session"
        };
    }

    private static string GetDescriptionForDay(TrainingDayType dayType, double distance)
    {
        return dayType switch
        {
            TrainingDayType.Easy => $"Run at a conversational, easy pace for {distance:0.#} km.",
            TrainingDayType.Interval => "Warm up, then run intervals at a fast pace, followed by recovery jogs.",
            TrainingDayType.Tempo => "Run at a steady, comfortably hard tempo pace.",
            TrainingDayType.LongRun => $"Build endurance with a steady, relaxed {distance:0.#} km run.",
            TrainingDayType.Rest => "Give your body time to adapt and get stronger today. No running.",
            TrainingDayType.RecoveryEasy => "A very short, very slow run to help flush out legs.",
            _ => "Follow the target workout parameters."
        };
    }

    private static string? NullIfEmpty(string? val)
    {
        return string.IsNullOrWhiteSpace(val) ? null : val;
    }

    private static string? MapCustomGoalType(string? customGoalType)
    {
        if (string.IsNullOrWhiteSpace(customGoalType)) return null;
        return customGoalType.Trim().ToLower() switch
        {
            "comfort" or "finish" => "comfort",
            "steady_pace" or "steady" => "steady_pace",
            "finish_under_time" or "time" => "finish_under_time",
            _ => customGoalType
        };
    }

    private static string[] ResolvePreferredDays(string? preferredDaysCsv, int daysPerWeek)
    {
        if (!string.IsNullOrWhiteSpace(preferredDaysCsv))
        {
            var parsed = preferredDaysCsv
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(RunningDay.Normalize)
                .Where(d => d != null)
                .Select(d => d!)
                .ToArray();

            if (parsed.Length > 0)
                return parsed;
        }

        // Generate default running days evenly distributed across Mon–Sun.
        var allDays = RunningDay.All.ToArray();
        var clamped = Math.Clamp(daysPerWeek, 1, 7);

        return clamped switch
        {
            1 => new[] { RunningDay.Wednesday },
            2 => new[] { RunningDay.Tuesday, RunningDay.Saturday },
            3 => new[] { RunningDay.Monday, RunningDay.Wednesday, RunningDay.Friday },
            4 => new[] { RunningDay.Monday, RunningDay.Wednesday, RunningDay.Friday, RunningDay.Sunday },
            5 => new[] { RunningDay.Monday, RunningDay.Tuesday, RunningDay.Thursday, RunningDay.Friday, RunningDay.Sunday },
            6 => new[] { RunningDay.Monday, RunningDay.Tuesday, RunningDay.Wednesday, RunningDay.Thursday, RunningDay.Friday, RunningDay.Saturday },
            7 => allDays,
            _ => new[] { RunningDay.Monday, RunningDay.Wednesday, RunningDay.Friday },
        };
    }

    // JSON parsing helper classes
    private class TemplateJsonData
    {
        public string TemplateId { get; set; } = string.Empty;
        public List<TemplateWeek> Weeks { get; set; } = new();
    }

    private class TemplateWeek
    {
        public int WeekNumber { get; set; }
        public TrainingWeekType WeekType { get; set; }
        public List<TemplateDay> Days { get; set; } = new();
    }

    private class TemplateDay
    {
        public int SlotIndex { get; set; }
        public TrainingDayType DayType { get; set; }
        public double DistanceKm { get; set; }
        public int DurationMin { get; set; }
        public string? Intensity { get; set; }
    }

    /// <summary>
    /// Backend Integration Phase 4E.2: lightweight peek at the stored
    /// <c>PreviewPayloadJson</c> to decide dispatch without fully deserializing
    /// the snapshot. Returns <c>true</c> iff the JSON contains a top-level
    /// <c>generation_source</c> (snake_case, as serialized by
    /// <see cref="GenerateCatalogPreviewAsync"/>) field whose value is
    /// <c>"CATALOG"</c> (case-insensitive).
    ///
    /// This does NOT re-run <see cref="IGenerationRouteDecider"/>. It reads
    /// only the already-stored value. A missing or non-CATALOG value means
    /// the legacy SQL confirm path should handle the preview.
    ///
    /// Internally catches JSON exceptions (returns false on malformed JSON —
    /// the catalog confirm service will reject the preview with a proper typed
    /// error if needed, since it only runs after this returns true).
    /// </summary>
    private static bool IsCatalogSourcedPreview(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            // Try both snake_case (catalog shape) and camelCase (legacy shape).
            if (doc.RootElement.TryGetProperty("generation_source", out var gs) ||
                doc.RootElement.TryGetProperty("generationSource", out gs))
            {
                return gs.ValueKind == JsonValueKind.String &&
                       string.Equals(gs.GetString(), RunningApp.Application.RuntimeCatalog.PreviewRouting.GenerationSource.Catalog,
                           StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
