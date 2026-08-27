using System.Security.Cryptography;
using System.Text;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.8D — selection/value-mapping only. This adapter never invokes a
/// calendar composer, WeekStartDate, a weekday selector, or a spacing rule.
/// </summary>
internal static class LongHorizonRealCalendarProjectionAdapter
{
    public static LongHorizonActivatedCalendarProjectionResult MapSelectedWindow(
        TenKPreparationRunwayDarkOrchestrationResult? realComposition,
        LongHorizonRollingJitActivationResult activation,
        LongHorizonStructuralRoadmap roadmap,
        IReadOnlyList<DayOfWeek> preferredDays,
        DayOfWeek longRunDay,
        LongHorizonLockedRunwayCalendarProjection? existingRunwayProjection = null)
    {
        if (activation.ActivationWindow is null || activation.NewlyActivatedWeeks.Count == 0)
            throw new LongHorizonSessionCalendarProjectionMismatchException("A successful non-empty activation window is required for calendar projection.");

        var stages = new List<string> { "RealCompositionResultValidation" };
        var runwaySegment = roadmap.Segments.Single(s => s.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway);
        var coreSegment = roadmap.Segments.Single(s => s.SegmentType == LongHorizonStructuralSegmentType.Core);

        var selectedRunwayWeeks = activation.NewlyActivatedWeeks
            .Where(w => w.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway).ToArray();
        var selectedCoreWeeks = activation.NewlyActivatedWeeks
            .Where(w => w.SegmentType == LongHorizonStructuralSegmentType.Core).ToArray();

        LongHorizonLockedRunwayCalendarProjection? fullRunway = existingRunwayProjection;
        if (selectedRunwayWeeks.Length > 0)
        {
            if (fullRunway is null)
            {
                if (realComposition?.CalendarComposition?.DatedRunwayWeeks is null || activation.RunwayPrescription is null)
                    throw new LongHorizonMissingDatedSessionException("Selected Runway weeks require the real full Runway calendar composition or its immutable continuation projection.");

                fullRunway = BuildFullRunwayProjection(realComposition, activation, runwaySegment, preferredDays, longRunDay);
            }

            ValidateRunwayProjectionAuthority(fullRunway, activation, runwaySegment);
        }
        stages.Add("SelectedSessionIdentityMapping");

        var selected = new List<LongHorizonActivatedSessionCalendarProjection>();
        if (selectedRunwayWeeks.Length > 0)
        {
            var selectedGlobals = selectedRunwayWeeks.Select(w => w.GlobalWeekNumber).ToHashSet();
            selected.AddRange(fullRunway!.Sessions.Where(s => selectedGlobals.Contains(s.GlobalWeekNumber)).Select(s => s with
            {
                RunwaySliceId = activation.RunwaySlice?.SliceId,
                ContextVersion = activation.ContextVersion,
                CoreTargetLockId = activation.CoreTargetLock?.ContextVersion.VersionId,
            }));
        }

        if (selectedCoreWeeks.Length > 0)
        {
            if (realComposition?.CalendarComposition?.DatedCoreWeeks is null)
                throw new LongHorizonMissingDatedSessionException("Selected Core weeks require the real Core calendar composition.");
            selected.AddRange(BuildSelectedCoreProjection(realComposition, activation, coreSegment, preferredDays, longRunDay));
        }

        selected = selected.OrderBy(s => s.GlobalWeekNumber).ThenBy(s => s.SessionDate).ThenBy(s => s.SessionOrdinal).ToList();
        EnsureExactSelectedSessionSet(activation.NewlyActivatedWeeks, selected);
        stages.Add("SessionDateProjection");

        var projectionSeed = string.Join('|', activation.ContextVersion.VersionId,
            activation.ActivationWindow.StartGlobalWeek, activation.ActivationWindow.EndGlobalWeek,
            string.Join(';', selected.Select(SessionIdentitySeed)));

        return new LongHorizonActivatedCalendarProjectionResult
        {
            ProjectionId = StableGuid(projectionSeed),
            SelectedSessions = selected,
            FullRunwayProjection = fullRunway,
            ValidationStages = stages,
        };
    }

