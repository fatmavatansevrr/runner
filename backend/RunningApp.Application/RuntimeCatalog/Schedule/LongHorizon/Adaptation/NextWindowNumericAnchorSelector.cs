using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.4B.2 -- Rev4 §7 "Maintain / Reduce Numeric Anchor Semantics",
/// implemented exactly. This is a pure selection function only: it decides
/// WHICH already-authoritative <see cref="ValidatedSustainableLoad"/> value
/// (the freshly-aggregated current-window evidence, or the existing
/// PriorValidatedCheckpointLoad carried by <c>PriorAnchor(state)</c>) is
/// handed to the unmodified existing composition pipeline. It does not
/// compute adherence, does not touch the database, does not generate
/// workouts, and does not itself implement "CatalogProgressionStep" --
/// per Rev4 §13.7, that remains the existing composition pipeline's own
/// authority; this selector only chooses its input.
///
/// ProgressAsPlanned = the current-window evidence, unmodified -- exactly
///   today's pre-4M.4B.2 behavior, so composition still applies its own
///   existing progression to it.
/// Maintain          = PriorValidatedCheckpointLoad, verbatim. Composition
///   still runs (chronology/phase/catalog remain fully authoritative,
///   Rev4 §6), but the anchor it is seeded with is held, not re-aggregated.
/// Reduce            = the lower of the two by WeeklyVolumeKm when any
///   evidence exists (EffectiveCompletedCount > 0), else
///   PriorValidatedCheckpointLoad verbatim (Rev4's own "min(undefined, X)
///   = X" degeneracy -- Reduce becomes numerically identical to Maintain
///   in the zero-completion case, by the formula itself, not a special
///   case coded here).
/// </summary>
internal static class NextWindowNumericAnchorSelector
{
    public static ValidatedSustainableLoad? Select(
        NextWindowLoadDecision decision,
        ValidatedSustainableLoad? currentWindowValidatedLoad,
        ValidatedSustainableLoad? priorValidatedCheckpointLoad,
        int effectiveCompletedCount) => decision switch
    {
        NextWindowLoadDecision.ProgressAsPlanned => currentWindowValidatedLoad,
        // Rev4 does not literally define Maintain's behavior when no prior
        // checkpoint has ever been recorded (a plan's very first-ever
        // checkpoint can still legitimately produce Maintain, since
        // LoadDecision is driven by this window's own completion count,
        // not by comparison to a prior window). Rev4's own Reduce formula
        // already establishes the precedent that this whole model degrades
        // gracefully to whatever real evidence IS available ("min(undefined,
        // X) = X") rather than producing nothing -- applied symmetrically
        // here: with nothing to hold, fall back to this window's own
        // evidence rather than blocking an otherwise-successful activation.
        // See Phase 4M.4B.2 doc for the explicit disclosure of this
        // interpretive extension (no new persisted state, no invented value
        // -- both are already-computed, already-authoritative inputs).
        NextWindowLoadDecision.Maintain => priorValidatedCheckpointLoad ?? currentWindowValidatedLoad,
        NextWindowLoadDecision.Reduce => effectiveCompletedCount > 0
            ? SelectLowerByWeeklyVolume(currentWindowValidatedLoad, priorValidatedCheckpointLoad)
            : priorValidatedCheckpointLoad ?? currentWindowValidatedLoad,
        _ => throw new ArgumentOutOfRangeException(nameof(decision), decision, "Unrecognized next-window load decision."),
    };

    /// <summary>
    /// min(current, prior) per Rev4 §7, compared by WeeklyVolumeKm and
    /// returning the WHOLE selected record (never a per-field hybrid of
    /// the two evidence sources -- neither source's LongRunKm/provenance
    /// is mixed with the other's WeeklyVolumeKm). Degrades to whichever
    /// side is non-null if one side is absent, matching Rev4's own
    /// "min(undefined, X) = X" note.
    /// </summary>
    private static ValidatedSustainableLoad? SelectLowerByWeeklyVolume(
        ValidatedSustainableLoad? current, ValidatedSustainableLoad? prior)
    {
        if (current is null) return prior;
        if (prior is null) return current;
        if (current.WeeklyVolumeKm is not { } currentKm) return prior;
        if (prior.WeeklyVolumeKm is not { } priorKm) return current;
        return currentKm <= priorKm ? current : prior;
    }
}
