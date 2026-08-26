using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

/// <summary>
/// Phase 4G.6A.4D production-owned, dark-unwired materializer. It expands
/// already-resolved allocations and bindings into undated structural weeks.
/// It never allocates blocks, selects a progression prefix, assigns calendar
/// positions, or produces a prescription.
/// </summary>
internal static class PreparationRunwayWeekMaterializer
{
    public const string MaterializerId = "PREPARATION_RUNWAY_FULL_WEEK_MATERIALIZER";
    public const int MaterializerVersion = 1;

    public static async Task<PreparationRunwayWeekMaterializationResult<TKey>> MaterializeAsync<TKey>(
        PreparationRunwayWeekMaterializationRequest<TKey> request,
        ICatalogWorkoutDefinitionLoader workoutLoader,
        CancellationToken ct = default) where TKey : notnull
    {
        var trace = new List<string> { $"Materializer={MaterializerId}v{MaterializerVersion}" };

        if (request is null || workoutLoader is null)
            return Failure<TKey>(PreparationRunwayWeekMaterializationFailureCode.InvalidMaterializationRequest,
                "Request and workout loader are required.", trace);

        var requestFailure = ValidateRequest(request);
        if (requestFailure is not null)
            return Failure<TKey>(requestFailure.Value.Code, requestFailure.Value.Reason, trace);

        var allocationsByKey = request.OrderedBlockAllocations.ToDictionary(a => a.BlockKey);
        var bindingsByKey = request.OrderedBlockBindings.ToDictionary(b => b.BlockKey);
        var policiesByKey = request.BlockRolePolicies.ToDictionary(p => p.BlockKey);

        var weeks = new List<PreparationRunwayMaterializedWeek<TKey>>();
        var runwayWeekNumber = 1;

        foreach (var allocation in request.OrderedBlockAllocations
                     .Where(a => a.AllocatedWeeks > 0)
                     .OrderBy(a => a.CanonicalOrder))
        {
            if (!bindingsByKey.TryGetValue(allocation.BlockKey, out var bindingInput))
                return Failure<TKey>(PreparationRunwayWeekMaterializationFailureCode.MissingBlockBinding,
                    $"Positive allocation for '{allocation.BlockKey}' has no binding.", trace);

            if (!policiesByKey.TryGetValue(allocation.BlockKey, out var rolePolicy))
                return Failure<TKey>(PreparationRunwayWeekMaterializationFailureCode.UnsupportedBlockRolePolicy,
                    $"Block '{allocation.BlockKey}' has no structural role policy.", trace);

            for (var blockWeekOrdinal = 1; blockWeekOrdinal <= allocation.AllocatedWeeks; blockWeekOrdinal++)
            {
                var anchor = bindingInput.Binding.OrderedWorkoutReferences[blockWeekOrdinal - 1];
                var progressionStep = bindingInput.OrderedProgressionStepNumbers[blockWeekOrdinal - 1];

                if (!rolePolicy.AnchorRoleByProgressionStep.TryGetValue(progressionStep, out var anchorRole))
                    return Failure<TKey>(PreparationRunwayWeekMaterializationFailureCode.UnsupportedBlockRolePolicy,
                        $"Block '{allocation.BlockKey}' progression step {progressionStep} has no anchor-role policy.", trace);

                if (anchorRole is PreparationRunwaySlotRole.EasySupport)
                    return Failure<TKey>(PreparationRunwayWeekMaterializationFailureCode.AnchorRoleIncompatible,
                        $"Block '{allocation.BlockKey}' progression step {progressionStep} maps its single anchor to a repeated support role.", trace);

                var slots = new List<PreparationRunwayMaterializedWorkoutSlot<TKey>>(4);
                var roleOrdinals = new Dictionary<PreparationRunwaySlotRole, int>();
                var anchorUseCount = 0;

                for (var slotIndex = 0; slotIndex < request.CanonicalWeeklyLayout.OrderedRoles.Count; slotIndex++)
                {
                    var slotRole = request.CanonicalWeeklyLayout.OrderedRoles[slotIndex];
                    roleOrdinals.TryGetValue(slotRole, out var priorRoleCount);
                    var roleOrdinal = priorRoleCount + 1;
                    roleOrdinals[slotRole] = roleOrdinal;

                    var isAnchor = slotRole == anchorRole && anchorUseCount == 0;
                    var reference = isAnchor
                        ? anchor
                        : SupportReferenceFor(slotRole, request.SupportWorkoutPolicy);

                    var referenceFailure = await ValidateReferenceForRoleAsync(
                        reference, slotRole, isAnchor, workoutLoader, ct);
                    if (referenceFailure is not null)
                        return Failure<TKey>(referenceFailure.Value.Code, referenceFailure.Value.Reason, trace);

                    if (isAnchor)
                        anchorUseCount++;

                    slots.Add(new PreparationRunwayMaterializedWorkoutSlot<TKey>(
                        slotRole,
                        slotIndex + 1,
                        roleOrdinal,
                        reference.WorkoutId,
                        reference.WorkoutVersion,
                        allocation.BlockKey,
                        bindingInput.ProgressionId,
                        bindingInput.ProgressionVersion,
                        progressionStep,
                        isAnchor ? PreparationRunwayWorkoutSlotSource.Anchor : PreparationRunwayWorkoutSlotSource.SupportPolicy,
                        isAnchor ? rolePolicy.PolicyId : request.SupportWorkoutPolicy.PolicyId,
                        isAnchor ? rolePolicy.PolicyVersion : request.SupportWorkoutPolicy.PolicyVersion));
                }

                if (anchorUseCount != 1)
                    return Failure<TKey>(PreparationRunwayWeekMaterializationFailureCode.MaterializationInvariantViolation,
                        $"Runway week {runwayWeekNumber} used its selected anchor {anchorUseCount} times; exactly one is required.", trace);

                var cardinalityFailure = ValidateWeekCardinality(slots, runwayWeekNumber);
                if (cardinalityFailure is not null)
                    return Failure<TKey>(cardinalityFailure.Value.Code, cardinalityFailure.Value.Reason, trace);

                weeks.Add(new PreparationRunwayMaterializedWeek<TKey>(
                    runwayWeekNumber,
                    allocation.BlockKey,
                    blockWeekOrdinal,
                    bindingInput.ProgressionId,
                    bindingInput.ProgressionVersion,
                    progressionStep,
                    allocation.CanonicalOrder,
                    slots,
                    new PreparationRunwayMaterializedWeekProvenance(
                        request.ProfileKey,
                        request.CandidateKey,
                        request.CandidateVersion,
                        request.AllocationPolicyId,
                        request.AllocationPolicyVersion,
                        request.SupportWorkoutPolicy.PolicyId,
                        request.SupportWorkoutPolicy.PolicyVersion,
                        request.CanonicalWeeklyLayout.SourceLayout)));

                runwayWeekNumber++;
            }
        }

        var expectedTotal = allocationsByKey.Values.Sum(a => a.AllocatedWeeks);
        if (weeks.Count != expectedTotal)
            return Failure<TKey>(PreparationRunwayWeekMaterializationFailureCode.MaterializationInvariantViolation,
                $"Materialized {weeks.Count} weeks, but allocations sum to {expectedTotal}.", trace);

        if (!weeks.Select(w => w.RunwayWeekNumber).SequenceEqual(Enumerable.Range(1, weeks.Count)))
            return Failure<TKey>(PreparationRunwayWeekMaterializationFailureCode.NonContiguousWeekNumber,
                "Global runway week numbers are not contiguous.", trace);

        foreach (var allocation in request.OrderedBlockAllocations.Where(a => a.AllocatedWeeks > 0))
        {
            var actual = weeks.Where(w => EqualityComparer<TKey>.Default.Equals(w.BlockType, allocation.BlockKey))
                .Select(w => w.BlockWeekOrdinal);
            if (!actual.SequenceEqual(Enumerable.Range(1, allocation.AllocatedWeeks)))
                return Failure<TKey>(PreparationRunwayWeekMaterializationFailureCode.NonContiguousBlockWeekOrdinal,
                    $"Block '{allocation.BlockKey}' week ordinals are not contiguous.", trace);
        }

        trace.Add($"MaterializedWeekCount={weeks.Count}");
        return PreparationRunwayWeekMaterializationResult<TKey>.Success(weeks, trace);
    }

