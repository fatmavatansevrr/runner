namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview;

/// <summary>
/// Phase 4L.6C: server-authoritative, generation-only kill switch for the
/// dedicated Long-Horizon public preview endpoint
/// (<see cref="LongHorizonPublicPlanService.GeneratePreviewAsync"/>). Deliberately
/// scoped to NEW generation only — every other Long-Horizon surface (confirm of
/// an already-issued preview, Home/Calendar/detail reads, Complete, NotToday,
/// explicit activation, explicit retry, cancellation, terminal/history reads)
/// is untouched by this flag and remains fully operational regardless of its
/// value. No client request can set or read this value; it is bound only from
/// server-side configuration (environment variable / appsettings), read once
/// per request via the options pattern already used elsewhere in this
/// codebase (see CatalogLivePilotOptions for the same shape).
/// </summary>
public sealed class LongHorizonGenerationOptions
{
    public const string SectionName = "LongHorizon";
    public bool GenerationEnabled { get; set; } = true;
}
