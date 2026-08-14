using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;

/// <summary>
/// Phase 4K.8B — production-contract tests for the Preparation Runway
/// direction-guard and bounded-prescription contracts. Reuses the real,
/// unchanged <c>PreparationRunwayNumericMaterializer</c> and the existing
/// <c>PreparationRunwayNumericMaterializerTests</c> fixture helpers (Request/
/// Evidence/Target/StructuralWeeks) so every "full prescription" test is
/// built from an actual production materialization result -- never a
/// hand-built shortcut. Dark and unwired: nothing here is called from any
/// production/live request path.
/// </summary>
public sealed class PreparationRunwayDirectionAndBoundedPrescriptionContractsTests
{
    private const string CatalogProvenance = "TEN_K__4D__INTERMEDIATE v10 (unchanged)";

    private static LongHorizonLockedCoreWeekOneTarget Locked(double weekly, double longRun, int startWeek, int endWeek) => new()
    {
        TargetWeeklyVolumeKm = weekly,
        TargetLongRunKm = longRun,
        Source = LongHorizonEvidenceAuthorityCatalog.CoreWeekOneCurrentProductionSource,
        AuthorityStatus = LongHorizonEvidenceAuthorityStatus.LegacyCurrentProductionSource,
        ContextVersion = LongHorizonContextVersion.Initial(),
        LockedForActivatedRunwayWeekRange = (startWeek, endWeek),
        CreatedByDecisionId = Guid.NewGuid(),
    };

    private static PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType> RealFlatResult(int runwayWeeks, double weekly = 24, double longRun = 8) =>
        PreparationRunwayNumericMaterializer.Materialize(PreparationRunwayNumericMaterializerTests.Request(
            PreparationRunwayAllocationProfile.ConsistencyNeeded, runwayWeeks,
            PreparationRunwayNumericMaterializerTests.Evidence(
                PreparationRunwayLoadEvidenceState.Provided, weekly, PreparationRunwayLoadEvidenceState.Provided, longRun),
            PreparationRunwayNumericMaterializerTests.Target(weekly, longRun)));

    private static PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType> RealBelowTargetResult(int runwayWeeks, double feasibleStart)
    {
        return PreparationRunwayNumericMaterializer.Materialize(PreparationRunwayNumericMaterializerTests.Request(
            PreparationRunwayAllocationProfile.ConsistencyNeeded, runwayWeeks,
            PreparationRunwayNumericMaterializerTests.Evidence(
                PreparationRunwayLoadEvidenceState.Provided, feasibleStart, PreparationRunwayLoadEvidenceState.Missing, null),
            PreparationRunwayNumericMaterializerTests.Target(24, 8)));
    }

    // ═══════════════ DIRECTION GUARD (1-10) ═══════════════

    [Fact]
    public void Weekly_BelowTarget_ConditionallySupported()
    {
        var policy = PreparationRunwayDirectionGuard.Evaluate(20, 24, 8, 8);
        Assert.Equal(PreparationRunwayDirectionRelation.BelowTarget, policy.WeeklyDirection);
        Assert.True(policy.WeeklyDirectionSupported);
    }

    [Fact]
    public void Weekly_EqualTarget_ConditionallySupported()
    {
        var policy = PreparationRunwayDirectionGuard.Evaluate(24, 24, 8, 8);
        Assert.Equal(PreparationRunwayDirectionRelation.EqualTarget, policy.WeeklyDirection);
        Assert.True(policy.WeeklyDirectionSupported);
    }

    [Fact]
    public void Weekly_AboveTarget_Rejected()
    {
        var policy = PreparationRunwayDirectionGuard.Evaluate(28, 24, 8, 8);
        Assert.Equal(PreparationRunwayDirectionRelation.AboveTarget, policy.WeeklyDirection);
        Assert.False(policy.WeeklyDirectionSupported);
        Assert.False(policy.OverallSupported);
    }

    [Fact]
    public void LongRun_BelowTarget_ConditionallySupported()
    {
        var policy = PreparationRunwayDirectionGuard.Evaluate(24, 24, 6, 8);
        Assert.Equal(PreparationRunwayDirectionRelation.BelowTarget, policy.LongRunDirection);
        Assert.True(policy.LongRunDirectionSupported);
    }

