using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.2 — proves the new Phase 4F.2 materializer
/// types (<see cref="RunningApp.Application.RuntimeCatalog.Schedule.Materialization"/>)
/// are not wired into any live request path. Live preview generation and
/// catalog confirm behave identically to Phase 4F.1 — this phase adds only a
/// standalone, dependency-free materializer, never invoked by production
/// code as of this phase.
/// </summary>
public sealed class Phase4F2LiveBoundaryRegressionTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void CatalogPreviewGenerator_ConstructorDependencies_DoNotIncludeAnyMaterializationType()
    {
        var ctors = typeof(CatalogPreviewGenerator).GetConstructors();
        Assert.Single(ctors);
        var paramTypes = ctors[0].GetParameters().Select(p => p.ParameterType.FullName ?? "").ToList();

        Assert.DoesNotContain(paramTypes, t => t.Contains("Materialization"));
    }

    [Fact]
    public void CatalogPlanConfirmationService_ConstructorDependencies_DoNotIncludeAnyMaterializationType()
    {
        var ctors = typeof(CatalogPlanConfirmationService).GetConstructors();
        Assert.Single(ctors);
        var paramTypes = ctors[0].GetParameters().Select(p => p.ParameterType.FullName ?? "").ToList();

        Assert.DoesNotContain(paramTypes, t => t.Contains("Materialization"));
    }

    [Fact]
    public async Task RealCatalogPreviewGeneration_CurrentDefaultRoute_DoesNotExposeCatalogPayload()
    {
        // Phase 4F.8.2 keeps the repository non-live: v10 is DRAFT and
        // CatalogLivePilot activation defaults disabled. The request therefore
        // reaches the existing exact legacy template boundary and never exposes
        // a catalog payload.
        await using var context = NewContext();
        var service = TestPlanServicesFactory.Create(context);

        await Assert.ThrowsAsync<PlanTemplateNotAvailableException>(() => service.GeneratePreviewAsync(
            Guid.NewGuid(),
            new RunningApp.Application.DTOs.Plan.GeneratePreviewRequest
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
    public void GeneratedCatalogPlanSkeleton_IsNeverAssignableTo_GeneratedPreviewPlanPayloadType()
    {
        // Structural proof that the skeleton cannot be mistaken for, or
        // accidentally assigned into, CatalogPreviewSnapshot.GeneratedPreviewPlanPayload
        // (which remains typed exclusively as the Phase 4F.1 final contract).
        var payloadPropertyType = typeof(CatalogPreviewSnapshot)
            .GetProperty(nameof(CatalogPreviewSnapshot.GeneratedPreviewPlanPayload))!
            .PropertyType;

        Assert.False(payloadPropertyType.IsAssignableFrom(
            typeof(RunningApp.Application.RuntimeCatalog.Schedule.Materialization.GeneratedCatalogPlanSkeleton)));
    }
}
