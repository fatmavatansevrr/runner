using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using RunningApp.Application.Commands.Plan;
using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Application.Services;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview;

internal sealed record LongHorizonServerPreviewSnapshot
{
    public int SnapshotContractVersion { get; init; } = 1;
    public required Guid PlanStateId { get; init; }
    public required LongHorizonStructuralRoadmap StructuralRoadmap { get; init; }
    public required DateOnly PlanStartDate { get; init; }
    public required IReadOnlyList<DayOfWeek> PreferredDays { get; init; }
    public required DayOfWeek LongRunDay { get; init; }
    public required RollingNumericActivationWindow InitialWindow { get; init; }
    public required IReadOnlyDictionary<int, LongHorizonNumericLifecycleState> LifecycleStates { get; init; }
    public required IReadOnlyDictionary<int, ActivatedNumericWeek> ActivatedWeeks { get; init; }
    public required LongHorizonContextVersion ContextVersion { get; init; }
    public required string CatalogRootPath { get; init; }
    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }
    public required RacePlanPreviewCommand Command { get; init; }
    public required LongHorizonPlanPreviewContract PublicPreview { get; init; }
}

internal enum LongHorizonConfirmationFailpoint
{
    None,
    AfterRollingInitialization,
    AfterPlanOwnership,
    AfterPreviewStatusUpdate,
    BeforeCommit,
    AfterCommitBeforeAcknowledgement,
}

internal interface ILongHorizonConfirmationFailureInjector
{
    void MaybeThrow(LongHorizonConfirmationFailpoint point);
}

internal sealed class NoOpLongHorizonConfirmationFailureInjector : ILongHorizonConfirmationFailureInjector
{
    public static readonly NoOpLongHorizonConfirmationFailureInjector Instance = new();
    public void MaybeThrow(LongHorizonConfirmationFailpoint point) { }
}

internal sealed class LongHorizonInjectedConfirmationFailureException(LongHorizonConfirmationFailpoint point)
    : Exception($"Injected Long-Horizon confirmation failure at {point}.");

internal sealed class LongHorizonTestConfirmationFailureInjector(LongHorizonConfirmationFailpoint point)
    : ILongHorizonConfirmationFailureInjector
{
    public void MaybeThrow(LongHorizonConfirmationFailpoint current)
    {
        if (current == point) throw new LongHorizonInjectedConfirmationFailureException(current);
    }
}

public sealed class LongHorizonPublicPlanService : ILongHorizonPublicPlanService
{
    internal const int PublicContractVersion = 1;
    internal const int ConfirmationContractVersion = 1;
    internal const int PersistenceContractVersion = 1;
    internal static readonly TimeSpan PreviewLifetime = TimeSpan.FromMinutes(30);
    private const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
    private const int CandidateVersion = 10;

    private static readonly JsonSerializerOptions SnapshotJson = new(JsonSerializerDefaults.Web)
    {
        IncludeFields = true,
    };

    private readonly AppDbContext _db;
    private readonly ICatalogCandidateEligibilityGate _candidateGate;
    private readonly PlanCatalogOptions _catalogOptions;
    private readonly LongHorizonGenerationOptions _generationOptions;
    private readonly ILogger<LongHorizonPublicPlanService> _logger;
    private readonly ILongHorizonConfirmationFailureInjector _failureInjector;

    public LongHorizonPublicPlanService(
        AppDbContext db,
        ICatalogCandidateEligibilityGate candidateGate,
        IOptions<PlanCatalogOptions> catalogOptions,
        IOptions<LongHorizonGenerationOptions> generationOptions,
        ILogger<LongHorizonPublicPlanService> logger)
        : this(db, candidateGate, catalogOptions, generationOptions, logger, NoOpLongHorizonConfirmationFailureInjector.Instance)
    {
    }

    internal LongHorizonPublicPlanService(
        AppDbContext db,
        ICatalogCandidateEligibilityGate candidateGate,
        IOptions<PlanCatalogOptions> catalogOptions,
        IOptions<LongHorizonGenerationOptions> generationOptions,
        ILogger<LongHorizonPublicPlanService> logger,
        ILongHorizonConfirmationFailureInjector failureInjector)
    {
        _db = db;
        _candidateGate = candidateGate;
        _catalogOptions = catalogOptions.Value;
        _generationOptions = generationOptions.Value;
        _logger = logger;
        _failureInjector = failureInjector;
    }

