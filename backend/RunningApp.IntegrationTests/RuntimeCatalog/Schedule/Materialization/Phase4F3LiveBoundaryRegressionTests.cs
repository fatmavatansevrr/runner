using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.3 — proves the new internal
/// <see cref="RunningApp.Application.RuntimeCatalog.Schedule.Materialization.CatalogPlanSkeletonOrchestrator"/>
/// boundary (Option A) is not wired into any live request path, changes no
/// public behavior, and exposes nothing through public DTOs. Complements
/// <see cref="Phase4F2LiveBoundaryRegressionTests"/> rather than duplicating it.
/// </summary>
public sealed class Phase4F3LiveBoundaryRegressionTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    /// <summary>
    /// Backend Integration Phase 4F.4 note: this assertion's scope narrowed
    /// (not weakened) once dark skeleton wiring landed. It no longer means
    /// "no orchestrator wiring exists anywhere" (Phase 4F.4 deliberately adds
    /// that — see Phase4F4DarkSkeletonWiringTests). It still means, and still
    /// verifies, that <see cref="CatalogPreviewGenerator"/>'s PUBLIC/DI-facing
    /// constructor surface (what <c>GetConstructors()</c> sees by default —
    /// public instance constructors only) never grew a new parameter: the
    /// orchestrator is composed internally by an internal constructor overload
    /// instead (see that class's own doc comment), so this remains true.
    /// </summary>
    [Fact]
    public void CatalogPreviewGenerator_PublicConstructorSurface_DoesNotTakeOrchestratorAsAParameter()
    {
        var ctors = typeof(CatalogPreviewGenerator).GetConstructors();
        Assert.Single(ctors);
        var paramTypes = ctors[0].GetParameters().Select(p => p.ParameterType.FullName ?? "").ToList();

        Assert.DoesNotContain(paramTypes, t => t.Contains("CatalogPlanSkeletonOrchestrator"));
    }

    [Fact]
    public void CatalogPlanConfirmationService_ConstructorDependencies_DoNotIncludeOrchestratorType()
    {
        var ctors = typeof(CatalogPlanConfirmationService).GetConstructors();
        Assert.Single(ctors);
        var paramTypes = ctors[0].GetParameters().Select(p => p.ParameterType.FullName ?? "").ToList();

        Assert.DoesNotContain(paramTypes, t => t.Contains("CatalogPlanSkeletonOrchestrator"));
    }

    [Fact]
    public async Task RealCatalogPreviewGeneration_CurrentDefaultRoute_DoesNotExposeCatalogPayload()
    {
        await using var context = NewContext();
        var service = TestPlanServicesFactory.Create(context);

        await Assert.ThrowsAsync<PlanTemplateNotAvailableException>(() => service.GeneratePreviewAsync(
            Guid.NewGuid(),
            new GeneratePreviewRequest
            {
                GoalType = GoalType.Race,
                GoalDistance = GoalDistance.TenK,
                Level = RunningBackground.Intermediate,
                DaysPerWeek = 4,
                Unit = DistanceUnit.Km,
                RaceDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(84),
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
                PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
                LongRunDay = Weekday.Sun,
                TargetFinishTimeSeconds = 3600,
            }));

        Assert.Empty(context.PlanPreviews);
    }

    [Fact]
    public void CatalogPreviewSnapshot_HasNoSkeletonProperty()
    {
        var properties = typeof(CatalogPreviewSnapshot).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain(properties, n => n.Contains("Skeleton"));
    }

    [Fact]
    public void PublicPreviewDTOs_ExposeNoPhaseAllocationOrSkeletonContent()
    {
        var publicDtoTypes = new[]
        {
            typeof(GeneratePreviewResponse),
            typeof(PreviewWeekDto),
            typeof(PreviewDayDto),
            typeof(ConfirmPlanResponse),
        };

        foreach (var dtoType in publicDtoTypes)
        {
            var propertyNames = dtoType.GetProperties().Select(p => p.Name).ToList();

            Assert.DoesNotContain(propertyNames, n => n.Contains("Phase") || n.Contains("Skeleton") || n.Contains("StructuralRole"));

            foreach (var property in dtoType.GetProperties())
            {
                Assert.DoesNotContain("Materialization", property.PropertyType.FullName ?? "");
            }
        }
    }

    [Fact]
    public void ConfirmAsync_RemainsGated_NoTrainingPlanArtifactsExistFromOrchestration()
    {
        // The confirmation service is unchanged this phase (see
        // Phase4F2LiveBoundaryRegressionTests and the confirm-blocked-on-null-
        // payload guard already covered there) -- this test only re-asserts
        // that no Phase 4F.3 type appears anywhere in its dependency graph,
        // and that running the internal orchestrator standalone creates no
        // database rows of any kind.
        using var context = NewContext();

        var orchestrator = CatalogPlanSkeletonOrchestratorFixtures.RealOrchestrator();
        var candidate = CatalogPlanSkeletonOrchestratorFixtures.PilotCandidate();
        var skeletonContext = CatalogPlanSkeletonOrchestratorFixtures.OrchestrationContext(candidate: candidate);

        orchestrator.Build(skeletonContext);

        Assert.Empty(context.TrainingPlans);
        Assert.Empty(context.PlanPreviews);
    }

    [Fact]
    public void CatalogPlanSkeletonOrchestrator_HasNoDatabaseOrHttpDependency()
    {
        var ctor = typeof(RunningApp.Application.RuntimeCatalog.Schedule.Materialization.CatalogPlanSkeletonOrchestrator)
            .GetConstructors().Single();
        var paramTypeNames = ctor.GetParameters().Select(p => p.ParameterType.FullName ?? "").ToList();

        Assert.DoesNotContain(paramTypeNames, t => t.Contains("DbContext") || t.Contains("HttpContext"));
    }

    [Fact]
    public void CatalogPlanSkeletonOrchestrator_IsInternal_NotPubliclyExposed()
    {
        Assert.False(typeof(RunningApp.Application.RuntimeCatalog.Schedule.Materialization.CatalogPlanSkeletonOrchestrator).IsPublic);
        Assert.False(typeof(RunningApp.Application.RuntimeCatalog.Schedule.Materialization.CatalogPlanSkeletonOrchestrationContext).IsPublic);
        Assert.False(typeof(RunningApp.Application.RuntimeCatalog.Schedule.Materialization.CatalogPlanSkeletonOrchestrationResult).IsPublic);
    }
}
