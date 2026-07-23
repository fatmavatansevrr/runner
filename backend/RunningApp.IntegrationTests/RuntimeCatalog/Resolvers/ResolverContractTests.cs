using System;
using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4C (amended 4D.1.5) — contract-shape tests for
/// the resolver input snapshot, output result, and decision-trace models.
/// These tests construct plain data objects only: no resolver logic is
/// invoked beyond the shared factories' own invariant checks, no plan-catalog
/// file is read, and no TrainingWeek/TrainingDay is created.
/// </summary>
public sealed class ResolverContractTests
{
    [Fact]
    public void ResolverInputSnapshot_CanCarryAllPhase4BFitnessEvidenceFields()
    {
        var snapshot = new ResolverInputSnapshot
        {
            RecentLongestRunKm = 9.0,
            RecentWeeklyVolumeKm = 24.0,
            RecentRunsPerWeek = 4,
            RecentRaceDistanceKm = 5.0,
            RecentRaceFinishTimeSeconds = 1450,
            RecentRaceDate = new DateOnly(2026, 6, 15),
        };

        Assert.Equal(9.0, snapshot.RecentLongestRunKm);
        Assert.Equal(24.0, snapshot.RecentWeeklyVolumeKm);
        Assert.Equal(4, snapshot.RecentRunsPerWeek);
        Assert.Equal(5.0, snapshot.RecentRaceDistanceKm);
        Assert.Equal(1450, snapshot.RecentRaceFinishTimeSeconds);
        Assert.Equal(new DateOnly(2026, 6, 15), snapshot.RecentRaceDate);
    }

    [Fact]
    public void ResolverInputSnapshot_CanCarryDistanceAndScheduleIdentity()
    {
        var snapshot = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 8.0,
            CanonicalDistanceFamily = "TEN_K",
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10.0,
            StartDate = new DateOnly(2026, 8, 3),
            RaceDate = new DateOnly(2026, 10, 25),
            TargetFinishTimeSeconds = 3000,
            DaysPerWeek = 4,
            Level = RunningBackground.Intermediate,
        };

