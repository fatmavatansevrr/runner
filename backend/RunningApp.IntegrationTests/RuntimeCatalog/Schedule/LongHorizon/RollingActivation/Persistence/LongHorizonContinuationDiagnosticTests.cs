using Microsoft.EntityFrameworkCore;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2C -- root-cause resolution proof for the Runway continuation
/// window-advancement defect Phase 4L.2B discovered but did not fix.
///
/// Root cause: LongHorizonLockedCoreWeekOneTarget.LockedForActivatedRunwayWeekRange
/// is a (int StartGlobalWeek, int EndGlobalWeek) ValueTuple. System.Text.Json's
/// default JsonSerializerOptions only serialize/deserialize properties, not
/// public fields -- and ValueTuple exposes Item1/Item2 as public fields. The
/// Phase 4L.2A full-fidelity JSON round-trip therefore silently zeroed this
/// tuple on every restart, so the second continuation's reuse-validation path
/// (ImmutablePreparationRunwayPrescriptionValidator.Validate, Step 3) always
/// threw PreparationRunwayTargetLockScopeViolationException ("range (0-0) must
/// exactly cover ... (2-9)"), which LongHorizonRollingJitActivationRuntime's
/// outer catch (LongHorizonRollingContractException) silently converted to the
/// generic JitSegmentTransitionInfeasible reason -- masking the real cause.
///
/// Fixed via LongHorizonRollingActivationPersistenceAdapter.FullFidelityJsonOptions
/// (IncludeFields = true), applied symmetrically to every serialize call in
/// LongHorizonRollingActivationPersistenceAdapter and every matching deserialize
/// call in LongHorizonRollingStateReconstructionService. No numeric, calendar,
/// direction, evidence, target-lock, or slice formula was touched; no
/// prescription regeneration workaround was introduced.
/// </summary>
public sealed class LongHorizonRunwayContinuationWindowAdvancementRootCauseTests
{
    /// <summary>
    /// Direct reproduction of the defect's exact failure surface: reconstructing
    /// a persisted Runway prescription/target-lock after restart and validating
    /// it for reuse must now succeed, not throw PreparationRunwayTargetLockScopeViolationException.
    /// </summary>
    [Fact]
    public async Task ReconstructedPrescription_PassesReuseValidationAfterRestart()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var date1 = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var firstEntry = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, date1, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, firstEntry.Outcome);

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var snapshot = await repo.LoadRestartSnapshotAsync(planStateId);
        var prescription = snapshot!.DarkState.RunwayPrescription!;
        var targetLock = snapshot.DarkState.RunwayTargetLock!;

        // The exact field that silently zeroed under the pre-fix default JSON options.
        Assert.Equal((2, 9), targetLock.LockedForActivatedRunwayWeekRange);
        Assert.Equal(2, prescription.StartGlobalWeek);
        Assert.Equal(9, prescription.EndGlobalWeek);

        // Must not throw -- this is the exact validator call that failed pre-fix.
        RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway
            .ImmutablePreparationRunwayPrescriptionValidator.Validate(prescription);
    }

    /// <summary>
    /// Part 12 -- three-call progression proof: GE entry (call 0, done during
    /// InitializePlanAsync) -> first Runway continuation (call 1, weeks 2-5) ->
    /// second Runway continuation (call 2, weeks 6-9, the exact call that was
    /// blocked pre-fix) -> Runway-to-Core boundary continuation (call 3, weeks
    /// 10-13). Each call opens a brand-new AppDbContext (real restart), and
    /// each must advance the durable lifecycle boundary strictly forward.
    /// </summary>
    [Fact]
    public async Task ThreeCallProgression_AdvancesThroughRunwayIntoCoreAcrossRestarts()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var date1 = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);

        var call1 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, date1, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call1.Outcome);
        Assert.Equal((2, 5), (call1.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek, call1.Snapshot.DarkState.CurrentWindow.EndGlobalWeek));

        var date2 = date1.AddDays(28);
        var call2 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, call1.Snapshot.DarkState.CurrentWindow, date2, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call2.Outcome);
        Assert.Equal((6, 9), (call2.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek, call2.Snapshot.DarkState.CurrentWindow.EndGlobalWeek));

        var date3 = date2.AddDays(28);
        var call3 = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, call2.Snapshot.DarkState.CurrentWindow, date3, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call3.Outcome);
        Assert.Equal(10, call3.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek);
        Assert.True(call3.Snapshot.DarkState.CurrentWindow.EndGlobalWeek >= 10);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var plan = await verify.LongHorizonRollingPlanStates.SingleAsync(p => p.Id == planStateId);
        Assert.Equal(call3.Snapshot.DarkState.CurrentWindow.StartGlobalWeek, plan.CurrentWindowStartWeek);
        Assert.Equal(call3.Snapshot.DarkState.CurrentWindow.EndGlobalWeek, plan.CurrentWindowEndWeek);

        var activationRows = await verify.LongHorizonActivationWindowRecords.Where(a => a.PlanStateId == planStateId).OrderBy(a => a.StartGlobalWeek).ToListAsync();
        var distinctRanges = activationRows.Select(a => (a.StartGlobalWeek, a.EndGlobalWeek)).Distinct().ToList();
        Assert.Contains((2, 5), distinctRanges);
        Assert.Contains((6, 9), distinctRanges);
        Assert.Contains(distinctRanges, r => r.StartGlobalWeek == 10);
    }
}
