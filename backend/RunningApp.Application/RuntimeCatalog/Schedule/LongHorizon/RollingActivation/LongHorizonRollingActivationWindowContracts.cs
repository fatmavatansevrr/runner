namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>Phase 4K.5 Part 3 — outcome status of an activation-window attempt.</summary>
internal enum LongHorizonActivationWindowStatus
{
    Activated,
    Blocked,
}

/// <summary>
/// Phase 4K.5 Part 3 — the bounded, rolling numeric activation window
/// approved by Phase 4K.1 (preferred size 4 weeks; partial windows allowed
/// at plan/segment boundaries; mixed-segment windows permitted; atomic —
/// Phase 4K.1 §6, Phase 4K.4 §15).
/// </summary>
internal sealed record RollingNumericActivationWindow
{
    public required Guid WindowId { get; init; }
    public required LongHorizonContextVersion ContextVersion { get; init; }
    public required int StartGlobalWeek { get; init; }
    public required int EndGlobalWeek { get; init; }
    public required int RequestedWindowSizeWeeks { get; init; }
    public required int ActualWindowSizeWeeks { get; init; }
    public required IReadOnlyList<ActivatedNumericWeek> Weeks { get; init; }
    public required IReadOnlyList<LongHorizonStructuralSegmentType> SegmentsCovered { get; init; }
    public required LongHorizonInitialActivationSource ActivationSource { get; init; }
    public Guid? CheckpointDecisionId { get; init; }
    public Guid? JitContextDecisionId { get; init; }
    public required LongHorizonActivationWindowStatus Status { get; init; }
    public DateTime? DecisionTimestamp { get; init; }
}

/// <summary>
/// Phase 4K.5 Part 3 — validates activation-window invariants. Does not
/// validate individual week numeric completeness -- see
/// <see cref="LongHorizonActivatedNumericWeekValidator"/> for that.
/// </summary>
internal static class LongHorizonRollingActivationWindowValidator
{
    private const int PreferredWindowSizeWeeks = 4;

    public static void Validate(RollingNumericActivationWindow window)
    {
        if (window.ActualWindowSizeWeeks != window.EndGlobalWeek - window.StartGlobalWeek + 1)
        {
            throw new LongHorizonActivationWindowInvalidException(
                "ActualWindowSizeWeeks must equal EndGlobalWeek - StartGlobalWeek + 1.");
        }

        if (window.ActualWindowSizeWeeks > PreferredWindowSizeWeeks)
        {
            throw new LongHorizonActivationWindowInvalidException(
                $"Activation window may not exceed the preferred size of {PreferredWindowSizeWeeks} weeks.");
        }

        if (window.RequestedWindowSizeWeeks > PreferredWindowSizeWeeks)
        {
            throw new LongHorizonActivationWindowInvalidException(
                $"RequestedWindowSizeWeeks may not exceed the preferred size of {PreferredWindowSizeWeeks} weeks.");
        }

        if (window.Status == LongHorizonActivationWindowStatus.Activated && window.Weeks.Count != window.ActualWindowSizeWeeks)
        {
            throw new LongHorizonActivationWindowInvalidException(
                "An Activated window must carry exactly ActualWindowSizeWeeks activated weeks.");
        }

        if (window.Status == LongHorizonActivationWindowStatus.Blocked && window.Weeks.Count != 0)
        {
            throw new LongHorizonActivationWindowInvalidException(
                "A Blocked window must carry zero activated weeks (mixed-window atomicity, Phase 4K.4 §15).");
        }

        ValidateGlobalWeeksContiguous(window);
    }

    /// <summary>
    /// Phase 4K.6 pre-execution validation for the initial GE-only window.
    /// This runs before any numeric materializer is called, while the final
    /// <see cref="Validate"/> method validates the populated atomic result.
    /// </summary>
    public static void ValidateInitialSelection(
        int generalEnduranceWeeks,
        int startGlobalWeek,
        int endGlobalWeek,
        int requestedWindowSizeWeeks,
        int actualWindowSizeWeeks,
        IReadOnlyList<LongHorizonStructuralSegmentType> segmentsCovered,
        LongHorizonInitialActivationSource activationSource,
        LongHorizonContextVersion contextVersion)
    {
        var expectedEnd = Math.Min(PreferredWindowSizeWeeks, generalEnduranceWeeks);
        if (generalEnduranceWeeks < 1 || startGlobalWeek != 1 || endGlobalWeek != expectedEnd
            || requestedWindowSizeWeeks != PreferredWindowSizeWeeks || actualWindowSizeWeeks != expectedEnd
            || segmentsCovered.Count != 1 || segmentsCovered[0] != LongHorizonStructuralSegmentType.GeneralEndurance
            || activationSource != LongHorizonInitialActivationSource.InitialOnboardingActivation
            || contextVersion.Sequence != 1 || contextVersion.VersionId == Guid.Empty)
        {
            throw new LongHorizonActivationWindowInvalidException(
                "Initial activation must select only contiguous GE weeks 1..min(4, GeneralEnduranceWeeks), request four weeks, and use InitialOnboardingActivation/context sequence 1.");
        }
    }

    private static void ValidateGlobalWeeksContiguous(RollingNumericActivationWindow window)
    {
        if (window.Status != LongHorizonActivationWindowStatus.Activated)
        {
            return;
        }

        var sorted = window.Weeks.Select(w => w.GlobalWeekNumber).OrderBy(w => w).ToList();
        if (sorted.Count == 0 || sorted[0] != window.StartGlobalWeek || sorted[^1] != window.EndGlobalWeek)
        {
            throw new LongHorizonActivationWindowInvalidException(
                "Activated weeks must span exactly [StartGlobalWeek, EndGlobalWeek].");
        }

        for (var i = 1; i < sorted.Count; i++)
        {
            if (sorted[i] != sorted[i - 1] + 1)
            {
                throw new LongHorizonActivationWindowInvalidException("Activated weeks must be contiguous.");
            }
        }
    }

    /// <summary>
    /// Mixed-window atomicity (Phase 4K.1 §6, Phase 4K.4 §15): if any week in
    /// the window failed to activate, none of them may be NumericActivated.
    /// </summary>
    public static void ValidateAtomicity(LongHorizonActivationWindowStatus status, IReadOnlyList<ActivatedNumericWeek> weeks)
    {
        if (status == LongHorizonActivationWindowStatus.Blocked && weeks.Any(w => w.LifecycleState == LongHorizonNumericLifecycleState.NumericActivated))
        {
            throw new LongHorizonMixedWindowAtomicityViolationException(
                "A Blocked window must not contain any NumericActivated week -- the entire window succeeds or none of it activates.");
        }

        if (status == LongHorizonActivationWindowStatus.Activated && weeks.Any(w => w.LifecycleState != LongHorizonNumericLifecycleState.NumericActivated))
        {
            throw new LongHorizonMixedWindowAtomicityViolationException(
                "An Activated window must have every week NumericActivated -- partial activation is not supported (Phase 4K.4 §15).");
        }
    }
}
