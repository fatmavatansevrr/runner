using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

internal enum StageReachabilityOutcome { Pass, Fail, DecisionRequired, NotApplicable }

internal enum StageReachabilityFindingCode
{
    AllocationNotMathematicallyFeasible,
    PhaseProgressionMissing,
    PhaseProgressionDuplicate,
    StageKeyMissing,
    StageKeyDuplicate,
    FallbackTargetMissing,
    FallbackCycle,
    FallbackTargetAmbiguous,
    RuntimeConditionStageSelectionUnresolved,
    MinimumExposureUnsatisfied,
    MaximumExposureExceeded,
    RelativeOrderViolation,
    ProtectedOrFixedExposureOmitted,
    WeeklyStageSlotCapacityExceeded,
    ExactStageReachabilityFit,
    SchedulerRejectedStructure,
}

internal sealed record StageReachabilityFinding(
    StageReachabilityFindingCode Code,
    string Message,
    string? PhaseKey = null,
    string? StageKey = null,
    int? Required = null,
    int? Actual = null);

internal sealed record ScheduledStageExposure(
    int PhaseWeekNumber,
    string RequestedStageKey,
    string EffectiveStageKey,
    bool UsedFallback,
    int ExposureOrdinal);

internal sealed record PhaseStageReachabilityResult(
    string PhaseKey,
    int AllocatedWeeks,
    int AvailableExposureSlots,
    IReadOnlyList<ScheduledStageExposure> ScheduledExposures,
    IReadOnlyList<string> UnsatisfiedRequiredStageKeys,
    IReadOnlyList<string> OmittedOptionalStageKeys,
    StageReachabilityOutcome Outcome,
    IReadOnlyList<StageReachabilityFinding> Findings);

internal sealed record StageReachabilityVerificationResult(
    int TargetWeeks,
    IReadOnlyList<PhaseStageReachabilityResult> PhaseResults,
    StageReachabilityOutcome Outcome,
    IReadOnlyList<StageReachabilityFinding> Findings);