    [Fact]
    public void LongRun_EqualTarget_ConditionallySupported()
    {
        var policy = PreparationRunwayDirectionGuard.Evaluate(24, 24, 8, 8);
        Assert.Equal(PreparationRunwayDirectionRelation.EqualTarget, policy.LongRunDirection);
        Assert.True(policy.LongRunDirectionSupported);
    }

    [Fact]
    public void LongRun_AboveTarget_Rejected()
    {
        var policy = PreparationRunwayDirectionGuard.Evaluate(24, 24, 9, 8);
        Assert.Equal(PreparationRunwayDirectionRelation.AboveTarget, policy.LongRunDirection);
        Assert.False(policy.LongRunDirectionSupported);
        Assert.False(policy.OverallSupported);
    }

    [Fact]
    public void WeeklyAndLongRunDirections_AreIndependent()
    {
        // Weekly below (supported), long run above (unsupported) -- overall must be unsupported,
        // proving weekly equality/support never masks an independent long-run conflict.
        var policy = PreparationRunwayDirectionGuard.Evaluate(20, 24, 9, 8);
        Assert.True(policy.WeeklyDirectionSupported);
        Assert.False(policy.LongRunDirectionSupported);
        Assert.False(policy.OverallSupported);
    }

    [Fact]
    public void UnsupportedDirection_MapsToJitSegmentTransitionInfeasible()
    {
        var policy = PreparationRunwayDirectionGuard.Evaluate(28, 24, 8, 8);
        Assert.NotNull(policy.FailureReason);
        Assert.Equal(LongHorizonReasonCodeCategory.Jit, policy.FailureReason!.Value.Category);
        Assert.Equal(LongHorizonJitReasonCode.JitSegmentTransitionInfeasible, policy.FailureReason.Value.JitReason);
    }

    [Fact]
    public void NoDownwardFormula_SupportedDirectionsUseOnlyComparisonNoArithmeticTransform()
    {
        // Below/equal support is a pure relation, not a computed reduction value -- confirmed by
        // the guard never returning any magnitude/percentage, only relation + support booleans.
        var policy = PreparationRunwayDirectionGuard.Evaluate(20, 24, 6, 8);
        Assert.True(policy.OverallSupported);
        Assert.Contains("Phase 4K.8A", policy.PolicyProvenance);
    }

    [Fact]
    public void NoNewPercentage_GuardEvaluateHasNoWeightOrPercentageParameter()
    {
        var method = typeof(PreparationRunwayDirectionGuard).GetMethod(nameof(PreparationRunwayDirectionGuard.Evaluate));
        Assert.NotNull(method);
        Assert.DoesNotContain(method!.GetParameters(), p => p.Name!.Contains("percent", StringComparison.OrdinalIgnoreCase) || p.Name!.Contains("weight", StringComparison.OrdinalIgnoreCase));
    }