    private static (PreparationRunwayWeekMaterializationFailureCode Code, string Reason)? ValidateRequest<TKey>(
        PreparationRunwayWeekMaterializationRequest<TKey> request) where TKey : notnull
    {
        if (string.IsNullOrWhiteSpace(request.ProfileKey) ||
            string.IsNullOrWhiteSpace(request.CandidateKey) || request.CandidateVersion < 1 ||
            string.IsNullOrWhiteSpace(request.AllocationPolicyId) || request.AllocationPolicyVersion < 1 ||
            request.OrderedBlockAllocations is null || request.OrderedBlockBindings is null ||
            request.BlockRolePolicies is null || request.CanonicalWeeklyLayout is null ||
            request.SupportWorkoutPolicy is null)
            return (PreparationRunwayWeekMaterializationFailureCode.InvalidMaterializationRequest,
                "Materialization identity, collections, layout, and policies are required.");

        var layout = request.CanonicalWeeklyLayout;
        if (layout.SourceLayout is null || layout.OrderedRoles is null ||
            string.IsNullOrWhiteSpace(layout.SourceLayout.Key) || layout.SourceLayout.Version < 1 ||
            !PreparationRunwayWeeklyShape.IsValid(layout.OrderedRoles))
            return (PreparationRunwayWeekMaterializationFailureCode.WeekRoleCardinalityViolation,
                "Canonical weekly layout must be exactly one KEY_SESSION, one LONG_RUN, and one or more EASY_SUPPORT slots.");

        var support = request.SupportWorkoutPolicy;
        if (string.IsNullOrWhiteSpace(support.PolicyId) || support.PolicyVersion < 1 ||
            !ValidReference(support.KeySessionDefault) || !ValidReference(support.EasySupportDefault) ||
            !ValidReference(support.LongRunDefault))
            return (PreparationRunwayWeekMaterializationFailureCode.InvalidMaterializationRequest,
                "Support-workout policy identity and references must be valid.");

        var allocationKeys = request.OrderedBlockAllocations.Select(a => a.BlockKey).ToArray();
        if (allocationKeys.Distinct().Count() != allocationKeys.Length)
            return (PreparationRunwayWeekMaterializationFailureCode.DuplicateBlockAllocation,
                "Block allocations must be unique.");
        if (request.OrderedBlockAllocations.Count == 0 || request.OrderedBlockAllocations.Any(a => a.AllocatedWeeks < 0))
            return (PreparationRunwayWeekMaterializationFailureCode.InvalidMaterializationRequest,
                "At least one allocation is required and allocated weeks cannot be negative.");
        if (request.OrderedBlockAllocations.Sum(a => a.AllocatedWeeks) <= 0)
            return (PreparationRunwayWeekMaterializationFailureCode.InvalidMaterializationRequest,
                "At least one full runway week must be allocated.");

        var allocationOrders = request.OrderedBlockAllocations.Select(a => a.CanonicalOrder).ToArray();
        if (allocationOrders.Any(o => o < 1) || allocationOrders.Distinct().Count() != allocationOrders.Length)
            return (PreparationRunwayWeekMaterializationFailureCode.InvalidBlockOrder,
                "Allocation canonical orders must be positive and unique.");

        var policyKeys = request.BlockRolePolicies.Select(p => p.BlockKey).ToArray();
        if (policyKeys.Distinct().Count() != policyKeys.Length ||
            request.BlockRolePolicies.Select(p => p.CanonicalOrder).Distinct().Count() != request.BlockRolePolicies.Count ||
            request.BlockRolePolicies.Any(p => string.IsNullOrWhiteSpace(p.PolicyId) || p.PolicyVersion < 1 ||
                                               p.AnchorRoleByProgressionStep is null))
            return (PreparationRunwayWeekMaterializationFailureCode.InvalidBlockOrder,
                "Block role policies must have unique keys and canonical orders.");

        foreach (var allocation in request.OrderedBlockAllocations)
        {
            var policy = request.BlockRolePolicies.SingleOrDefault(p =>
                EqualityComparer<TKey>.Default.Equals(p.BlockKey, allocation.BlockKey));
            if (policy is null)
            {
                if (allocation.AllocatedWeeks > 0)
                    return (PreparationRunwayWeekMaterializationFailureCode.UnsupportedBlockRolePolicy,
                        $"Block '{allocation.BlockKey}' has no role policy.");
                continue;
            }
            if (policy.CanonicalOrder != allocation.CanonicalOrder)
                return (PreparationRunwayWeekMaterializationFailureCode.InvalidBlockOrder,
                    $"Block '{allocation.BlockKey}' allocation order does not match its role policy.");
        }

        var bindingKeys = request.OrderedBlockBindings.Select(b => b.BlockKey).ToArray();
        if (bindingKeys.Distinct().Count() != bindingKeys.Length)
            return (PreparationRunwayWeekMaterializationFailureCode.BlockBindingMismatch,
                "Materialization bindings must have unique claimed block keys.");

        foreach (var bindingInput in request.OrderedBlockBindings)
        {
            if (bindingInput.Binding is null || bindingInput.OrderedProgressionStepNumbers is null ||
                bindingInput.Binding.OrderedWorkoutReferences is null)
                return (PreparationRunwayWeekMaterializationFailureCode.InvalidMaterializationRequest,
                    $"Binding contract for '{bindingInput.BlockKey}' is incomplete.");

            if (!EqualityComparer<TKey>.Default.Equals(bindingInput.BlockKey, bindingInput.Binding.BlockKey))
                return (PreparationRunwayWeekMaterializationFailureCode.BlockBindingMismatch,
                    $"Claimed binding block '{bindingInput.BlockKey}' does not match binder output '{bindingInput.Binding.BlockKey}'.");

            var allocation = request.OrderedBlockAllocations.SingleOrDefault(a =>
                EqualityComparer<TKey>.Default.Equals(a.BlockKey, bindingInput.BlockKey));
            if (allocation is null)
                return (PreparationRunwayWeekMaterializationFailureCode.BlockBindingMismatch,
                    $"Binding for '{bindingInput.BlockKey}' has no corresponding allocation.");

            if (bindingInput.Binding.AllocatedWeeks != allocation.AllocatedWeeks ||
                bindingInput.Binding.OrderedWorkoutReferences.Count != allocation.AllocatedWeeks ||
                bindingInput.OrderedProgressionStepNumbers.Count != allocation.AllocatedWeeks)
                return (PreparationRunwayWeekMaterializationFailureCode.BindingCountMismatch,
                    $"Binding for '{bindingInput.BlockKey}' does not contain exactly {allocation.AllocatedWeeks} anchors and step numbers.");

            if (allocation.AllocatedWeeks > 0 &&
                (string.IsNullOrWhiteSpace(bindingInput.ProgressionId) || bindingInput.ProgressionVersion < 1 ||
                 !bindingInput.OrderedProgressionStepNumbers.SequenceEqual(Enumerable.Range(1, allocation.AllocatedWeeks)) ||
                 bindingInput.Binding.OrderedWorkoutReferences.Any(r => !ValidReference(r))))
                return (PreparationRunwayWeekMaterializationFailureCode.InvalidMaterializationRequest,
                    $"Binding provenance for '{bindingInput.BlockKey}' is incomplete or non-contiguous.");
        }

        foreach (var positive in request.OrderedBlockAllocations.Where(a => a.AllocatedWeeks > 0))
        {
            if (!bindingKeys.Contains(positive.BlockKey))
                return (PreparationRunwayWeekMaterializationFailureCode.MissingBlockBinding,
                    $"Positive allocation for '{positive.BlockKey}' has no binding.");
        }

        return null;
    }

