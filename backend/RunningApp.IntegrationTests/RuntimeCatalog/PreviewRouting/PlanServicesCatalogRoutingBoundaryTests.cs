using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.PlanGeneration;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.1 / 4E.2 — boundary guarantees for
/// PlanServices catalog routing and confirmation dispatch.
///
/// Phase 4E.1 test (#1): pilot catalog failure never calls into the legacy SQL
/// generation engine (proven with a spy engine that fails the test if invoked).
///
/// Phase 4E.2 test (#2): ConfirmPlanAsync dispatches CATALOG-sourced previews
/// using the stored GenerationSource field — NOT by re-running IGenerationRouteDecider.
/// A preview row whose PreviewPayloadJson contains generation_source="CATALOG"
/// is routed to CatalogPlanConfirmationService, which throws
/// PlanPreviewNotFoundException (typed, not the old ConflictAppException)
/// when the previewId is not found for the requesting user. This confirms the
/// dispatch path is active and the old Phase 4E.1 ConflictAppException guard
/// no longer fires for catalog-sourced previews.
/// </summary>
public sealed class PlanServicesCatalogRoutingBoundaryTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class CountingPlanGenerationEngine : IPlanGenerationEngine
    {
        public bool WasCalled { get; private set; }

        public Task<TemplateSelectionResult> SelectTemplateAsync(GeneratePreviewRequest request, CancellationToken ct = default)
        {
            WasCalled = true;
            throw new PlanTemplateNotAvailableException("No exact legacy template is available for this request.");
        }
    }

    // Phase 4F.8.2: the real v10 candidate is DRAFT and activation defaults disabled,
    // so a real-user pilot-shaped request must not invoke the dark catalog path.
    [Fact]
    public async Task GeneratePreviewAsync_DefaultDisabledDraftPilot_UsesLegacyExactTemplatePath()
    {
        await using var context = NewContext();
        var spyEngine = new CountingPlanGenerationEngine();
        var service = TestPlanServicesFactory.Create(context, spyEngine);

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

        Assert.True(spyEngine.WasCalled);
        Assert.Empty(context.PlanPreviews);
    }

    // Phase 4E.2 test: catalog dispatch uses stored GenerationSource, not IGenerationRouteDecider.
    // A preview whose PreviewPayloadJson contains "generation_source":"CATALOG" is dispatched to
    // CatalogPlanConfirmationService. That service performs ownership validation and throws
    // PlanPreviewForbiddenException when the requesting user is not the owner (the stored
    // InternalUserId differs from the internalUserId argument). This proves dispatch is active.
    // The old Phase 4E.1 ConflictAppException ("catalog pilot flow, not yet confirmable") no
    // longer fires — it has been replaced by the real catalog confirm flow.
    [Fact]
    public async Task ConfirmPlanAsync_CatalogSourcedPreview_DispatchesToCatalogConfirmService_NotOldConflictGuard()
    {
        await using var context = NewContext();
        var service = TestPlanServicesFactory.Create(context);
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var previewId = Guid.NewGuid();

        // A minimal catalog-sourced preview payload: just enough to make
        // IsCatalogSourcedPreview return true (has "generation_source":"CATALOG").
        var catalogPayload = JsonSerializer.Serialize(new
        {
            generation_source = "CATALOG",
            candidate_key = "TEN_K__4D__INTERMEDIATE",
            candidate_version = 10,
        });

        context.PlanPreviews.Add(new PlanPreview
        {
            Id = previewId,
            InternalUserId = ownerId,   // owner is ownerId, not differentUserId
            TemplateId = "TEN_K__4D__INTERMEDIATE",
            RequestPayloadJson = "{}",
            PreviewPayloadJson = catalogPayload,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow,
        });
        await context.SaveChangesAsync();

        // Confirming as a different user must throw PlanPreviewForbiddenException (HTTP 403)
        // from CatalogPlanConfirmationService.ConfirmAsync — NOT the old ConflictAppException
        // ("catalog pilot flow, not yet confirmable") from Phase 4E.1.
        // This is the proof that dispatch is active and ownership is validated.
        var ex = await Assert.ThrowsAsync<PlanPreviewForbiddenException>(() =>
            service.ConfirmPlanAsync(differentUserId, new ConfirmPlanRequest { PreviewId = previewId }));

        Assert.Contains(previewId.ToString(), ex.Message);
        Assert.Empty(context.TrainingPlans);
    }
}