/// <summary>
/// Phase 4G.3B.3b dark verifier. It constructs only the existing pure phase/week
/// skeleton input and delegates all stage selection, fallback, ordering,
/// compression and extension behavior to <see cref="ProgressionStageAllocator"/>.
/// It then validates the allocator's typed schedule; it does not implement a
/// second scheduling algorithm and has no production call site.
/// </summary>
internal static class StageReachabilityVerifier
{
    public static StageReachabilityVerificationResult Verify(
        PhaseAllocationResult allocation,
        CatalogWorkoutProgressionDefinition progression,
        IReadOnlyList<RuntimeConditionResolutionResult> runtimeConditions,
        IReadOnlyList<string> weeklySlotRoles)
    {
        if (!allocation.IsMathematicallyFeasible)
        {
            var finding = Finding(StageReachabilityFindingCode.AllocationNotMathematicallyFeasible,
                "The phase allocation is not mathematically feasible.");
            return new(allocation.TargetWeeks, [], StageReachabilityOutcome.NotApplicable, [finding]);
        }

        var structural = ValidateInputs(allocation, progression, weeklySlotRoles);
        if (structural.Count > 0)
        {
            return new(allocation.TargetWeeks, [], StageReachabilityOutcome.Fail, structural);
        }

        try
        {
            var skeleton = BuildSkeleton(allocation, progression, weeklySlotRoles);
            var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
            {
                CandidateKey = "DARK_STAGE_REACHABILITY_VERIFICATION",
                CandidateVersion = progression.Version,
                Progression = progression,
                Skeleton = skeleton,
                ConditionResults = runtimeConditions,
            });

            return Inspect(allocation, progression, schedule);
        }
        catch (ProgressionStageConditionResultMissingException ex)
        {
            return Unresolved(allocation.TargetWeeks, ex.Message);
        }
        catch (ProgressionStageUnknownConditionKeyException ex)
        {
            return Unresolved(allocation.TargetWeeks, ex.Message);
        }
        catch (ProgressionStageFallbackTargetNotFoundException ex)
        {
            return Failed(allocation.TargetWeeks, StageReachabilityFindingCode.FallbackTargetMissing, ex.Message);
        }
        catch (ProgressionStageFallbackCycleException ex)
        {
            return Failed(allocation.TargetWeeks, StageReachabilityFindingCode.FallbackCycle, ex.Message);
        }
        catch (ProgressionStageAmbiguousFallbackException ex)
        {
            return Failed(allocation.TargetWeeks, StageReachabilityFindingCode.FallbackTargetAmbiguous, ex.Message);
        }
        catch (ProgressionStageDuplicateOrMissingKeyException ex)
        {
            return Failed(allocation.TargetWeeks, StageReachabilityFindingCode.StageKeyDuplicate, ex.Message);
        }
        catch (ProgressionPhaseCapacityInsufficientException ex)
        {
            return Failed(allocation.TargetWeeks, StageReachabilityFindingCode.MinimumExposureUnsatisfied, ex.Message);
        }
        catch (ProgressionPhaseCapacityExceedsMaximumException ex)
        {
            return Failed(allocation.TargetWeeks, StageReachabilityFindingCode.MaximumExposureExceeded, ex.Message);
        }
        catch (ProgressionStageIneligibleWithoutFallbackException ex)
        {
            return Failed(allocation.TargetWeeks, StageReachabilityFindingCode.MinimumExposureUnsatisfied, ex.Message);
        }
        catch (ProgressionStageSchedulingException ex)
        {
            return Failed(allocation.TargetWeeks, StageReachabilityFindingCode.SchedulerRejectedStructure, ex.Message);
        }
    }

    private static List<StageReachabilityFinding> ValidateInputs(
        PhaseAllocationResult allocation,
        CatalogWorkoutProgressionDefinition progression,
        IReadOnlyList<string> roles)
    {
        var findings = new List<StageReachabilityFinding>();
        if (roles.Count(r => r == ScheduledProgressionWeek.KeySessionStructuralRole) != 1)
        {
            findings.Add(Finding(StageReachabilityFindingCode.WeeklyStageSlotCapacityExceeded,
                $"Weekly role contract must contain exactly one KEY_SESSION slot; found {roles.Count(r => r == ScheduledProgressionWeek.KeySessionStructuralRole)}."));
        }

        foreach (var phase in allocation.Phases)
        {
            var matches = progression.PhaseProgressions.Where(p => p.PhaseKey == phase.PhaseKey).ToList();
            if (matches.Count == 0)
                findings.Add(Finding(StageReachabilityFindingCode.PhaseProgressionMissing, "Allocated phase has no progression definition.", phase.PhaseKey));
            else if (matches.Count > 1)
                findings.Add(Finding(StageReachabilityFindingCode.PhaseProgressionDuplicate, "Allocated phase has duplicate progression definitions.", phase.PhaseKey));
        }

        foreach (var phase in progression.PhaseProgressions)
        {
            if (phase.Stages.Any(s => string.IsNullOrWhiteSpace(s.ProgressionStageKey)))
                findings.Add(Finding(StageReachabilityFindingCode.StageKeyMissing, "Progression contains a blank stage key.", phase.PhaseKey));
            foreach (var duplicate in phase.Stages.GroupBy(s => s.ProgressionStageKey).Where(g => g.Count() > 1))
                findings.Add(Finding(StageReachabilityFindingCode.StageKeyDuplicate, "Progression contains a duplicate stage key.", phase.PhaseKey, duplicate.Key));

            var byKey = phase.Stages.Where(s => !string.IsNullOrWhiteSpace(s.ProgressionStageKey))
                .GroupBy(s => s.ProgressionStageKey).Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.Single());
            foreach (var stage in byKey.Values.Where(s => s.FallbackStageKey is not null))
            {
                if (!byKey.ContainsKey(stage.FallbackStageKey!))
                {
                    findings.Add(Finding(StageReachabilityFindingCode.FallbackTargetMissing,
                        "Fallback target does not exist in the same phase.", phase.PhaseKey, stage.ProgressionStageKey));
                    continue;
                }
                var visited = new HashSet<string> { stage.ProgressionStageKey };
                var current = stage;
                while (current.FallbackStageKey is not null && byKey.TryGetValue(current.FallbackStageKey, out var next))
                {
                    if (!visited.Add(next.ProgressionStageKey))
                    {
                        findings.Add(Finding(StageReachabilityFindingCode.FallbackCycle,
                            "Fallback topology contains a self-reference or cycle.", phase.PhaseKey, stage.ProgressionStageKey));
                        break;
                    }
                    current = next;
                }
            }
        }
        return findings;
    }

    private static GeneratedCatalogPlanSkeleton BuildSkeleton(
        PhaseAllocationResult allocation,
        CatalogWorkoutProgressionDefinition progression,
        IReadOnlyList<string> roles)
    {
        var context = new CatalogStageToWeekMaterializationContext
        {
            StartDate = new DateOnly(2000, 1, 3),
            AsOfDate = new DateOnly(2000, 1, 3),
            PlannedWeekCount = allocation.TargetWeeks,
            DaysPerWeek = roles.Count,
            CanonicalDistanceFamily = progression.DistanceFamily,
            CandidateKey = "DARK_STAGE_REACHABILITY_VERIFICATION",
            CandidateVersion = progression.Version,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
            SelectedStageSequence = allocation.Phases.Select(p => p.PhaseKey).ToList(),
            StageWeekAllocations = allocation.Phases.Select(p => new CatalogStageWeekAllocation(p.PhaseKey, p.AllocatedWeeks)).ToList(),
            RunLayout = new PlanCatalogReference("DARK_VERIFICATION_LAYOUT", 1),
            RunLayoutSlotRoles = roles,
        };
        return new CatalogStageToWeekMaterializer().Materialize(context).Skeleton;
    }

    private static StageReachabilityVerificationResult Inspect(
        PhaseAllocationResult allocation,
        CatalogWorkoutProgressionDefinition progression,
        GeneratedCatalogStageSchedule schedule)
    {
        var allFindings = new List<StageReachabilityFinding>();
        var phaseResults = new List<PhaseStageReachabilityResult>();

        foreach (var allocated in allocation.Phases)
        {
            var phase = progression.PhaseProgressions.Single(p => p.PhaseKey == allocated.PhaseKey);
            var scheduled = schedule.Weeks.Where(w => w.PhaseKey == allocated.PhaseKey).OrderBy(w => w.WeekNumber).ToList();
            var firstWeek = scheduled.Count == 0 ? 0 : scheduled[0].WeekNumber - 1;
            var ordinals = new Dictionary<string, int>();
            var exposures = scheduled.Select(w =>
            {
                var requested = w.RequestedProgressionStageKey ?? w.ProgressionStageKey;
                ordinals.TryGetValue(requested, out var prior);
                ordinals[requested] = prior + 1;
                return new ScheduledStageExposure(w.WeekNumber - firstWeek, requested, w.ProgressionStageKey,
                    w.FallbackOrigin is not null, prior + 1);
            }).ToList();

            var findings = new List<StageReachabilityFinding>();
            if (scheduled.Count > allocated.AllocatedWeeks)
                findings.Add(Finding(StageReachabilityFindingCode.WeeklyStageSlotCapacityExceeded, "Scheduled exposures exceed allocated phase weeks.", allocated.PhaseKey, actual: scheduled.Count));

            if (!scheduled.Select(w => w.StageRelativeOrder).SequenceEqual(scheduled.Select(w => w.StageRelativeOrder).OrderBy(x => x)))
                findings.Add(Finding(StageReachabilityFindingCode.RelativeOrderViolation, "Scheduled stage RelativeOrder values are not monotonic.", allocated.PhaseKey));

            var fallbackTargets = phase.Stages.Where(s => s.FallbackStageKey is not null).Select(s => s.FallbackStageKey!).ToHashSet();
            var topLevel = phase.Stages.Where(s => !fallbackTargets.Contains(s.ProgressionStageKey)).ToList();
            var unsatisfied = new List<string>();
            foreach (var stage in topLevel)
            {
                var count = exposures.Count(e => e.RequestedStageKey == stage.ProgressionStageKey);
                var compressedFloor = stage.CompressionBehavior == CatalogStageCompressionBehavior.Compressible ? Math.Min(stage.MinimumExposures, 1) : stage.MinimumExposures;
                if (count < compressedFloor)
                {
                    unsatisfied.Add(stage.ProgressionStageKey);
                    var code = stage.CompressionBehavior == CatalogStageCompressionBehavior.Protected || stage.ExtensionBehavior == CatalogStageExtensionBehavior.FixedExposure
                        ? StageReachabilityFindingCode.ProtectedOrFixedExposureOmitted
                        : StageReachabilityFindingCode.MinimumExposureUnsatisfied;
                    findings.Add(Finding(code, "Scheduled exposure count is below the permitted floor.", allocated.PhaseKey,
                        stage.ProgressionStageKey, compressedFloor, count));
                }
                if (count > stage.MaximumExposures)
                    findings.Add(Finding(StageReachabilityFindingCode.MaximumExposureExceeded, "Scheduled exposure count exceeds MaximumExposures.", allocated.PhaseKey,
                        stage.ProgressionStageKey, stage.MaximumExposures, count));
            }

            if (scheduled.Count == allocated.AllocatedWeeks && findings.Count == 0)
                findings.Add(Finding(StageReachabilityFindingCode.ExactStageReachabilityFit, "Every phase week has exactly one valid progression exposure.", allocated.PhaseKey));

            var outcome = findings.Any(f => f.Code != StageReachabilityFindingCode.ExactStageReachabilityFit)
                ? StageReachabilityOutcome.Fail : StageReachabilityOutcome.Pass;
            phaseResults.Add(new(allocated.PhaseKey, allocated.AllocatedWeeks, allocated.AllocatedWeeks,
                exposures, unsatisfied, [], outcome, findings));
            allFindings.AddRange(findings);
        }

        var overall = phaseResults.All(p => p.Outcome == StageReachabilityOutcome.Pass)
            ? StageReachabilityOutcome.Pass : StageReachabilityOutcome.Fail;
        return new(allocation.TargetWeeks, phaseResults, overall, allFindings);
    }

    private static StageReachabilityVerificationResult Unresolved(int targetWeeks, string message) =>
        new(targetWeeks, [], StageReachabilityOutcome.DecisionRequired,
            [Finding(StageReachabilityFindingCode.RuntimeConditionStageSelectionUnresolved, message)]);

    private static StageReachabilityVerificationResult Failed(int targetWeeks, StageReachabilityFindingCode code, string message) =>
        new(targetWeeks, [], StageReachabilityOutcome.Fail, [Finding(code, message)]);

    private static StageReachabilityFinding Finding(StageReachabilityFindingCode code, string message,
        string? phaseKey = null, string? stageKey = null, int? required = null, int? actual = null) =>
        new(code, message, phaseKey, stageKey, required, actual);
}
