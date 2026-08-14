namespace RunningApp.Application.RuntimeCatalog.Schedule.Horizon;

/// <summary>
/// Dark preparation/core composition vocabulary. These values describe what
/// a future planner would need; they do not grant public horizon support.
///
/// Backend Integration Phase 4G.6B.2 — API-surface audit decision: this
/// type, <see cref="CoreHorizonDecisionReason"/>, and <see cref="CoreHorizonDecision"/>
/// were widened from <c>internal</c> to <c>public</c> in Phase 4G.6B.1 so the
/// single authoritative horizon decision could be carried as a parameter on
/// <c>ICatalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync</c> — a
/// necessarily-public interface, because <c>RunningApp.Api</c>'s
/// <c>Program.cs</c> registers it via DI (<c>AddScoped&lt;ICatalogPreviewGenerator, CatalogPreviewGenerator&gt;()</c>)
/// and that assembly has no <c>InternalsVisibleTo</c> grant into
/// <c>RunningApp.Application</c> (confirmed: only <c>RunningApp.IntegrationTests</c>
/// has that grant). Splitting a separate internal
/// <c>IPreparationRunwayPreviewGenerator</c> interface (considered) would
/// still require the DI container to resolve it by name from
/// <c>RunningApp.Api</c>, hitting the identical visibility wall — the split
/// buys no actual encapsulation without a materially larger DI-registration
/// change (e.g. an Application-side public extension method performing the
/// internal-type registration), judged out of proportion to the risk this
/// audit is meant to catch. Retained public deliberately (Option D):
/// <list type="bullet">
/// <item>These three types are never referenced by any public HTTP request/
/// response DTO (see <see cref="RunningApp.Application.DTOs.Plan.GeneratePreviewResponse"/>,
/// <c>PreviewWeekDto</c>, <c>PreviewDayDto</c> — none has a field of any of
/// these types) and are never serialized to a client. Proven by
/// <c>PreparationRunwayHorizonAuthorityTests.CoreHorizonDecisionTypes_AreNeverUsedAsHttpResponseFields</c>.</item>
/// <item>Their only public-surface use is as method parameters on
/// <c>ICatalogPreviewGenerator</c> and as the type of
/// <c>TenKPreparationRunwayDarkOrchestrationRequest.HorizonDecision</c> (an
/// `internal` record) — never a controller action parameter, never a
/// Swagger-visible schema.</item>
/// </list>
/// </summary>
public enum CoreHorizonMode
{
    Unsupported,
    ReadinessOnly,
    CompressedCore,
    PreferredCore,
    ExtendedCore,
    PreparationRunwayPlusCore,
    InvalidInput,
}

public enum CoreHorizonDecisionReason
{
    InvalidDateRange,
    InvalidCoreBounds,
    BelowMinimumCore,
    CompressedStandaloneCore,
    PreferredStandaloneCore,
    ExtendedStandaloneCore,
    ExceedsMaximumCore,
}

/// <summary>
/// Inputs required for a day-accurate horizon decision. Core bounds come
/// from the selected catalog template; this contract does not load or invent
/// them.
/// </summary>
internal sealed record CoreHorizonContext(
    DateOnly StartDate,
    DateOnly RaceDate,
    int MinimumCoreWeeks,
    int PreferredCoreWeeks,
    int MaximumCoreWeeks);

public sealed record CoreHorizonDecision(
    int AvailableDays,
    int AvailableFullWeeks,
    int LeadingPartialDays,
    int MinimumCoreWeeks,
    int PreferredCoreWeeks,
    int MaximumCoreWeeks,
    CoreHorizonMode Mode,
    CoreHorizonDecisionReason Reason,
    IReadOnlyList<string> Rules);

/// <summary>
/// Canonical, day-accurate horizon classifier. RaceHorizonPolicy consumes its
/// decision and maps the mode to public eligibility/error behavior; routing,
/// preview generation, and allocation consume that same decision downstream.
/// </summary>
internal static class CoreHorizonClassifier
{
    internal const string Version = "PHASE_4G_5A_DAY_ACCURATE_V1";

    public static CoreHorizonDecision Classify(CoreHorizonContext context)
    {
        if (context.RaceDate < context.StartDate)
        {
            return Decision(0, context, CoreHorizonMode.InvalidInput,
                CoreHorizonDecisionReason.InvalidDateRange,
                "RaceDate must be on or after StartDate.");
        }

        if (context.MinimumCoreWeeks <= 0 ||
            context.MinimumCoreWeeks > context.PreferredCoreWeeks ||
            context.PreferredCoreWeeks > context.MaximumCoreWeeks)
        {
            return Decision(0, context, CoreHorizonMode.InvalidInput,
                CoreHorizonDecisionReason.InvalidCoreBounds,
                "Core bounds must satisfy 0 < minimum <= preferred <= maximum.");
        }

        // Established exclusive elapsed-day boundary: RaceDate - StartDate.
        // Full weeks and remainder stay separate; partial days never round
        // into an additional standalone-core week.
        var availableDays = context.RaceDate.DayNumber - context.StartDate.DayNumber;
        var minimumDays = context.MinimumCoreWeeks * 7;
        var preferredDays = context.PreferredCoreWeeks * 7;
        var maximumDays = context.MaximumCoreWeeks * 7;

        if (availableDays < minimumDays)
        {
            // Existing policy has no approved readiness-only selection rule;
            // fail closed rather than inventing one. ReadinessOnly remains a
            // representable future outcome in CoreHorizonMode.
            return Decision(availableDays, context, CoreHorizonMode.Unsupported,
                CoreHorizonDecisionReason.BelowMinimumCore,
                "Existing policy supplies no readiness-only routing rule for a below-minimum race core.");
        }

        if (availableDays < preferredDays)
        {
            return Decision(availableDays, context, CoreHorizonMode.CompressedCore,
                CoreHorizonDecisionReason.CompressedStandaloneCore,
                "Available days meet the catalog minimum but are below the preferred core duration.");
        }

        if (availableDays == preferredDays)
        {
            return Decision(availableDays, context, CoreHorizonMode.PreferredCore,
                CoreHorizonDecisionReason.PreferredStandaloneCore,
                "Available days exactly equal the preferred core duration.");
        }

        if (availableDays <= maximumDays)
        {
            return Decision(availableDays, context, CoreHorizonMode.ExtendedCore,
                CoreHorizonDecisionReason.ExtendedStandaloneCore,
                "Available days exceed preferred and do not exceed the catalog standalone maximum.");
        }

        return Decision(availableDays, context, CoreHorizonMode.PreparationRunwayPlusCore,
            CoreHorizonDecisionReason.ExceedsMaximumCore,
            "Available days exceed the catalog standalone maximum; future composition is required.");
    }

    private static CoreHorizonDecision Decision(
        int availableDays,
        CoreHorizonContext context,
        CoreHorizonMode mode,
        CoreHorizonDecisionReason reason,
        string rule) =>
        new(
            AvailableDays: availableDays,
            AvailableFullWeeks: availableDays / 7,
            LeadingPartialDays: availableDays % 7,
            MinimumCoreWeeks: context.MinimumCoreWeeks,
            PreferredCoreWeeks: context.PreferredCoreWeeks,
            MaximumCoreWeeks: context.MaximumCoreWeeks,
            Mode: mode,
            Reason: reason,
            Rules: [Version, rule]);
}