    public async Task<LongHorizonPlanPreviewContract> GeneratePreviewAsync(
        Guid internalUserId, RacePlanPreviewCommand command, CancellationToken ct = default)
    {
        if (!_generationOptions.GenerationEnabled)
        {
            _logger.LogInformation(
                "Long-Horizon generation rejected: GenerationEnabled=false. User={UserId}", internalUserId);
            throw new LongHorizonGenerationTemporarilyDisabledException(
                "New Long-Horizon plan generation is temporarily unavailable. Please try again shortly.");
        }

        ValidatePilot(command);
        var horizon = RaceHorizonPolicy.Decide(command.StartDate, command.RaceDate);
        var composition = LongHorizonCompositionResolver.Resolve(horizon, ResolveProfile(command));
        if (composition.Eligibility == LongHorizonEligibility.UnsupportedAboveAnnualWindow)
        {
            _logger.LogInformation(
                "Long-Horizon preview rejected: horizon exceeds supported window. User={UserId} Horizon={Horizon}",
                internalUserId, horizon.AvailableFullWeeks);
            throw new LongHorizonPlanHorizonExceededException("Long-Horizon plans support at most 52 complete weeks.");
        }
        if (composition.Eligibility != LongHorizonEligibility.SupportedLongHorizon)
        {
            _logger.LogInformation(
                "Long-Horizon preview rejected: horizon below supported dedicated window. User={UserId} Horizon={Horizon}",
                internalUserId, horizon.AvailableFullWeeks);
            throw new LongHorizonPilotUnsupportedException("The dedicated Long-Horizon endpoint supports exactly 21-52 complete weeks.");
        }

        _logger.LogInformation("Long-Horizon preview requested. User={UserId} Horizon={Horizon}", internalUserId, horizon.AvailableFullWeeks);
        var candidate = await _candidateGate.LoadForPublicPreviewAsync(CandidateKey, CandidateVersion, ct);
        var preferredDays = command.PreferredDays.Select(ToDayOfWeek).ToList();
        var longRunDay = ToDayOfWeek(command.LongRunDay);
        var workoutLoader = new CatalogWorkoutDefinitionLoader(Options.Create(_catalogOptions));
        var initial = await new LongHorizonRollingInitialActivationRuntime().BuildInitialActivationAsync(new LongHorizonRollingInitialActivationRequest
        {
            CompositionDecision = composition,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            StartDate = command.StartDate,
            RaceDate = command.RaceDate,
            OnboardingBaseline = new LongHorizonGeEntryBaselineInput(command.RecentWeeklyVolumeKm, command.RecentLongestRunKm, command.RecentRunsPerWeek),
            PreferredDays = preferredDays,
            LongRunDay = longRunDay,
            CatalogRoot = _catalogOptions.CatalogRootPath,
            WorkoutLoader = workoutLoader,
        }, ct);
        if (initial.Status != LongHorizonRollingInitialActivationStatus.Approved)
        {
            _logger.LogWarning("Long-Horizon preview blocked. User={UserId} Horizon={Horizon} Outcome=initialization_infeasible", internalUserId, horizon.AvailableFullWeeks);
            throw new LongHorizonInitializationInfeasibleException("A safe initial executable window could not be generated; update recent training evidence and regenerate the preview.");
        }

        var previewId = Guid.NewGuid();
        var generatedAt = DateTime.UtcNow;
        var expiresAt = generatedAt.Add(PreviewLifetime);
        var lifecycle = initial.ActivatedNumericWeeks.Concat(initial.PendingNumericWeeks)
            .ToDictionary(w => w.GlobalWeekNumber, w => w.LifecycleState);
        var activated = initial.ActivatedNumericWeeks.ToDictionary(w => w.GlobalWeekNumber);
        var state = new LongHorizonFullDarkLifecycleState
        {
            StructuralRoadmap = initial.StructuralRoadmap!,
            StructuralSkeleton = initial.StructuralSkeleton!,
            LifecycleStates = lifecycle,
            ActivatedWeeks = activated,
            CurrentWindow = initial.ActivationWindow!,
            ContextVersion = initial.ContextVersion!,
            CheckpointDecisions = [], CoreContextIds = [], AuditEvents = [],
            ActivationInvocationCount = 1, FullRunwayMaterializationCount = 0,
            LastCheckpointDate = command.StartDate,
        };
        var publicPreview = LongHorizonPublicPreviewMapper.Map(new LongHorizonPublicPreviewMapperInput
        {
            PreviewId = previewId,
            GoalType = "Race",
            GoalDistance = "TenK",
            State = state,
            StartDate = command.StartDate,
            EstimatedEndDate = command.RaceDate,
            DaysPerWeek = 4,
            PreferredDays = preferredDays,
            LongRunDay = longRunDay,
            ProvenanceSummary = LongHorizonPublicProvenance.GeneratedFromInitialProfile,
        }) with
        {
            GeneratedAtUtc = generatedAt,
            ExpiresAtUtc = expiresAt,
            ConfirmationReadiness = LongHorizonConfirmationReadiness.ReadyForRollingPersistence,
        };
        LongHorizonPublicPreviewContractValidator.Validate(publicPreview);

        var snapshot = new LongHorizonServerPreviewSnapshot
        {
            PlanStateId = previewId,
            StructuralRoadmap = initial.StructuralRoadmap!, PlanStartDate = command.StartDate,
            PreferredDays = preferredDays, LongRunDay = longRunDay,
            InitialWindow = initial.ActivationWindow!, LifecycleStates = lifecycle, ActivatedWeeks = activated,
            ContextVersion = initial.ContextVersion!, CatalogRootPath = _catalogOptions.CatalogRootPath,
            CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            Command = command, PublicPreview = publicPreview,
        };
        var requestJson = JsonSerializer.Serialize(command, SnapshotJson);
        var publicJson = JsonSerializer.Serialize(publicPreview, SnapshotJson);
        var snapshotJson = JsonSerializer.Serialize(snapshot, SnapshotJson);
        _db.PlanPreviews.Add(new PlanPreview
        {
            Id = previewId, InternalUserId = internalUserId, TemplateId = CandidateKey,
            RequestPayloadJson = requestJson, PreviewPayloadJson = publicJson,
            LongHorizonInitializationSnapshotJson = snapshotJson,
            ScheduleStrategy = PlanScheduleStrategy.RollingLongHorizon.ToString(),
            NormalizedInputFingerprint = Hash(requestJson),
            StructuralRoadmapFingerprint = Hash(JsonSerializer.Serialize(initial.StructuralRoadmap, SnapshotJson)),
            ExecutableWindowFingerprint = Hash(JsonSerializer.Serialize(initial.ActivationWindow, SnapshotJson)),
            LongHorizonPlanStateId = previewId,
            PublicContractVersion = PublicContractVersion,
            ConfirmationContractVersion = ConfirmationContractVersion,
            RollingPersistenceContractVersion = PersistenceContractVersion,
            ExpiresAt = expiresAt, CreatedAt = generatedAt,
        });
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Long-Horizon preview generated. User={UserId} Preview={PreviewId} Horizon={Horizon}", internalUserId, previewId, horizon.AvailableFullWeeks);
        return publicPreview;
    }

