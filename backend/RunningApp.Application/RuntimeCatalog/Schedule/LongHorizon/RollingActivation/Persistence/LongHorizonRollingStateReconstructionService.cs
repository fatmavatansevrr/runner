using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2 Part 20 -- database records to validated
/// <see cref="LongHorizonRollingRestartSnapshot"/>. Never regenerates
/// numeric/calendar plans: the only regenerated value is the structural
/// skeleton (workout ROLE labels only, e.g. "KEY_SESSION"/"EASY_SUPPORT" --
/// no distance/pace/date), which is a pure, evidence-independent function
/// of (TotalWeeks, ReadinessProfile, CandidateKey/Version) -- the same
/// three facts already durably stored on the plan aggregate -- via the
/// exact same unmodified <see cref="LongHorizonStructuralMaterializer"/>
/// Phase 4K.6's own initial activation calls. Reconstruction validates and
/// assembles persisted authority; it does not invent numeric/session state.
/// </summary>
internal static class LongHorizonRollingStateReconstructionService
{
    public static async Task<LongHorizonRollingRestartSnapshot?> ReconstructAsync(
        AppDbContext db, Guid planStateId, string catalogRootPath, PlanCatalogCandidateSummary candidate, CancellationToken cancellationToken)
    {
        var plan = await db.LongHorizonRollingPlanStates.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planStateId, cancellationToken);
        if (plan is null) return null;

        var xmin = await db.LongHorizonRollingPlanStates
            .Where(p => p.Id == planStateId)
            .Select(p => EF.Property<uint>(p, "xmin"))
            .SingleAsync(cancellationToken);

        var weekRows = await db.LongHorizonRollingWeekStates.AsNoTracking()
            .Where(w => w.PlanStateId == planStateId)
            .Include(w => w.Sessions)
            .OrderBy(w => w.GlobalWeek)
            .ToListAsync(cancellationToken);

        LongHorizonRollingPersistenceIntegrityValidator.ValidateWeekRows(plan, weekRows);

        var profile = Enum.Parse<ReadinessProfile>(plan.ReadinessProfile);
        var compositionDecision = LongHorizonCompositionResolver.Resolve(
            RaceHorizonPolicy.Decide(plan.StartDate, plan.RaceDate), profile);