    public static LongHorizonRollingJitActivationResult AlignActivationResult(
        LongHorizonRollingJitActivationResult activation,
        LongHorizonActivatedCalendarProjectionResult projection,
        IReadOnlyList<DayOfWeek> preferredDays,
        DayOfWeek longRunDay)
    {
        var aligned = activation.NewlyActivatedWeeks.Select(week =>
        {
            if (week.SegmentType == LongHorizonStructuralSegmentType.GeneralEndurance)
                return week;

            var projected = projection.SelectedSessions.Where(s => s.GlobalWeekNumber == week.GlobalWeekNumber)
                .OrderBy(s => s.SessionOrdinal).ToArray();
            if (week.SessionPrescriptions is null || projected.Length != week.SessionPrescriptions.Count)
                throw new LongHorizonMissingDatedSessionException($"Week {week.GlobalWeekNumber} does not have one projected date per numeric session.");

            var sessions = week.SessionPrescriptions.Select((session, index) =>
            {
                var match = projected.SingleOrDefault(p => p.SessionOrdinal == session.SessionOrdinal);
                if (match is null)
                    throw new LongHorizonMissingDatedSessionException($"Week {week.GlobalWeekNumber} session ordinal {session.SessionOrdinal} has no composed date.");
                if (!string.Equals(match.SessionRole, session.SessionRole, StringComparison.Ordinal)
                    || !string.Equals(match.WorkoutKey, session.WorkoutKey, StringComparison.Ordinal)
                    || match.WorkoutVersion != session.WorkoutVersion)
                    throw new LongHorizonCalendarIdentityMismatchException($"Week {week.GlobalWeekNumber} session ordinal {session.SessionOrdinal} identity differs from the composed session.");

                return session with
                {
                    AssignedDate = match.SessionDate,
                    Source = $"{match.CalendarCompositionIdentity}|{match.OriginalComposedSessionIdentity}",
                };
            }).ToArray();

            return week with { SessionPrescriptions = sessions };
        }).ToArray();

        var window = activation.ActivationWindow! with { Weeks = aligned };
        var result = activation with
        {
            ActivationWindow = window,
            NewlyActivatedWeeks = aligned,
            ValidationStages = activation.ValidationStages.Concat(["SessionDateProjection"]).ToArray(),
        };

        LongHorizonActivatedCalendarAlignmentValidator.Validate(result, projection, preferredDays, longRunDay);
        foreach (var week in result.NewlyActivatedWeeks)
            LongHorizonActivatedNumericWeekValidator.Validate(week);
        LongHorizonRollingActivationWindowValidator.Validate(result.ActivationWindow);
        LongHorizonRollingActivationWindowValidator.ValidateAtomicity(result.ActivationWindow.Status, result.ActivationWindow.Weeks);
        return result with
        {
            ValidationStages = result.ValidationStages.Concat([
                "PerWeekCalendarAlignment", "MixedBoundaryContinuity", "ActivatedNumericWeekValidation", "FinalActivationResultValidation"
            ]).ToArray(),
        };
    }