        Assert.Equal(8.0, snapshot.RequestedTargetDistanceKm);
        Assert.Equal("TEN_K", snapshot.CanonicalDistanceFamily);
        Assert.Equal(10.0, snapshot.GoalDistanceKm);
        Assert.NotEqual(snapshot.RequestedTargetDistanceKm, snapshot.GoalDistanceKm); // never conflated, mirrors Phase 3's own guarantee
    }

    [Fact]
    public void ResolverInputSnapshot_AllFieldsAreOptional_EmptySnapshotIsValid()
    {
        var snapshot = new ResolverInputSnapshot();

        Assert.Null(snapshot.RequestedTargetDistanceKm);
        Assert.Null(snapshot.RecentLongestRunKm);
        Assert.Null(snapshot.RecentRaceDate);
        Assert.Null(snapshot.GoalType);
    }

    // ─── RuntimeConditionResolutionResult: Evaluated ────────────────────────

    [Theory]
    [InlineData("GOAL_FEASIBILITY_IN", "REALISTIC")]
    [InlineData("PACE_SOURCE_IN", "RECENT_RACE")]
    [InlineData("TIME_ADEQUACY_IN", "ADEQUATE")]
    [InlineData("CORE_ENTRY_READINESS_IN", "READY")]
    [InlineData("PLAN_MODE_IN", "STANDARD")]
    public void RuntimeConditionResolutionResult_Evaluated_CanRepresentEachConditionType(string conditionType, string outputValue)
    {
        var result = RuntimeConditionResolutionResult.Evaluated(conditionType, outputValue, "CONTRACT_TEST_PLACEHOLDER");

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal(conditionType, result.ConditionType);
        Assert.Equal(outputValue, result.OutputValue);
        Assert.False(result.FallbackApplied);
        Assert.Empty(result.Warnings);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RuntimeConditionResolutionResult_Evaluated_RequiresNonBlankOutputValue(string? blankOutputValue)
    {
        Assert.Throws<ArgumentException>(() =>
            RuntimeConditionResolutionResult.Evaluated("TIME_ADEQUACY_IN", blankOutputValue!, "SOME_REASON"));
    }

    [Fact]
    public void RuntimeConditionResolutionResult_CanCarryRicherGoalFeasibilityBand_AsMetadataOnly()
    {
        // GOAL_FEASIBILITY_IN's registry-simple output stays CHALLENGING, but the
        // richer Appsel V1 aggressiveness band (STRETCH) is carried as metadata,
        // never as OutputValue -- per Phase 4A.3's owner-approved V1 scope.
        var result = RuntimeConditionResolutionResult.Evaluated(
            "GOAL_FEASIBILITY_IN", "CHALLENGING", "WITHIN_CHALLENGING_BAND",
            metadata: new Dictionary<string, string> { ["aggressivenessBand"] = "STRETCH", ["goalGapRatio"] = "0.08" });

        Assert.Equal("CHALLENGING", result.OutputValue);
        Assert.Equal("STRETCH", result.Metadata["aggressivenessBand"]);
        Assert.NotEqual(result.OutputValue, result.Metadata["aggressivenessBand"]);
    }

    [Fact]
    public void RuntimeConditionResolutionResult_PaceRecencyConfidence_IsMetadataNotOutputValue()
    {
        var result = RuntimeConditionResolutionResult.Evaluated(
            "PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED",
            confidenceLabel: "HIGH",
            metadata: new Dictionary<string, string> { ["paceRecencyConfidence"] = "HIGH", ["paceEvidenceAgeDays"] = "49" });

        Assert.Equal("RECENT_RACE", result.OutputValue);
        Assert.Equal("HIGH", result.ConfidenceLabel);
        Assert.Equal("HIGH", result.Metadata["paceRecencyConfidence"]);
        Assert.NotEqual("HIGH", result.OutputValue);
    }

    // ─── RuntimeConditionResolutionResult: NotEvaluated ─────────────────────

    [Fact]
    public void RuntimeConditionResolutionResult_NotEvaluated_HasNullOutputValue_AndRequiredReasonCode()
    {
        var result = RuntimeConditionResolutionResult.NotEvaluated("PACE_SOURCE_IN", "MISSING_RECENT_RACE_AND_TARGET_TIME");

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Null(result.OutputValue);
        Assert.Equal("MISSING_RECENT_RACE_AND_TARGET_TIME", result.ReasonCode);
    }

    [Fact]
    public void RuntimeConditionResolutionResult_NotEvaluated_CanCarryMetadataAndWarnings()
    {
        var result = RuntimeConditionResolutionResult.NotEvaluated(
            "CORE_ENTRY_READINESS_IN", "MISSING_WEEKLY_VOLUME",
            warnings: new[] { "no recent weekly volume was provided" },
            metadata: new Dictionary<string, string> { ["hint"] = "ask user for recent training history" });

        Assert.Null(result.OutputValue);
        Assert.Single(result.Warnings);
        Assert.Equal("ask user for recent training history", result.Metadata["hint"]);
    }

    [Fact]
    public void RuntimeConditionResolutionResult_NotEvaluated_IsDistinctFromGoalFeasibilityNotRequested()
    {
        // NOT_REQUESTED is a real, Evaluated GOAL_FEASIBILITY_IN registry
        // value (e.g. "no target time was requested, so feasibility is
        // N/A-by-design"). NotEvaluated status means the resolver COULD NOT
        // decide at all due to missing prerequisite evidence. These must
        // never be confused with each other.
        var notRequested = RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "NOT_REQUESTED", "NO_TARGET_TIME_REQUESTED");
        var notEvaluated = RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "MISSING_PREREQUISITE_RESOLVER_OUTPUT");

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, notRequested.Status);
        Assert.Equal("NOT_REQUESTED", notRequested.OutputValue);

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, notEvaluated.Status);
        Assert.Null(notEvaluated.OutputValue);

        Assert.NotEqual(notRequested.Status, notEvaluated.Status);
    }

    // ─── Decision trace ──────────────────────────────────────────────────────

    [Fact]
    public void ResolverDecisionTrace_CanContainMultipleResolverSteps_InOrder_MixingEvaluatedAndNotEvaluated()
    {
        var coreEntryReadiness = RuntimeConditionResolutionResult.Evaluated("CORE_ENTRY_READINESS_IN", "READY", "CONTRACT_TEST_PLACEHOLDER");
        var timeAdequacy = RuntimeConditionResolutionResult.Evaluated("TIME_ADEQUACY_IN", "ADEQUATE", "CONTRACT_TEST_PLACEHOLDER");
        var paceSource = RuntimeConditionResolutionResult.NotEvaluated("PACE_SOURCE_IN", "MISSING_RECENT_RACE_AND_TARGET_TIME");

        var trace = new ResolverDecisionTrace
        {
            Steps = new[]
            {
                ResolverDecisionTraceStep.FromResult(0, "CORE_ENTRY_READINESS_RESOLVER", coreEntryReadiness),
                ResolverDecisionTraceStep.FromResult(1, "TIME_ADEQUACY_RESOLVER", timeAdequacy),
                ResolverDecisionTraceStep.FromResult(2, "PACE_SOURCE_RESOLVER", paceSource),
            },
        };

        Assert.Equal(3, trace.Steps.Count);
        Assert.Equal(new[] { 0, 1, 2 }, trace.Steps.Select(s => s.StepIndex));
        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, trace.Steps[0].Status);
        Assert.Equal("READY", trace.Steps[0].OutputValue);
        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, trace.Steps[2].Status);
        Assert.Null(trace.Steps[2].OutputValue);
    }

    [Fact]
    public void ResolverDecisionTraceStep_NotEvaluated_DoesNotExposeAnOutputValue()
    {
        var result = RuntimeConditionResolutionResult.NotEvaluated("TIME_ADEQUACY_IN", "NOT_APPLICABLE_NON_RACE_PLAN");
        var step = ResolverDecisionTraceStep.FromResult(0, "TIME_ADEQUACY_RESOLVER", result);

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, step.Status);
        Assert.Null(step.OutputValue);
        Assert.Equal("NOT_APPLICABLE_NON_RACE_PLAN", step.ReasonCode);
    }

    [Fact]
    public void ResolverDecisionTrace_EmptyStepsIsValid()
    {
        var trace = new ResolverDecisionTrace();

        Assert.Empty(trace.Steps);
    }
}