        var workoutLoader = new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = catalogRootPath }));
        // Phase 10K-FREQ.6D.18 -- MaterializeAsync's daysPerWeek defaults to 4;
        // omitting plan.DaysPerWeek here silently rebuilt a 4D-shaped structural
        // skeleton (StructuralWorkoutRoles) on every reload of a real 5D plan,
        // even though its persisted sessions were genuinely 5D-shaped -- a
        // reconstruction-only mismatch invisible to every prior FREQ.6D.13/14/15
        // test because none of them read StructuralWorkoutRoles.Count after reload.
        var skeleton = await LongHorizonStructuralMaterializer.MaterializeAsync(compositionDecision, catalogRootPath, workoutLoader, cancellationToken, plan.DaysPerWeek);
        var rolesByWeek = skeleton.Weeks.ToDictionary(w => w.GlobalWeekNumber, w => (IReadOnlyList<string>)w.OrderedWorkoutSlots.Select(s => s.StructuralRole).ToList());

        var lifecycleStates = new Dictionary<int, LongHorizonNumericLifecycleState>();
        var activatedWeeks = new Dictionary<int, ActivatedNumericWeek>();
        var structuralWeeks = new List<LongHorizonStructuralWeek>();

        foreach (var row in weekRows)
        {
            var lifecycleState = MapLifecycleBack(row.LifecycleState);
            lifecycleStates[row.GlobalWeek] = lifecycleState;

            structuralWeeks.Add(new LongHorizonStructuralWeek
            {
                GlobalWeekNumber = row.GlobalWeek,
                SegmentType = MapSegmentBack(row.SegmentType),
                Stage = row.Stage,
                StructuralWorkoutRoles = rolesByWeek.GetValueOrDefault(row.GlobalWeek, []),
                CalendarRange = (row.StructuralStartDate, row.StructuralEndDate),
                NumericLifecycleState = lifecycleState,
            });

            if (row.WeeklyVolumeKm is not null && row.LongRunKm is not null)
            {
                var sessions = row.Sessions.OrderBy(s => s.SessionOrdinal).Select(s => new LongHorizonSessionPrescriptionReference
                {
                    SessionOrdinal = s.SessionOrdinal,
                    SessionRole = s.SessionRole,
                    DistanceKm = s.DistanceKm,
                    WorkoutKey = s.WorkoutKey,
                    WorkoutVersion = s.WorkoutVersion,
                    AssignedDate = s.AssignedDate,
                    Source = s.Provenance,
                    // Phase 10K-FREQ.6D.15 -- these five FREQ.6D.13 lineage columns were already
                    // written on persist but never read back here on reconstruction; a real
                    // Postgres reload proved the gap (raw-row queries saw them, this projection
                    // did not). Restoring them, not inventing new values.
                    LaneOrdinal = s.LaneOrdinal,
                    SlotOrdinal = s.SlotOrdinal,
                    ProgressionStageKey = s.ProgressionStageKey,
                    ProfileKey = s.CatalogPrescriptionProfileKey,
                    ProfileVersion = s.CatalogPrescriptionProfileVersion,
                }).ToList();

                activatedWeeks[row.GlobalWeek] = new ActivatedNumericWeek
                {
                    GlobalWeekNumber = row.GlobalWeek,
                    SegmentType = MapSegmentBack(row.SegmentType),
                    LifecycleState = lifecycleState,
                    TotalWeeklyVolumeKm = row.WeeklyVolumeKm,
                    LongRunKm = row.LongRunKm,
                    SessionPrescriptions = sessions,
                    CalendarDates = (row.StructuralStartDate, row.StructuralEndDate),
                    ContextVersion = row.ActivationContextVersionSequence is { } seq
                        ? new LongHorizonContextVersion { VersionId = plan.ActiveContextVersionId, Sequence = seq }
                        : null,
                };
            }
        }

        var geWeeks = plan.TotalWeeks - 20;
        var segments = new List<LongHorizonStructuralSegment>
        {
            new() { SegmentType = LongHorizonStructuralSegmentType.GeneralEndurance, StartGlobalWeek = 1, EndGlobalWeek = geWeeks, Provenance = "Reconstructed" },
            new() { SegmentType = LongHorizonStructuralSegmentType.PreparationRunway, StartGlobalWeek = geWeeks + 1, EndGlobalWeek = geWeeks + 8, Provenance = "Reconstructed" },
            new() { SegmentType = LongHorizonStructuralSegmentType.Core, StartGlobalWeek = geWeeks + 9, EndGlobalWeek = plan.TotalWeeks, Provenance = "Reconstructed" },
        };

        var roadmap = new LongHorizonStructuralRoadmap
        {
            TotalWeeks = plan.TotalWeeks,
            GeneralEnduranceWeeks = geWeeks,
            PreparationRunwayWeeks = 8,
            CoreWeeks = 12,
            Segments = segments,
            GlobalWeekNumbers = Enumerable.Range(1, plan.TotalWeeks).ToList(),
            Weeks = structuralWeeks,
            RaceDate = plan.RaceDate,
            Profile = profile,
            StructuralStatus = "ReconstructedFromPersistence",
        };

        var latestWindowRecord = await db.LongHorizonActivationWindowRecords.AsNoTracking()
            .Where(a => a.PlanStateId == planStateId)
            .OrderByDescending(a => a.ActivatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var windowWeeks = activatedWeeks
            .Where(kv => kv.Key >= plan.CurrentWindowStartWeek && kv.Key <= plan.CurrentWindowEndWeek
                         && lifecycleStates[kv.Key] == LongHorizonNumericLifecycleState.NumericActivated)
            .OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();

        var currentWindow = new RollingNumericActivationWindow
        {
            WindowId = latestWindowRecord?.Id ?? Guid.NewGuid(),
            ContextVersion = new LongHorizonContextVersion { VersionId = plan.ActiveContextVersionId, Sequence = plan.ActiveContextVersionSequence },
            StartGlobalWeek = plan.CurrentWindowStartWeek,
            EndGlobalWeek = plan.CurrentWindowEndWeek,
            RequestedWindowSizeWeeks = 4,
            ActualWindowSizeWeeks = plan.CurrentWindowEndWeek - plan.CurrentWindowStartWeek + 1,
            Weeks = windowWeeks,
            SegmentsCovered = windowWeeks.Select(w => w.SegmentType).Distinct().ToList(),
            ActivationSource = LongHorizonInitialActivationSource.CheckpointRollingActivation,
            Status = windowWeeks.Count > 0 ? LongHorizonActivationWindowStatus.Activated : LongHorizonActivationWindowStatus.Blocked,
        };

        var checkpointDecisions = (await db.LongHorizonCheckpointRecords.AsNoTracking()
            .Where(c => c.PlanStateId == planStateId).OrderBy(c => c.AsOfDate).ToListAsync(cancellationToken))
            .Select(c => new LongHorizonCheckpointDecision
            {
                DecisionId = c.Id,
                EvidenceSnapshotId = c.Id,
                Outcome = MapCheckpointOutcomeBack(c.Decision),
                ActivationWindowBoundary = (c.SourceWindowStartWeek, c.SourceWindowEndWeek),
                SafetyPriorityApplied = false,
                PolicyProvenance = "ReconstructedFromLongHorizonCheckpointRecord",
            }).ToList();

        var runwayRow = await db.LongHorizonRunwayStates.AsNoTracking().FirstOrDefaultAsync(r => r.PlanStateId == planStateId, cancellationToken);

        // Full-fidelity round-trip: these are the exact real internal objects
        // (never regenerated) -- deserialized back unchanged so a Runway
        // continuation slice after restart never recomposes them.
        var runwayPrescription = runwayRow?.PrescriptionPayloadJson is { } prescriptionJson
            ? JsonSerializer.Deserialize<ImmutablePreparationRunwayPrescription<PreparationRunwayBlockType>>(prescriptionJson, LongHorizonRollingActivationPersistenceAdapter.FullFidelityJsonOptions)
            : null;
        var runwayTargetLock = runwayRow?.TargetLockPayloadJson is { } targetLockJson
            ? JsonSerializer.Deserialize<LongHorizonLockedCoreWeekOneTarget>(targetLockJson, LongHorizonRollingActivationPersistenceAdapter.FullFidelityJsonOptions)
            : null;
        var runwayCalendarProjection = runwayRow?.CalendarProjectionPayloadJson is { } calendarJson
            ? JsonSerializer.Deserialize<LongHorizonLockedRunwayCalendarProjection>(calendarJson, LongHorizonRollingActivationPersistenceAdapter.FullFidelityJsonOptions)
            : null;

        var state = new LongHorizonFullDarkLifecycleState
        {
            StructuralRoadmap = roadmap,
            StructuralSkeleton = skeleton,
            LifecycleStates = lifecycleStates,
            ActivatedWeeks = activatedWeeks,
            CurrentWindow = currentWindow,
            ContextVersion = new LongHorizonContextVersion { VersionId = plan.ActiveContextVersionId, Sequence = plan.ActiveContextVersionSequence },
            CheckpointDecisions = checkpointDecisions,
            RunwayTargetLock = runwayTargetLock,
            RunwayPrescription = runwayPrescription,
            RunwayCalendarProjection = runwayCalendarProjection,
            CoreContextIds = await db.LongHorizonCoreContextRecords.AsNoTracking()
                .Where(c => c.PlanStateId == planStateId).Select(c => c.Id).ToListAsync(cancellationToken),
            AuditEvents = [],
            ActivationInvocationCount = checkpointDecisions.Count + (runwayRow is not null ? 1 : 0),
            FullRunwayMaterializationCount = runwayRow is not null ? 1 : 0,
            LastCheckpointDate = plan.LatestCheckpointDate ?? plan.StartDate,
        };

        LongHorizonRollingPersistenceIntegrityValidator.ValidateReconstructedState(plan, state);

        return new LongHorizonRollingRestartSnapshot
        {
            PlanStateId = planStateId,
            PlanStartDate = plan.StartDate,
            DarkState = state,
            ConcurrencyVersion = xmin,
            CatalogRootPath = catalogRootPath,
            Candidate = candidate,
        };
    }

    private static LongHorizonStructuralSegmentType MapSegmentBack(LongHorizonPersistedSegmentType segment) => segment switch
    {
        LongHorizonPersistedSegmentType.GeneralEndurance => LongHorizonStructuralSegmentType.GeneralEndurance,
        LongHorizonPersistedSegmentType.PreparationRunway => LongHorizonStructuralSegmentType.PreparationRunway,
        LongHorizonPersistedSegmentType.Core => LongHorizonStructuralSegmentType.Core,
        _ => throw new ArgumentOutOfRangeException(nameof(segment)),
    };

    private static LongHorizonNumericLifecycleState MapLifecycleBack(LongHorizonPersistedLifecycleState state) => state switch
    {
        LongHorizonPersistedLifecycleState.StructurallyPlanned => LongHorizonNumericLifecycleState.StructurallyPlanned,
        LongHorizonPersistedLifecycleState.NumericPending => LongHorizonNumericLifecycleState.NumericPending,
        LongHorizonPersistedLifecycleState.NumericActivated => LongHorizonNumericLifecycleState.NumericActivated,
        LongHorizonPersistedLifecycleState.NumericActivationBlocked => LongHorizonNumericLifecycleState.NumericActivationBlocked,
        LongHorizonPersistedLifecycleState.Completed => LongHorizonNumericLifecycleState.Completed,
        LongHorizonPersistedLifecycleState.Missed => LongHorizonNumericLifecycleState.Missed,
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };

    private static LongHorizonCheckpointOutcome MapCheckpointOutcomeBack(LongHorizonPersistedCheckpointDecision decision) => decision switch
    {
        LongHorizonPersistedCheckpointDecision.GrowthEligible => LongHorizonCheckpointOutcome.GrowthEligible,
        LongHorizonPersistedCheckpointDecision.MaintenanceOnly => LongHorizonCheckpointOutcome.MaintenanceOnly,
        LongHorizonPersistedCheckpointDecision.Blocked => LongHorizonCheckpointOutcome.NumericActivationBlocked,
        _ => throw new ArgumentOutOfRangeException(nameof(decision)),
    };
}