    private static LongHorizonLockedRunwayCalendarProjection BuildFullRunwayProjection(
        TenKPreparationRunwayDarkOrchestrationResult composition,
        LongHorizonRollingJitActivationResult activation,
        LongHorizonStructuralSegment runwaySegment,
        IReadOnlyList<DayOfWeek> preferredDays,
        DayOfWeek longRunDay)
    {
        var calendar = composition.CalendarComposition!;
        var prescription = activation.RunwayPrescription!;
        var identity = CalendarIdentity(calendar, preferredDays, longRunDay);
        var version = $"{calendar.DateAuthority!.SourcePolicyKey}:v{calendar.DateAuthority.SourcePolicyVersion}";
        var sessions = calendar.DatedRunwayWeeks!.SelectMany(week => week.StructuralOrderedSlots.Select(slot =>
        {
            var structural = slot.PrescribedSlot.StructuralSlot;
            var globalWeek = runwaySegment.StartGlobalWeek + week.SegmentLocalWeekNumber - 1;
            return new LongHorizonActivatedSessionCalendarProjection
            {
                GlobalWeekNumber = globalWeek,
                Segment = LongHorizonStructuralSegmentType.PreparationRunway,
                SessionOrdinal = structural.SlotOrdinal,
                SessionRole = LongHorizonSessionRoleCodec.ToCanonicalToken(structural.SlotRole),
                WorkoutKey = structural.WorkoutId,
                WorkoutVersion = structural.WorkoutVersion,
                SessionDate = slot.SessionDate,
                Weekday = slot.SessionDayOfWeek,
                PreferredDayProvenance = slot.CalendarProvenance.AssignmentRule,
                LongRunDayProvenance = structural.SlotRole == PreparationRunwaySlotRole.LongRun ? longRunDay.ToString() : "NOT_LONG_RUN",
                CalendarCompositionIdentity = identity,
                CalendarCompositionVersion = version,
                OriginalComposedSessionIdentity = $"RUNWAY:{week.SegmentLocalWeekNumber}:{structural.SlotOrdinal}:{structural.WorkoutId}:v{structural.WorkoutVersion}",
                ContextVersion = activation.ContextVersion,
                RunwayPrescriptionId = prescription.PrescriptionId,
                RunwaySliceId = activation.RunwaySlice?.SliceId,
                CoreTargetLockId = activation.CoreTargetLock?.ContextVersion.VersionId,
            };
        })).OrderBy(s => s.GlobalWeekNumber).ThenBy(s => s.SessionOrdinal).ToArray();

        return new LongHorizonLockedRunwayCalendarProjection
        {
            ProjectionId = StableGuid(string.Join('|', prescription.PrescriptionId.Value, identity, string.Join(';', sessions.Select(SessionIdentitySeed)))),
            PrescriptionId = prescription.PrescriptionId,
            StartGlobalWeek = runwaySegment.StartGlobalWeek,
            EndGlobalWeek = runwaySegment.EndGlobalWeek,
            Sessions = sessions,
            CalendarCompositionIdentity = identity,
            CalendarCompositionVersion = version,
        };
    }

    private static IReadOnlyList<LongHorizonActivatedSessionCalendarProjection> BuildSelectedCoreProjection(
        TenKPreparationRunwayDarkOrchestrationResult composition,
        LongHorizonRollingJitActivationResult activation,
        LongHorizonStructuralSegment coreSegment,
        IReadOnlyList<DayOfWeek> preferredDays,
        DayOfWeek longRunDay)
    {
        var calendar = composition.CalendarComposition!;
        var identity = CalendarIdentity(calendar, preferredDays, longRunDay);
        var version = $"{calendar.DateAuthority!.SourcePolicyKey}:v{calendar.DateAuthority.SourcePolicyVersion}";
        var selectedWeeks = activation.NewlyActivatedWeeks.Where(w => w.SegmentType == LongHorizonStructuralSegmentType.Core).ToDictionary(w => w.GlobalWeekNumber);

        return calendar.DatedCoreWeeks!.Where(w => selectedWeeks.ContainsKey(coreSegment.StartGlobalWeek + w.SegmentLocalWeekNumber - 1))
            .SelectMany(week =>
            {
                var globalWeek = coreSegment.StartGlobalWeek + week.SegmentLocalWeekNumber - 1;
                var numeric = selectedWeeks[globalWeek];
                return week.CoreWeek.SessionSlots.OrderBy(s => s.SlotOrderInWeek).Select(slot =>
                {
                    var numericSession = numeric.SessionPrescriptions!.SingleOrDefault(s => s.SessionOrdinal == slot.SlotOrderInWeek)
                        ?? throw new LongHorizonMissingDatedSessionException($"Core week {globalWeek} slot {slot.SlotOrderInWeek} has no numeric session identity.");
                    return new LongHorizonActivatedSessionCalendarProjection
                    {
                        GlobalWeekNumber = globalWeek,
                        Segment = LongHorizonStructuralSegmentType.Core,
                        SessionOrdinal = slot.SlotOrderInWeek,
                        SessionRole = slot.StructuralRole,
                        WorkoutKey = numericSession.WorkoutKey ?? throw new LongHorizonCalendarIdentityMismatchException("Core workout key is missing."),
                        WorkoutVersion = numericSession.WorkoutVersion ?? throw new LongHorizonCalendarIdentityMismatchException("Core workout version is missing."),
                        SessionDate = slot.SessionDate,
                        Weekday = slot.SessionDayOfWeek,
                        PreferredDayProvenance = slot.Provenance.AssignmentRule,
                        LongRunDayProvenance = LongHorizonSessionRoleCodec.IsLongRun(slot.StructuralRole) ? longRunDay.ToString() : "NOT_LONG_RUN",
                        CalendarCompositionIdentity = identity,
                        CalendarCompositionVersion = version,
                        OriginalComposedSessionIdentity = $"CORE:{week.SegmentLocalWeekNumber}:{slot.SlotOrderInWeek}:{numericSession.WorkoutKey}:v{numericSession.WorkoutVersion}",
                        ContextVersion = activation.ContextVersion,
                        CoreTargetLockId = activation.CoreTargetLock?.ContextVersion.VersionId,
                    };
                });
            }).OrderBy(s => s.GlobalWeekNumber).ThenBy(s => s.SessionOrdinal).ToArray();
    }