    private static PreparationRunwayWorkoutReference SupportReferenceFor(
        PreparationRunwaySlotRole role,
        PreparationRunwaySupportWorkoutPolicy policy) => role switch
        {
            PreparationRunwaySlotRole.KeySession => policy.KeySessionDefault,
            PreparationRunwaySlotRole.EasySupport => policy.EasySupportDefault,
            PreparationRunwaySlotRole.LongRun => policy.LongRunDefault,
            _ => throw new InvalidOperationException($"Unsupported structural role '{role}'."),
        };

    private static async Task<(PreparationRunwayWeekMaterializationFailureCode Code, string Reason)?> ValidateReferenceForRoleAsync(
        PreparationRunwayWorkoutReference reference,
        PreparationRunwaySlotRole role,
        bool isAnchor,
        ICatalogWorkoutDefinitionLoader loader,
        CancellationToken ct)
    {
        var genericFailure = await PreparationRunwayBlockWorkoutReferenceValidator.ValidateAsync(loader, reference, ct);
        if (genericFailure is not null)
            return (isAnchor
                    ? PreparationRunwayWeekMaterializationFailureCode.AnchorWorkoutReferenceInvalid
                    : PreparationRunwayWeekMaterializationFailureCode.SupportWorkoutReferenceInvalid,
                $"Workout '{reference.WorkoutId}' v{reference.WorkoutVersion} failed runway reference validation ({genericFailure}).");

        CatalogWorkoutDefinitionSummary summary;
        try
        {
            summary = await loader.LoadAsync(new PlanCatalogReference(reference.WorkoutId, reference.WorkoutVersion), ct);
        }
        catch (PlanCatalogLoadException ex)
        {
            return (isAnchor
                    ? PreparationRunwayWeekMaterializationFailureCode.AnchorWorkoutReferenceInvalid
                    : PreparationRunwayWeekMaterializationFailureCode.SupportWorkoutReferenceInvalid,
                ex.Message);
        }

        var compatible = role switch
        {
            PreparationRunwaySlotRole.KeySession => summary.Family is "EASY" or "QUALITY",
            PreparationRunwaySlotRole.EasySupport => summary.Family == "EASY",
            PreparationRunwaySlotRole.LongRun => summary.Family == "LONG_RUN",
            _ => false,
        };
        if (!compatible)
            return (isAnchor
                    ? PreparationRunwayWeekMaterializationFailureCode.AnchorRoleIncompatible
                    : PreparationRunwayWeekMaterializationFailureCode.SupportWorkoutReferenceInvalid,
                $"Workout '{summary.Key}' family '{summary.Family}' is incompatible with structural role '{role}'.");

        return null;
    }