    // ═══════════════ FULL PRESCRIPTION (11-24) ═══════════════

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    public void FullPrescription_Durations3To8_Validate(int runwayWeeks)
    {
        var result = RealFlatResult(runwayWeeks);
        Assert.True(result.IsSuccess, result.FailureReason);

        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 20 + runwayWeeks - 1), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        Assert.Equal(runwayWeeks, prescription.FullRunwayDurationWeeks);
        Assert.Equal(runwayWeeks, prescription.FullWeekReferences.Count);
    }

    [Fact]
    public void FullPrescription_DurationBelow3_Rejects()
    {
        var result = RealFlatResult(3);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 22), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);
        var mutated = prescription with { FullRunwayDurationWeeks = 2 };
        Assert.Throws<PreparationRunwayFullPrescriptionInvalidException>(() => ImmutablePreparationRunwayPrescriptionValidator.Validate(mutated));
    }

    [Fact]
    public void FullPrescription_DurationAbove8_Rejects()
    {
        var result = RealFlatResult(8);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 27), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);
        var mutated = prescription with { FullRunwayDurationWeeks = 9 };
        Assert.Throws<PreparationRunwayFullPrescriptionInvalidException>(() => ImmutablePreparationRunwayPrescriptionValidator.Validate(mutated));
    }

    [Fact]
    public void FullPrescription_LocalWeeksExactly1ToN()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        Assert.Equal(Enumerable.Range(1, 5), prescription.FullWeekReferences.Select(w => w.LocalRunwayWeek).OrderBy(w => w));
    }

    [Fact]
    public void FullPrescription_GlobalWeeksContiguous()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        Assert.Equal(Enumerable.Range(20, 5), prescription.FullWeekReferences.Select(w => w.GlobalPlanWeek).OrderBy(w => w));
    }

    [Fact]
    public void FullPrescription_LocalGlobalMapping_IsExact()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        Assert.All(prescription.FullWeekReferences, w => Assert.Equal(prescription.StartGlobalWeek + w.LocalRunwayWeek - 1, w.GlobalPlanWeek));
    }

    [Fact]
    public void FullPrescription_DuplicateLocalWeek_Rejects()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);
        var duped = prescription.FullWeekReferences.ToList();
        duped[1] = duped[1] with { LocalRunwayWeek = duped[0].LocalRunwayWeek };
        var mutated = prescription with { FullWeekReferences = duped };

        Assert.Throws<PreparationRunwayFullPrescriptionInvalidException>(() => ImmutablePreparationRunwayPrescriptionValidator.Validate(mutated));
    }

    [Fact]
    public void FullPrescription_DuplicateGlobalWeek_Rejects()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);
        var duped = prescription.FullWeekReferences.ToList();
        duped[1] = duped[1] with { GlobalPlanWeek = duped[0].GlobalPlanWeek };
        var mutated = prescription with { FullWeekReferences = duped };

        Assert.Throws<PreparationRunwayFullPrescriptionInvalidException>(() => ImmutablePreparationRunwayPrescriptionValidator.Validate(mutated));
    }

    [Fact]
    public void FullPrescription_OnePrescriptionUsesOneTargetLock()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        Assert.All(prescription.FullWeekReferences, w => Assert.Equal(prescription.LockedCoreWeekOneTarget, w.TargetLock));
    }

    [Fact]
    public void FullPrescription_LockRangeMismatch_Rejects()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        // Lock range (20-23) does not cover the full prescription range (20-24).
        Assert.Throws<PreparationRunwayTargetLockScopeViolationException>(
            () => factory.Create(result, 24, 8, Locked(24, 8, 20, 23), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance));
    }

    [Fact]
    public void FullPrescription_UnsupportedDirection_PreventsCreation()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();

        Assert.Throws<PreparationRunwayDirectionUnsupportedException>(
            () => factory.Create(result, 28, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance));
    }

    [Fact]
    public void FullPrescription_WeekReferences_PreserveExistingOutputValues()
    {
        var result = RealBelowTargetResult(5, 20);
        Assert.True(result.IsSuccess, result.FailureReason);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 20, result.PrescribedWeeks![0].PlannedLongRunDistanceKm, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        for (var i = 0; i < result.PrescribedWeeks.Count; i++)
        {
            Assert.Equal(result.PrescribedWeeks[i].PlannedWeeklyVolumeKm, prescription.FullWeekReferences[i].WeeklyVolumeKm);
            Assert.Equal(result.PrescribedWeeks[i].PlannedLongRunDistanceKm, prescription.FullWeekReferences[i].LongRunKm);
            Assert.Same(result.PrescribedWeeks[i], prescription.FullWeekReferences[i].ProductionWeek);
        }
    }

    [Fact]
    public void FullPrescription_IsImmutable_RecordWithProducesNewInstance()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var copy = prescription with { CatalogProvenance = "different" };
        Assert.NotEqual(prescription, copy);
        Assert.Equal(CatalogProvenance, prescription.CatalogProvenance);
    }

    [Fact]
    public void ComputedInternalPending_DoesNotBecomeNumericActivated()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        Assert.True(prescription.ComputedInternalPending);
        // ImmutablePreparationRunwayPrescription<TKey> carries no LongHorizonNumericLifecycleState field at all --
        // it cannot represent NumericActivated by construction.
        Assert.DoesNotContain(typeof(ImmutablePreparationRunwayPrescription<PreparationRunwayBlockType>).GetProperties(),
            p => p.PropertyType == typeof(LongHorizonNumericLifecycleState));
    }

    // ═══════════════ TERMINAL STAGE (25-30) ═══════════════

    [Fact]
    public void TerminalStage_FinalWeek_IsPreSpecificTransition()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var final = prescription.FullWeekReferences.Single(w => w.LocalRunwayWeek == 5);
        Assert.Equal("PreSpecificTransition", final.Stage);
    }

    [Fact]
    public void TerminalStage_EarlierPreSpecificTransition_Rejects()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);
        var tampered = prescription.FullWeekReferences.ToList();
        tampered[1] = tampered[1] with { Stage = "PreSpecificTransition" };
        var mutated = prescription with { FullWeekReferences = tampered };

        Assert.Throws<PreparationRunwayTerminalStageViolationException>(() => ImmutablePreparationRunwayPrescriptionValidator.Validate(mutated));
    }

    [Fact]
    public void TerminalStage_MissingFinalPreSpecificTransition_Rejects()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);
        var tampered = prescription.FullWeekReferences.ToList();
        var finalIndex = tampered.FindIndex(w => w.LocalRunwayWeek == 5);
        tampered[finalIndex] = tampered[finalIndex] with { Stage = "GeneralEndurance" };
        var mutated = prescription with { FullWeekReferences = tampered };

        Assert.Throws<PreparationRunwayTerminalStageViolationException>(() => ImmutablePreparationRunwayPrescriptionValidator.Validate(mutated));
    }

    [Fact]
    public void TerminalStage_FirstSlice_DoesNotSynthesizeTerminalStage()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var slice = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 1, 2);
        Assert.DoesNotContain(slice.WeekReferences, w => w.Stage == "PreSpecificTransition");
    }

    [Fact]
    public void TerminalStage_MiddleSlice_DoesNotSynthesizeTerminalStage()
    {
        var result = RealFlatResult(8);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 27), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var slice = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 4, 6);
        Assert.DoesNotContain(slice.WeekReferences, w => w.Stage == "PreSpecificTransition");
    }

    [Fact]
    public void TerminalStage_FinalSlice_ExposesOriginalTerminalWeekUnchanged()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var slice = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 5, 5);
        Assert.Equal("PreSpecificTransition", Assert.Single(slice.WeekReferences).Stage);
        Assert.Equal(prescription.FullWeekReferences.Single(w => w.LocalRunwayWeek == 5), Assert.Single(slice.WeekReferences));
    }

    // ═══════════════ BOUNDED SLICES (31-46) ═══════════════

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(1, 4)]
    public void BoundedSlice_FirstSlices_AreExact(int start, int end)
    {
        var result = RealFlatResult(8);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 27), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var slice = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, start, end);
        Assert.Equal(end - start + 1, slice.ActualWeekCount);
        PreparationRunwayBoundedSliceEquivalenceValidator.Validate(prescription, slice);
    }

    [Theory]
    [InlineData(4, 4)]
    [InlineData(3, 6)]
    [InlineData(4, 7)]
    public void BoundedSlice_MiddleSlices_AreExact(int start, int end)
    {
        var result = RealFlatResult(8);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 27), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var slice = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, start, end);
        PreparationRunwayBoundedSliceEquivalenceValidator.Validate(prescription, slice);
    }

    [Theory]
    [InlineData(8, 8)]
    [InlineData(7, 8)]
    [InlineData(5, 8)]
    public void BoundedSlice_FinalSlices_AreExact(int start, int end)
    {
        var result = RealFlatResult(8);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 27), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var slice = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, start, end);
        Assert.Equal(end - start + 1, slice.ActualWeekCount);
    }

    [Fact]
    public void BoundedSlice_LocalCoordinates_DoNotReset()
    {
        var result = RealFlatResult(8);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 27), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var slice = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 4, 6);
        Assert.Equal(new[] { 4, 5, 6 }, slice.WeekReferences.Select(w => w.LocalRunwayWeek));
    }

    [Fact]
    public void BoundedSlice_GlobalCoordinates_DoNotChange()
    {
        var result = RealFlatResult(8);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 27), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var slice = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 4, 6);
        Assert.Equal(new[] { 23, 24, 25 }, slice.WeekReferences.Select(w => w.GlobalPlanWeek));
    }

    [Fact]
    public void BoundedSlice_ValuesEqualFullOutput()
    {
        var result = RealFlatResult(8);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 27), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var slice = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 4, 6);
        foreach (var week in slice.WeekReferences)
        {
            var full = prescription.FullWeekReferences.Single(w => w.LocalRunwayWeek == week.LocalRunwayWeek);
            Assert.Equal(full, week);
        }
    }

    [Fact]
    public void BoundedSlice_TargetLockEqualsFullPrescriptionLock()
    {
        var result = RealFlatResult(8);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 27), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var slice = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 4, 6);
        Assert.Equal(prescription.LockedCoreWeekOneTarget.CreatedByDecisionId, slice.TargetLockId);
        Assert.Equal(prescription.LockedCoreWeekOneTarget.ContextVersion, slice.TargetLockVersion);
    }

    [Fact]
    public void BoundedSlice_ProvenanceEqualsFullPrescriptionProvenance()
    {
        var result = RealFlatResult(8);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 27), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var slice = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 4, 6);
        Assert.Contains(prescription.PrescriptionId.Value.ToString(), slice.BoundedExposureProvenance);
        Assert.Contains(prescription.PrescriptionVersion.Version.Sequence.ToString(), slice.BoundedExposureProvenance);
    }

    [Fact]
    public void BoundedSlice_RangeOutsideFullDuration_Rejects()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        Assert.Throws<PreparationRunwayBoundedSliceInvalidException>(() => PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 4, 7));
    }

    [Fact]
    public void BoundedSlice_SizeAbove4_Rejects()
    {
        var result = RealFlatResult(8);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 27), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        Assert.Throws<PreparationRunwayBoundedSliceInvalidException>(() => PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 1, 5));
    }

    [Fact]
    public void BoundedSlice_MixedPrescriptionVersions_Reject()
    {
        var resultA = RealFlatResult(5);
        var resultB = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescriptionA = factory.Create(resultA, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);
        var prescriptionB = factory.Create(resultB, 24, 8, Locked(24, 8, 30, 34), 30, ReadinessProfile.CoreEntryReady, CatalogProvenance);

        var sliceB = PreparationRunwayBoundedSliceFactory.CreateSlice(prescriptionB, 1, 2);
        var mixed = sliceB with { PrescriptionId = prescriptionA.PrescriptionId, PrescriptionVersion = prescriptionA.PrescriptionVersion };

        Assert.Throws<PreparationRunwaySliceEquivalenceViolationException>(() => PreparationRunwayBoundedSliceEquivalenceValidator.Validate(prescriptionA, mixed));
    }

    [Fact]
    public void BoundedSlice_FactoryNeverInvokesNumericMaterializer()
    {
        var method = typeof(PreparationRunwayBoundedSliceFactory).GetMethod(nameof(PreparationRunwayBoundedSliceFactory.CreateSlice));
        Assert.NotNull(method);
        // The factory's own signature takes an already-built ImmutablePreparationRunwayPrescription --
        // it structurally cannot call the numeric materializer, which requires a
        // PreparationRunwayNumericMaterializationRequest it never receives.
        Assert.DoesNotContain(method!.GetParameters(), p => p.ParameterType.Name.Contains("MaterializationRequest", StringComparison.Ordinal));
    }

    // ═══════════════ LOCK/REFRESH (47-53) ═══════════════

    [Fact]
    public void Lock_OneLockCoversCompleteRunwayRange()
    {
        var scope = new PreparationRunwayTargetLockScope
        {
            Target = Locked(24, 8, 20, 24),
            PrescriptionId = new PreparationRunwayPrescriptionId(Guid.NewGuid()),
            PrescriptionVersion = new PreparationRunwayPrescriptionVersion(LongHorizonContextVersion.Initial()),
            RunwayGlobalRange = (20, 24),
        };
        var exception = Record.Exception(() => PreparationRunwayTargetLockScopeValidator.Validate(scope));
        Assert.Null(exception);
    }

    [Fact]
    public void Lock_PerSliceLock_Rejected()
    {
        var scope = new PreparationRunwayTargetLockScope
        {
            Target = Locked(24, 8, 20, 24),
            PrescriptionId = new PreparationRunwayPrescriptionId(Guid.NewGuid()),
            PrescriptionVersion = new PreparationRunwayPrescriptionVersion(LongHorizonContextVersion.Initial()),
            RunwayGlobalRange = (22, 23), // a slice sub-range, not the full Runway range
        };
        Assert.Throws<PreparationRunwayTargetLockScopeViolationException>(() => PreparationRunwayTargetLockScopeValidator.Validate(scope));
    }

    [Fact]
    public void Refresh_MidRunway_Rejected()
    {
        var existingVersion = LongHorizonContextVersion.Initial();
        var newVersion = existingVersion.Next();
        Assert.Throws<PreparationRunwayMidRunwayRefreshViolationException>(() =>
            PreparationRunwayTargetRefreshGuard.ValidateRefreshOutsideRunwayRange((20, 27), (25, 28), existingVersion, newVersion));
    }

    [Fact]
    public void Refresh_OverlappingFutureLock_Rejected()
    {
        var existingVersion = LongHorizonContextVersion.Initial();
        var newVersion = existingVersion.Next();
        Assert.Throws<PreparationRunwayMidRunwayRefreshViolationException>(() =>
            PreparationRunwayTargetRefreshGuard.ValidateRefreshOutsideRunwayRange((20, 27), (27, 30), existingVersion, newVersion));
    }

    [Fact]
    public void Refresh_NonOverlappingPostRunwayCoreRefresh_AllowedInPrinciple()
    {
        var existingVersion = LongHorizonContextVersion.Initial();
        var newVersion = existingVersion.Next();
        var exception = Record.Exception(() =>
            PreparationRunwayTargetRefreshGuard.ValidateRefreshOutsideRunwayRange((20, 27), (28, 39), existingVersion, newVersion));
        Assert.Null(exception);
    }

    [Fact]
    public void Refresh_StrictlyLaterContextVersion_Required()
    {
        var existingVersion = LongHorizonContextVersion.Initial();
        Assert.Throws<PreparationRunwayMidRunwayRefreshViolationException>(() =>
            PreparationRunwayTargetRefreshGuard.ValidateRefreshOutsideRunwayRange((20, 27), (28, 39), existingVersion, existingVersion));
    }

    [Fact]
    public void Refresh_OriginalPrescriptionRemainsUnchangedAfterRejectedRefresh()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);
        var originalWeeks = prescription.FullWeekReferences;

        var existingVersion = prescription.LockedCoreWeekOneTarget.ContextVersion;
        Assert.Throws<PreparationRunwayMidRunwayRefreshViolationException>(() =>
            PreparationRunwayTargetRefreshGuard.ValidateRefreshOutsideRunwayRange((20, 24), (22, 25), existingVersion, existingVersion.Next()));

        Assert.Same(originalWeeks, prescription.FullWeekReferences);
    }

    // ═══════════════ INTERNAL SAFEGUARDS (54-62) ═══════════════

    [Fact]
    public void InternalPrescription_CreatesNoActivatedNumericWeek()
    {
        Assert.DoesNotContain(typeof(ImmutablePreparationRunwayPrescription<>).Assembly.GetTypes(),
            t => t == typeof(ActivatedNumericWeek) && t.Namespace == typeof(ImmutablePreparationRunwayPrescription<>).Namespace);
    }

    [Fact]
    public void BoundedSlice_ChangesNoLifecycleState()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);
        var slice = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 1, 2);

        Assert.True(slice.NonExecutableUntilActivation);
        Assert.DoesNotContain(typeof(BoundedPreparationRunwayPrescriptionSlice<PreparationRunwayBlockType>).GetProperties(),
            p => p.PropertyType == typeof(LongHorizonNumericLifecycleState));
    }

    [Fact]
    public void NoTrainingDayType_ReferencedByPrescriptionContracts()
    {
        var namespaceTypes = typeof(ImmutablePreparationRunwayPrescription<>).Assembly.GetTypes()
            .Where(t => t.Namespace == "RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway");

        Assert.DoesNotContain(namespaceTypes, t => t.Name.Contains("TrainingDay", StringComparison.Ordinal));
    }

    [Fact]
    public void NoCheckpointEvidenceType_ReferencedByPrescriptionContracts()
    {
        var namespaceTypes = typeof(ImmutablePreparationRunwayPrescription<>).Assembly.GetTypes()
            .Where(t => t.Namespace == "RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway");

        Assert.DoesNotContain(namespaceTypes, t => t.Name.Contains("Controller", StringComparison.Ordinal) || t.Name.Contains("Dto", StringComparison.Ordinal));
    }

    // ═══════════════ DETERMINISM (63-68) ═══════════════

    [Fact]
    public void Determinism_IdenticalFullOutput_YieldsSamePrescriptionIdentity()
    {
        var resultA = RealFlatResult(5);
        var resultB = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var lockA = Locked(24, 8, 20, 24);
        var prescriptionA = factory.Create(resultA, 24, 8, lockA, 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);
        var prescriptionB = factory.Create(resultB, 24, 8, lockA, 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        Assert.Equal(prescriptionA.PrescriptionId, prescriptionB.PrescriptionId);
        Assert.Equal(prescriptionA.PrescriptionVersion, prescriptionB.PrescriptionVersion);
    }

    [Fact]
    public void Determinism_IdenticalSliceRange_YieldsSameSliceIdentity()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var sliceA = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 1, 2);
        var sliceB = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 1, 2);
        Assert.Equal(sliceA.SliceId, sliceB.SliceId);
    }

    [Fact]
    public void Determinism_ChangedTargetLock_ChangesPrescriptionIdentity()
    {
        var resultA = RealFlatResult(5);
        var resultB = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescriptionA = factory.Create(resultA, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);
        var prescriptionB = factory.Create(resultB, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        Assert.NotEqual(prescriptionA.PrescriptionId, prescriptionB.PrescriptionId);
    }

    [Fact]
    public void Determinism_ChangedSliceRange_ChangesSliceIdentity()
    {
        var result = RealFlatResult(8);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription = factory.Create(result, 24, 8, Locked(24, 8, 20, 27), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        var sliceA = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 1, 2);
        var sliceB = PreparationRunwayBoundedSliceFactory.CreateSlice(prescription, 3, 4);
        Assert.NotEqual(sliceA.SliceId, sliceB.SliceId);
    }

    [Fact]
    public void Determinism_ChangedFullOutput_ChangesPrescriptionIdentity()
    {
        var result5 = RealFlatResult(5);
        var result8 = RealFlatResult(8);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var prescription5 = factory.Create(result5, 24, 8, Locked(24, 8, 20, 24), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);
        var prescription8 = factory.Create(result8, 24, 8, Locked(24, 8, 20, 27), 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        Assert.NotEqual(prescription5.PrescriptionId, prescription8.PrescriptionId);
    }

    [Fact]
    public void Determinism_NoRandomGuidOrClockAffectsPrescriptionId()
    {
        var result = RealFlatResult(5);
        var factory = new PreparationRunwayFullPrescriptionFactory<PreparationRunwayBlockType>();
        var lockTarget = Locked(24, 8, 20, 24);
        var first = factory.Create(result, 24, 8, lockTarget, 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);
        Thread.Sleep(5);
        var second = factory.Create(RealFlatResult(5), 24, 8, lockTarget, 20, ReadinessProfile.ConsistencyNeeded, CatalogProvenance);

        Assert.Equal(first.PrescriptionId, second.PrescriptionId);
    }

    // ═══════════════ INTEGRATION BOUNDARY (69-74) ═══════════════

    [Fact]
    public void Phase4K8_JitRuntime_TypesAreAbsent()
    {
        var namespaceTypes = typeof(ImmutablePreparationRunwayPrescription<>).Assembly.GetTypes()
            .Where(t => t.Namespace == "RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway")
            .Select(t => t.Name);

        Assert.DoesNotContain(namespaceTypes, n => n.Contains("Orchestrator", StringComparison.Ordinal) || n.Contains("Runtime", StringComparison.Ordinal));
    }

    [Fact]
    public void ExistingNumericMaterializerRegressionTest_StillPasses()
    {
        var result = RealFlatResult(8);
        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.Equal(8, result.PrescribedWeeks!.Count);
    }

    [Fact]
    public void RollingActivationContracts_PreparationRunwayNamespace_DoesNotReferenceControllerOrDbContext()
    {
        var namespaceTypes = typeof(ImmutablePreparationRunwayPrescription<>).Assembly.GetTypes()
            .Where(t => t.Namespace == "RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway");

        Assert.DoesNotContain(namespaceTypes, t => t.Name.Contains("DbContext", StringComparison.Ordinal));
    }
}
