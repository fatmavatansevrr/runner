namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>Phase 4K.1's six approved numeric lifecycle states, verbatim.</summary>
internal enum LongHorizonNumericLifecycleState
{
    StructurallyPlanned,
    NumericPending,
    NumericActivated,
    Completed,
    Missed,
    NumericActivationBlocked,
}

/// <summary>
/// Phase 4K.5 Part 2 — the allowed lifecycle transition matrix. Completed
/// and Missed are terminal (no outgoing transition at all -- Phase 4K.1's
/// "completed/missed history is immutable"). NumericActivationBlocked can
/// only return to NumericPending through an explicit new checkpoint
/// decision, modeled as a required boolean flag rather than an ordinary
/// entry in the transition table, so it cannot be triggered accidentally.
/// </summary>
internal static class LongHorizonNumericLifecycleTransitionValidator
{
    private static readonly IReadOnlyDictionary<LongHorizonNumericLifecycleState, IReadOnlyCollection<LongHorizonNumericLifecycleState>> AllowedTransitions =
        new Dictionary<LongHorizonNumericLifecycleState, IReadOnlyCollection<LongHorizonNumericLifecycleState>>
        {
            [LongHorizonNumericLifecycleState.StructurallyPlanned] = new[] { LongHorizonNumericLifecycleState.NumericPending },
            [LongHorizonNumericLifecycleState.NumericPending] = new[]
            {
                LongHorizonNumericLifecycleState.NumericActivated,
                LongHorizonNumericLifecycleState.NumericActivationBlocked,
            },
            [LongHorizonNumericLifecycleState.NumericActivated] = new[]
            {
                LongHorizonNumericLifecycleState.Completed,
                LongHorizonNumericLifecycleState.Missed,
            },
            [LongHorizonNumericLifecycleState.NumericActivationBlocked] = Array.Empty<LongHorizonNumericLifecycleState>(),
            [LongHorizonNumericLifecycleState.Completed] = Array.Empty<LongHorizonNumericLifecycleState>(),
            [LongHorizonNumericLifecycleState.Missed] = Array.Empty<LongHorizonNumericLifecycleState>(),
        };

    /// <summary>Validates an ordinary transition. NumericActivationBlocked -&gt; NumericPending is never legal through this overload -- use <see cref="ValidateBlockedRecoveryTransition"/>.</summary>
    public static void ValidateTransition(LongHorizonNumericLifecycleState from, LongHorizonNumericLifecycleState to)
    {
        if (AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to))
        {
            return;
        }

        throw new LongHorizonIllegalLifecycleTransitionException(
            $"Illegal Long-Horizon numeric lifecycle transition: {from} -> {to}.");
    }

    /// <summary>
    /// The one legal exception to the ordinary matrix: a blocked week may
    /// return to NumericPending, but only when an explicit new checkpoint
    /// decision authorizes it (Phase 4K.1 §7, "NumericActivationBlocked ->
    /// NumericPending only through an explicit new checkpoint decision").
    /// </summary>
    public static void ValidateBlockedRecoveryTransition(
        LongHorizonNumericLifecycleState from,
        LongHorizonNumericLifecycleState to,
        bool hasNewCheckpointDecision)
    {
        if (from == LongHorizonNumericLifecycleState.NumericActivationBlocked
            && to == LongHorizonNumericLifecycleState.NumericPending)
        {
            if (!hasNewCheckpointDecision)
            {
                throw new LongHorizonIllegalLifecycleTransitionException(
                    "NumericActivationBlocked -> NumericPending requires an explicit new checkpoint decision.");
            }

            return;
        }

        ValidateTransition(from, to);
    }
}
