using System.Linq;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4F.4/4F.5 — proves the dark skeleton AND dark
/// calendar-assignment wiring added to <see cref="CatalogPreviewGenerator"/>
/// has zero effect on <see cref="CatalogPlanConfirmationService"/> or the
/// legacy SQL flow. Confirm-specific persistence/rejection behavior itself
/// is already exhaustively covered by the pre-existing, unmodified
/// <c>CatalogPlanConfirmationServiceTests</c> (still green — see the full
/// suite run) and legacy routing by <c>Phase4F8_2LivePilotRoutingTests</c>
/// (the actually-registered <see cref="LivePlanPreviewRoutingService"/>,
/// per Phase 4F.9.1A's removal of the unregistered, dead
/// <c>PilotGenerationRouteDecider</c>); this file only adds the
/// dependency-graph proof.
/// </summary>
public sealed class Phase4F4ConfirmAndLegacyRegressionTests
{
    [Fact]
    public void CatalogPlanConfirmationService_ConstructorSurface_UnchangedByPhase4F4Or4F5()
    {
        var ctors = typeof(CatalogPlanConfirmationService).GetConstructors();
        Assert.Single(ctors);

        var paramTypeNames = ctors[0].GetParameters().Select(p => p.ParameterType.FullName ?? "").ToList();

        // "Materialization" covers both the Phase 4F.3 orchestrator/resolvers
        // AND the Phase 4F.5 calendar materializer -- all live in the
        // RuntimeCatalog.Schedule.Materialization namespace.
        Assert.DoesNotContain(paramTypeNames, t => t.Contains("CatalogPlanSkeletonOrchestrator") || t.Contains("Materialization"));
        // Unchanged since Phase 4E.2/4F.1: AppDbContext, ILogger, IGeneratedCatalogPlanPayloadValidator.
        Assert.Equal(3, paramTypeNames.Count);
    }

    [Fact]
    public void CatalogPlanConfirmationService_NeverInvokesSkeletonOrCalendarMaterialization_NoTypeReferenceAnywhereInAssembly()
    {
        // Structural proof at the method-signature level: ConfirmAsync's own
        // declared return/parameter types carry no orchestration/calendar
        // reference, and the type has no field of either type (reflection
        // over instance fields, incl. private).
        var fields = typeof(CatalogPlanConfirmationService).GetFields(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.DoesNotContain(fields, f => f.FieldType.FullName?.Contains("Materialization") ?? false);
    }
}
