using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;

/// <summary>
/// Phase 4K.8B Part 9 — a narrow association between one locked Core Week-1
/// target and the exact Runway global-week range it governs. One
/// prescription uses exactly one lock; the lock's own range must equal the
/// prescription's full range (enforced by
/// <see cref="ImmutablePreparationRunwayPrescriptionValidator"/> directly,
/// this type exists for callers that need to validate the association
/// before a full prescription is built).
/// </summary>
internal sealed record PreparationRunwayTargetLockScope
{
    public required LongHorizonLockedCoreWeekOneTarget Target { get; init; }
    public required PreparationRunwayPrescriptionId PrescriptionId { get; init; }
    public required PreparationRunwayPrescriptionVersion PrescriptionVersion { get; init; }
    public required (int StartGlobalWeek, int EndGlobalWeek) RunwayGlobalRange { get; init; }
}

internal static class PreparationRunwayTargetLockScopeValidator
{
    public static void Validate(PreparationRunwayTargetLockScope scope)
    {
        LongHorizonCoreTargetLockValidator.Validate(scope.Target);

        var lockRange = scope.Target.LockedForActivatedRunwayWeekRange;
        if (lockRange.StartGlobalWeek != scope.RunwayGlobalRange.StartGlobalWeek || lockRange.EndGlobalWeek != scope.RunwayGlobalRange.EndGlobalWeek)
        {
            throw new PreparationRunwayTargetLockScopeViolationException(
                $"One target lock must exactly cover the full Runway global range ({scope.RunwayGlobalRange.StartGlobalWeek}-{scope.RunwayGlobalRange.EndGlobalWeek}); " +
                $"the target's own range is ({lockRange.StartGlobalWeek}-{lockRange.EndGlobalWeek}) -- no per-slice lock is permitted.");
        }
    }
}

/// <summary>
/// Phase 4K.8B Part 10 — rejects any new context whose range overlaps an
/// already-locked full Runway prescription (Phase 4K.8A §21/§22: mid-Runway
/// target refresh is forbidden; a later context may govern only
/// non-overlapping, not-yet-activated weeks that begin after the Runway
/// global range ends). Reuses the same overlap-check shape as
/// <see cref="LongHorizonCoreTargetLockValidator.ValidateRefresh"/> rather
/// than inventing a split-Runway reconciliation policy.
/// </summary>
internal static class PreparationRunwayTargetRefreshGuard
{
    public static void ValidateRefreshOutsideRunwayRange(
        (int StartGlobalWeek, int EndGlobalWeek) existingRunwayRange,
        (int StartGlobalWeek, int EndGlobalWeek) newContextRange,
        LongHorizonContextVersion existingVersion,
        LongHorizonContextVersion newVersion)
    {
        var overlaps = newContextRange.StartGlobalWeek <= existingRunwayRange.EndGlobalWeek
            && newContextRange.EndGlobalWeek >= existingRunwayRange.StartGlobalWeek;

        if (overlaps)
        {
            throw new PreparationRunwayMidRunwayRefreshViolationException(
                $"A new context ({newContextRange.StartGlobalWeek}-{newContextRange.EndGlobalWeek}) must not overlap " +
                $"the locked Runway range ({existingRunwayRange.StartGlobalWeek}-{existingRunwayRange.EndGlobalWeek}) -- " +
                "mid-Runway target refresh is forbidden (Phase 4K.8A).");
        }

        if (newVersion.Sequence <= existingVersion.Sequence)
        {
            throw new PreparationRunwayMidRunwayRefreshViolationException(
                "A new context must carry a strictly later ContextVersion than the Runway prescription it follows.");
        }
    }
}