    private static (PreparationRunwayWeekMaterializationFailureCode Code, string Reason)? ValidateWeekCardinality<TKey>(
        IReadOnlyList<PreparationRunwayMaterializedWorkoutSlot<TKey>> slots,
        int runwayWeekNumber) where TKey : notnull
    {
        if (!PreparationRunwayWeeklyShape.IsValid(slots.Select(s => s.SlotRole).ToArray()) ||
            slots.Select(s => s.SlotOrdinal).Distinct().Count() != slots.Count)
            return (PreparationRunwayWeekMaterializationFailureCode.WeekRoleCardinalityViolation,
                $"Runway week {runwayWeekNumber} does not contain exactly 1 KEY_SESSION, 1 LONG_RUN, and one or more EASY_SUPPORT slots.");
        return null;
    }

    private static bool ValidReference(PreparationRunwayWorkoutReference reference) =>
        reference is not null && !string.IsNullOrWhiteSpace(reference.WorkoutId) && reference.WorkoutVersion > 0;

    private static PreparationRunwayWeekMaterializationResult<TKey> Failure<TKey>(
        PreparationRunwayWeekMaterializationFailureCode code,
        string reason,
        List<string> trace) where TKey : notnull
    {
        trace.Add($"FAILED:{code}:{reason}");
        return PreparationRunwayWeekMaterializationResult<TKey>.Failure(code, reason, trace);
    }
}
