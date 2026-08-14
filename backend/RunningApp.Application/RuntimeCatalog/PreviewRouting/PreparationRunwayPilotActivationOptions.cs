namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4G.6B.1 — the real, tested, candidate-scoped
/// activation control for the 15-20 week TEN_K Preparation Runway public
/// preview path (Phase 4G.6B), replacing what was previously an
/// observational-only named constant with no actual runtime effect.
///
/// Mirrors the established <see cref="CatalogLivePilotOptions"/> pattern
/// (same options-binding convention: <c>services.Configure&lt;T&gt;(configuration.GetSection(T.SectionName))</c>,
/// registered in Program.cs, overridable per-environment via appsettings).
///
/// This gate controls ONLY the exact pilot scope
/// (Race/TenK/Intermediate/4-days-per-week/TEN_K__4D__INTERMEDIATE v10,
/// 15-20 available full weeks) — see <c>PlanServices.IsPreparationRunwayPilotScope</c>.
/// It has no effect on 8-14 week requests, on any other candidate/identity,
/// or on 21+ week requests, which are never routed through this gate at all.
///
/// Disabled behavior is a deliberate rollback-compatibility choice: when
/// <see cref="Enabled"/> is false, an exact-pilot 15-20 week request falls
/// through to the same <c>PlanHorizonCompositionRequiredException</c>
/// (HTTP 422 <c>PLAN_HORIZON_COMPOSITION_REQUIRED</c>) every other
/// unsupported horizon already produces — the pre-Phase-4G.6B public
/// contract, not a new <c>PREPARATION_RUNWAY_PREVIEW_NOT_ENABLED</c> error.
/// This minimizes public contract drift on rollback: a caller who only ever
/// saw the pre-4G.6B behavior sees the identical response if the gate is
/// ever disabled.
/// </summary>
public sealed class PreparationRunwayPilotActivationOptions
{
    public const string SectionName = "PreparationRunwayPilotActivation";

    /// <summary>
    /// The exact semantic activation key this options section governs.
    /// Matches the pre-existing <c>CatalogPreviewGenerator.PreparationRunwayPreviewGateName</c>
    /// constant used for trace/log identification — this is now the value
    /// that constant actually controls at runtime.
    /// </summary>
    public const string GateKey = "TEN_K_4D_INTERMEDIATE_PREPARATION_RUNWAY_PREVIEW";

    /// <summary>
    /// Backend Integration Phase 4G.6C — the second, narrower confirmation
    /// gate. Deliberately independent of <see cref="Enabled"/> (the preview
    /// gate) so preview activation and confirmation/persistence activation
    /// can be rolled out and rolled back independently:
    /// <list type="bullet">
    /// <item><see cref="Enabled"/>=true, this=false: preview succeeds (HTTP
    /// 200), lifecycle is <c>PreparationRunwayPreviewNotConfirmable</c>,
    /// confirm returns <c>CATALOG_PREVIEW_NOT_PERSISTABLE</c> — exactly
    /// Phase 4G.6B/4G.6B.1/4G.6B.2's existing, unmodified behavior.</item>
    /// <item><see cref="Enabled"/>=true, this=true: preview succeeds,
    /// lifecycle is <c>PreparationRunwayPreviewConfirmable</c>, confirm
    /// persists the plan.</item>
    /// <item><see cref="Enabled"/>=false: this is never consulted (the
    /// preview route is never reached at all).</item>
    /// </list>
    /// </summary>
    public const string ConfirmationGateKey = "TEN_K_4D_INTERMEDIATE_PREPARATION_RUNWAY_CONFIRMATION";

    /// <summary>Defaults enabled — matches this repository's pre-release, single-environment convention (no staged rollout infrastructure exists).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Defaults disabled in code (a materially higher-risk activation than preview alone — real persistence); enabled explicitly via appsettings for the environments that exercise it, mirroring the existing <see cref="Enabled"/>/<c>CatalogLivePilotOptions</c> convention of an explicit per-environment override.</summary>
    public bool ConfirmationEnabled { get; set; } = false;
}