    private static void ValidateRunwayProjectionAuthority(LongHorizonLockedRunwayCalendarProjection projection,
        LongHorizonRollingJitActivationResult activation, LongHorizonStructuralSegment segment)
    {
        if (!projection.Immutable || activation.RunwayPrescription is null
            || projection.PrescriptionId != activation.RunwayPrescription.PrescriptionId
            || projection.StartGlobalWeek != segment.StartGlobalWeek || projection.EndGlobalWeek != segment.EndGlobalWeek)
            throw new LongHorizonCalendarIdentityMismatchException("Existing Runway calendar projection does not match the immutable full prescription and structural range.");
        // Phase 10K-FREQ.6D.19 -- derives the expected per-week session count from the
        // real prescribed week's own OrderedSlots width (5 for the approved 5D Runway
        // shape, 1K+3E+1L) instead of a hardcoded 4 -- byte-identical for every 4D
        // caller, whose OrderedSlots width is already 4.
        var sessionsPerWeek = activation.RunwayPrescription.FullWeekReferences[0].ProductionWeek.OrderedSlots.Count;
        if (projection.Sessions.Count != activation.RunwayPrescription.FullRunwayDurationWeeks * sessionsPerWeek)
            throw new LongHorizonMissingDatedSessionException("Full Runway calendar projection must contain the exact expected session count for every full Runway week.");
    }

    private static void EnsureExactSelectedSessionSet(IReadOnlyList<ActivatedNumericWeek> weeks,
        IReadOnlyList<LongHorizonActivatedSessionCalendarProjection> selected)
    {
        foreach (var week in weeks.Where(w => w.SegmentType is LongHorizonStructuralSegmentType.PreparationRunway or LongHorizonStructuralSegmentType.Core))
        {
            var projected = selected.Where(s => s.GlobalWeekNumber == week.GlobalWeekNumber).ToArray();
            if (week.SessionPrescriptions is null || projected.Length != week.SessionPrescriptions.Count)
                throw new LongHorizonMissingDatedSessionException($"Week {week.GlobalWeekNumber} has a missing or unexpected composed session.");
            if (projected.Select(s => s.SessionOrdinal).Distinct().Count() != projected.Length)
                throw new LongHorizonDuplicateDatedSessionException($"Week {week.GlobalWeekNumber} has a duplicate composed session ordinal.");
        }
    }

    private static string CalendarIdentity(PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType> calendar,
        IReadOnlyList<DayOfWeek> preferredDays, DayOfWeek longRunDay)
    {
        var dates = calendar.OrderedCombinedWeeks!.SelectMany(w => w.RunwayWeek?.StructuralOrderedSlots.Select(s => s.SessionDate)
            ?? w.CoreWeek!.CoreWeek.SessionSlots.Select(s => s.SessionDate));
        var seed = string.Join('|', calendar.DateAuthority!.SourcePolicyKey, calendar.DateAuthority.SourcePolicyVersion,
            string.Join(',', preferredDays), longRunDay, string.Join(',', dates.Select(d => d.ToString("yyyy-MM-dd"))));
        return StableGuid(seed).ToString("N");
    }

    private static string SessionIdentitySeed(LongHorizonActivatedSessionCalendarProjection s) =>
        $"{s.GlobalWeekNumber}:{s.Segment}:{s.SessionOrdinal}:{s.SessionRole}:{s.WorkoutKey}:v{s.WorkoutVersion}:{s.SessionDate:yyyy-MM-dd}";

    private static Guid StableGuid(string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, 16));
}
