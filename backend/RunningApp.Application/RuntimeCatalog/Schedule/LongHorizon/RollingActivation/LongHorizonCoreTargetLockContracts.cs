namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.5 Part 12 — the typed target Preparation Runway's numeric
/// materializer interpolates toward (Phase 4K.4 §4/§9's real, existing
/// dependency finding). Immutable by construction: <see cref="Refresh"/>
/// returns a NEW instance with a new <see cref="LongHorizonContextVersion"/>
/// rather than mutating this one, and
/// <see cref="LongHorizonCoreTargetLockValidator.ValidateRefresh"/> refuses
/// a refresh that would overlap an already-activated Runway week range.
/// </summary>
internal sealed record LongHorizonLockedCoreWeekOneTarget
{
    public required double TargetWeeklyVolumeKm { get; init; }
    public required double TargetLongRunKm { get; init; }
    public double? PaceTargetSecondsPerKm { get; init; }
    public required LongHorizonEvidenceAuthorityRecord Source { get; init; }
    public required LongHorizonEvidenceAuthorityStatus AuthorityStatus { get; init; }
    public required LongHorizonContextVersion ContextVersion { get; init; }
    public required (int StartGlobalWeek, int EndGlobalWeek) LockedForActivatedRunwayWeekRange { get; init; }
    public required Guid CreatedByDecisionId { get; init; }
    public bool ImmutableAfterActivation { get; init; } = true;

    /// <summary>
    /// Produces a new, later-versioned target for weeks NOT YET activated
    /// against this one (Phase 4K.4 §9's "versioned future-only Core
    /// refresh"). Never mutates the current instance.
    /// </summary>
    public LongHorizonLockedCoreWeekOneTarget Refresh(
        double newTargetWeeklyVolumeKm,
        double newTargetLongRunKm,
        double? newPaceTargetSecondsPerKm,
        LongHorizonEvidenceAuthorityRecord newSource,
        (int StartGlobalWeek, int EndGlobalWeek) newLockedRange,
        Guid newDecisionId) => this with
    {
        TargetWeeklyVolumeKm = newTargetWeeklyVolumeKm,
        TargetLongRunKm = newTargetLongRunKm,
        PaceTargetSecondsPerKm = newPaceTargetSecondsPerKm,
        Source = newSource,
        ContextVersion = ContextVersion.Next(),
        LockedForActivatedRunwayWeekRange = newLockedRange,
        CreatedByDecisionId = newDecisionId,
    };
}

/// <summary>Phase 4K.5 Part 12 — enforces that a target is mandatory when Runway is activated, and that a refresh never overlaps an already-locked range.</summary>
internal static class LongHorizonCoreTargetLockValidator
{
    public static void Validate(LongHorizonLockedCoreWeekOneTarget target)
    {
        if (target.TargetWeeklyVolumeKm <= 0 || target.TargetLongRunKm <= 0)
        {
            throw new LongHorizonJitContextInvalidException(
                "LockedCoreWeekOneTarget's TargetWeeklyVolumeKm and TargetLongRunKm must be positive.");
        }

        if (target.LockedForActivatedRunwayWeekRange.EndGlobalWeek < target.LockedForActivatedRunwayWeekRange.StartGlobalWeek)
        {
            throw new LongHorizonJitContextInvalidException(
                "LockedForActivatedRunwayWeekRange.EndGlobalWeek must be >= StartGlobalWeek.");
        }
    }

    /// <summary>
    /// A refreshed target's newly locked range must not overlap the prior
    /// target's already-activated range -- already-activated Runway weeks'
    /// target must never change (Phase 4K.4 §4/§9, Phase 4K.5 Part 12).
    /// </summary>
    public static void ValidateRefresh(LongHorizonLockedCoreWeekOneTarget prior, LongHorizonLockedCoreWeekOneTarget refreshed)
    {
        var priorRange = prior.LockedForActivatedRunwayWeekRange;
        var refreshedRange = refreshed.LockedForActivatedRunwayWeekRange;

        var overlaps = refreshedRange.StartGlobalWeek <= priorRange.EndGlobalWeek
            && refreshedRange.EndGlobalWeek >= priorRange.StartGlobalWeek;

        if (overlaps)
        {
            throw new LongHorizonLockedTargetImmutabilityViolationException(
                $"A refreshed Core Week-1 target's week range ({refreshedRange.StartGlobalWeek}-{refreshedRange.EndGlobalWeek}) " +
                $"must not overlap the already-locked range ({priorRange.StartGlobalWeek}-{priorRange.EndGlobalWeek}).");
        }

        if (refreshed.ContextVersion.Sequence <= prior.ContextVersion.Sequence)
        {
            throw new LongHorizonLockedTargetImmutabilityViolationException(
                "A refreshed target must carry a strictly later ContextVersion than the target it refreshes.");
        }
    }
}