    public async Task<LongHorizonConfirmPlanResponse> ConfirmAsync(
        Guid internalUserId, LongHorizonConfirmPlanRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Long-Horizon confirmation requested. User={UserId} Preview={PreviewId}", internalUserId, request.PreviewId);
        if (request.ContractVersion != ConfirmationContractVersion)
            throw new LongHorizonPreviewStaleException("The confirmation contract version is unsupported; generate a new preview.");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var preview = await _db.PlanPreviews.SingleOrDefaultAsync(p => p.Id == request.PreviewId && p.InternalUserId == internalUserId, ct)
                ?? throw new LongHorizonPreviewNotFoundException("Long-Horizon preview was not found.");
            if (preview.ConfirmedPlanId is { } confirmedId)
            {
                _logger.LogInformation("Long-Horizon confirmation replayed. User={UserId} Preview={PreviewId} Plan={PlanId}", internalUserId, preview.Id, confirmedId);
                return await ExistingResponseAsync(preview, confirmedId, ct);
            }
            ValidatePreviewAuthority(preview);
            if (preview.ExpiresAt <= DateTime.UtcNow) throw new LongHorizonPreviewExpiredException("Long-Horizon preview expired; generate a new preview.");
            if (await _db.TrainingPlans.AnyAsync(p => p.InternalUserId == internalUserId && p.Status == TrainingPlanStatus.Active, ct))
                throw new LongHorizonActivePlanConflictException("An active plan already exists.");

            var snapshot = DeserializeSnapshot(preview);
            var candidate = await _candidateGate.LoadForPublicPreviewAsync(snapshot.CandidateKey, snapshot.CandidateVersion, ct);
            ValidateFingerprints(preview, snapshot);
            var repository = new LongHorizonRollingStateRepository(_db);
            await repository.InitializeStructuralStateAsync(new LongHorizonRollingInitializationRequest
            {
                PlanStateId = snapshot.PlanStateId,
                StructuralRoadmap = snapshot.StructuralRoadmap,
                PlanStartDate = snapshot.PlanStartDate,
                PreferredDays = snapshot.PreferredDays,
                LongRunDay = snapshot.LongRunDay,
                InitialWindow = snapshot.InitialWindow,
                LifecycleStates = snapshot.LifecycleStates,
                ActivatedWeeks = snapshot.ActivatedWeeks,
                ContextVersion = snapshot.ContextVersion,
                CatalogRootPath = snapshot.CatalogRootPath,
                Candidate = candidate,
            }, ct);
            _failureInjector.MaybeThrow(LongHorizonConfirmationFailpoint.AfterRollingInitialization);

            var now = DateTime.UtcNow;
            var plan = BuildTrainingPlan(internalUserId, preview.Id, snapshot, now);
            _db.TrainingPlans.Add(plan);
            await _db.SaveChangesAsync(ct);
            _failureInjector.MaybeThrow(LongHorizonConfirmationFailpoint.AfterPlanOwnership);
            preview.ConfirmedPlanId = plan.Id;
            await _db.SaveChangesAsync(ct);
            _failureInjector.MaybeThrow(LongHorizonConfirmationFailpoint.AfterPreviewStatusUpdate);
            _failureInjector.MaybeThrow(LongHorizonConfirmationFailpoint.BeforeCommit);
            await transaction.CommitAsync(ct);
            _failureInjector.MaybeThrow(LongHorizonConfirmationFailpoint.AfterCommitBeforeAcknowledgement);
            _logger.LogInformation("Long-Horizon confirmation succeeded. User={UserId} Preview={PreviewId} Plan={PlanId}", internalUserId, preview.Id, plan.Id);
            return Response(snapshot, plan.Id, preview.Id, LongHorizonConfirmationOutcome.Confirmed, now);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(ct);
            _db.ChangeTracker.Clear();
            var preview = await _db.PlanPreviews.AsNoTracking().SingleOrDefaultAsync(p => p.Id == request.PreviewId && p.InternalUserId == internalUserId, ct);
            if (preview?.ConfirmedPlanId is { } confirmedId) return await ExistingResponseAsync(preview, confirmedId, ct);
            _logger.LogWarning("Long-Horizon confirmation conflict. User={UserId} Preview={PreviewId} Outcome=active_plan_race_lost", internalUserId, request.PreviewId);
            throw new LongHorizonActivePlanConflictException("Another confirmation won the active-plan ownership race.");
        }
    }

    private static TrainingPlan BuildTrainingPlan(Guid userId, Guid previewId, LongHorizonServerPreviewSnapshot snapshot, DateTime now) => new()
    {
        Id = Guid.NewGuid(), InternalUserId = userId, Status = TrainingPlanStatus.Active,
        GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, GoalDistanceKm = 10,
        Level = RunningBackground.Intermediate, DaysPerWeek = 4, Unit = snapshot.Command.Unit,
        RaceName = snapshot.Command.RaceName, RaceDate = snapshot.Command.RaceDate,
        TargetFinishTimeSeconds = snapshot.Command.TargetFinishTimeSeconds,
        StartedAt = DateTime.SpecifyKind(snapshot.Command.StartDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
        EstimatedEndDate = DateTime.SpecifyKind(snapshot.Command.RaceDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
        CreatedAt = now, UpdatedAt = now, CatalogConfirmedAtUtc = now,
        PreferredDays = JsonSerializer.Serialize(snapshot.Command.PreferredDays, SnapshotJson),
        LongRunDay = snapshot.Command.LongRunDay.ToString(),
        CatalogCandidateKey = snapshot.CandidateKey, CatalogCandidateVersion = snapshot.CandidateVersion,
        GenerationSource = "LONG_HORIZON_ROLLING_PUBLIC_V1", SourcePreviewId = previewId,
        ScheduleStrategy = PlanScheduleStrategy.RollingLongHorizon,
        LongHorizonRollingPlanStateId = snapshot.PlanStateId,
    };

    private async Task<LongHorizonConfirmPlanResponse> ExistingResponseAsync(PlanPreview preview, Guid planId, CancellationToken ct)
    {
        var plan = await _db.TrainingPlans.AsNoTracking().SingleOrDefaultAsync(p => p.Id == planId && p.InternalUserId == preview.InternalUserId, ct)
            ?? throw new LongHorizonPreviewCorruptException("Confirmed preview references an unavailable plan.");
        var snapshot = DeserializeSnapshot(preview);
        return Response(snapshot, plan.Id, preview.Id, LongHorizonConfirmationOutcome.AlreadyConfirmed, plan.CatalogConfirmedAtUtc ?? plan.CreatedAt);
    }

    private static LongHorizonConfirmPlanResponse Response(LongHorizonServerPreviewSnapshot snapshot, Guid planId, Guid previewId, LongHorizonConfirmationOutcome outcome, DateTime confirmedAt) => new()
    {
        PlanId = planId, PreviewId = previewId, Outcome = outcome,
        TotalWeeks = snapshot.StructuralRoadmap.TotalWeeks,
        CurrentExecutableWeeks = snapshot.PublicPreview.CurrentExecutableWeeks,
        NextPendingGlobalWeek = snapshot.PublicPreview.StructuralRoadmap.Where(w => w.LifecycleStatus == LongHorizonPublicLifecycleStatus.Pending).Select(w => (int?)w.GlobalWeek).FirstOrDefault(),
        PlanStatus = "active", PublicMessage = outcome == LongHorizonConfirmationOutcome.Confirmed ? "long_horizon.confirmed" : "long_horizon.already_confirmed",
        ConfirmedAtUtc = confirmedAt,
    };

    private static void ValidatePilot(RacePlanPreviewCommand command)
    {
        if (command.GoalType != GoalType.Race || command.GoalDistance != GoalDistance.TenK ||
            command.Level != RunningBackground.Intermediate || command.DaysPerWeek != 4)
            throw new LongHorizonPilotUnsupportedException("Only Race/TenK/Intermediate/4-day requests are enabled for Long-Horizon preview.");
    }

    private static ReadinessProfile ResolveProfile(RacePlanPreviewCommand command)
    {
        var result = new CoreEntryReadinessResolver().Resolve(new RuntimeResolverContext
        {
            InputSnapshot = new ResolverInputSnapshot
            {
                GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
                RecentWeeklyVolumeKm = command.RecentWeeklyVolumeKm,
                RecentLongestRunKm = command.RecentLongestRunKm,
                RecentRunsPerWeek = command.RecentRunsPerWeek,
            },
        });
        return result.OutputValue == "READY" ? ReadinessProfile.CoreEntryReady : ReadinessProfile.ConsistencyNeeded;
    }

    private static void ValidatePreviewAuthority(PlanPreview preview)
    {
        if (preview.IsInvalidated == true) throw new LongHorizonPreviewStaleException("Preview was invalidated; generate a new preview.");
        if (preview.ScheduleStrategy != PlanScheduleStrategy.RollingLongHorizon.ToString() ||
            preview.PublicContractVersion != PublicContractVersion || preview.ConfirmationContractVersion != ConfirmationContractVersion ||
            preview.RollingPersistenceContractVersion != PersistenceContractVersion || preview.LongHorizonPlanStateId != preview.Id)
            throw new LongHorizonPreviewStaleException("Preview authority version is unsupported; generate a new preview.");
    }

    private static LongHorizonServerPreviewSnapshot DeserializeSnapshot(PlanPreview preview)
    {
        try
        {
            var snapshot = JsonSerializer.Deserialize<LongHorizonServerPreviewSnapshot>(preview.LongHorizonInitializationSnapshotJson ?? "", SnapshotJson);
            return snapshot is { SnapshotContractVersion: 1 } ? snapshot : throw new JsonException("Unsupported snapshot.");
        }
        catch (JsonException exception) { throw new LongHorizonPreviewCorruptException("Server-owned preview snapshot is invalid.", exception); }
    }

    private static void ValidateFingerprints(PlanPreview preview, LongHorizonServerPreviewSnapshot snapshot)
    {
        var input = Hash(JsonSerializer.Serialize(snapshot.Command, SnapshotJson));
        var roadmap = Hash(JsonSerializer.Serialize(snapshot.StructuralRoadmap, SnapshotJson));
        var window = Hash(JsonSerializer.Serialize(snapshot.InitialWindow, SnapshotJson));
        if (preview.NormalizedInputFingerprint != input || preview.StructuralRoadmapFingerprint != roadmap || preview.ExecutableWindowFingerprint != window)
            throw new LongHorizonPreviewCorruptException("Server-owned preview fingerprint verification failed.");
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static DayOfWeek ToDayOfWeek(Weekday day) => day switch
    {
        Weekday.Mon => DayOfWeek.Monday, Weekday.Tue => DayOfWeek.Tuesday, Weekday.Wed => DayOfWeek.Wednesday,
        Weekday.Thu => DayOfWeek.Thursday, Weekday.Fri => DayOfWeek.Friday, Weekday.Sat => DayOfWeek.Saturday,
        Weekday.Sun => DayOfWeek.Sunday, _ => throw new ArgumentOutOfRangeException(nameof(day)),
    };
}
