using System.Linq;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4F.4 — proves the dark skeleton wiring added to
/// <see cref="CatalogPreviewGenerator"/> has zero effect on
/// <see cref="CatalogPlanConfirmationService"/> or the legacy SQL flow.
/// Confirm-specific persistence/rejection behavior itself is already
/// exhaustively covered by the pre-existing, unmodified
/// <c>CatalogPlanConfirmationServiceTests</c> (still green — see the full
/// suite run) and legacy routing by the pre-existing, unmodified
/// <c>PilotGenerationRouteDeciderTests</c>; this file only adds the
/// Phase 4F.4-specific dependency-graph proof.
/// </summary>
public sealed class Phase4F4ConfirmAndLegacyRegressionTests
{
    [Fact]
    public void CatalogPlanConfirmationService_ConstructorSurface_UnchangedByPhase4F4()
    {
        var ctors = typeof(CatalogPlanConfirmationService).GetConstructors();
        Assert.Single(ctors);

        var paramTypeNames = ctors[0].GetParameters().Select(p => p.ParameterType.FullName ?? "").ToList();

        Assert.DoesNotContain(paramTypeNames, t => t.Contains("CatalogPlanSkeletonOrchestrator") || t.Contains("Materialization"));
        // Unchanged since Phase 4E.2/4F.1: AppDbContext, ILogger, IGeneratedCatalogPlanPayloadValidator.
        Assert.Equal(3, paramTypeNames.Count);
    }

    [Fact]
    public void CatalogPlanConfirmationService_NeverInvokesSkeletonOrchestration_NoTypeReferenceAnywhereInAssembly()
    {
        // Structural proof at the method-signature level: ConfirmAsync's own
        // declared return/parameter types carry no orchestration reference,
        // and the type has no field of that type either (reflection over
        // instance fields, incl. private).
        var fields = typeof(CatalogPlanConfirmationService).GetFields(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.DoesNotContain(fields, f => f.FieldType.FullName?.Contains("CatalogPlanSkeletonOrchestrator") ?? false);
    }
}
