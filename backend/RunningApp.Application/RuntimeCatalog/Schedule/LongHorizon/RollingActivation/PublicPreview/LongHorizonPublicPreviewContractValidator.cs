namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview;

/// <summary>Phase 4L.1 Part 24 -- validates the public contract's own internal invariants. Run by the mapper's own tests; not (yet) run by any live endpoint.</summary>
internal static class LongHorizonPublicPreviewContractValidator
{
    private const int MinimumTotalWeeks = 21;
    private const int MaximumTotalWeeks = 52;

    public static void Validate(LongHorizonPlanPreviewContract contract)
    {
        if (contract.ContractVersion != 1)
        {
            throw new LongHorizonPublicPreviewContractInvalidException($"Unsupported ContractVersion {contract.ContractVersion}.");
        }

        if (contract.TotalWeeks is < MinimumTotalWeeks or > MaximumTotalWeeks)
        {
            throw new LongHorizonPublicPreviewContractInvalidException(
                $"TotalWeeks must be {MinimumTotalWeeks}-{MaximumTotalWeeks}, was {contract.TotalWeeks}.");
        }

        ValidateRoadmap(contract);
        ValidateExecutableWeeks(contract);
        ValidateCrossContract(contract);

        if (contract.BlockedState is not null && contract.BlockedState.LastEvaluatedDate == default)
        {
            throw new LongHorizonPublicPreviewContractInvalidException("BlockedState.LastEvaluatedDate must be set.");
        }
    }

    private static void ValidateRoadmap(LongHorizonPlanPreviewContract contract)
    {
        if (contract.StructuralRoadmap.Count != contract.TotalWeeks)
        {
            throw new LongHorizonPublicPreviewContractInvalidException(
                $"StructuralRoadmap must contain exactly TotalWeeks ({contract.TotalWeeks}) rows, had {contract.StructuralRoadmap.Count}.");
        }

        var weeks = contract.StructuralRoadmap.Select(r => r.GlobalWeek).ToList();
        if (weeks.Distinct().Count() != weeks.Count)
        {
            throw new LongHorizonPublicPreviewContractInvalidException("StructuralRoadmap weeks must be represented exactly once.");
        }

        var sorted = weeks.OrderBy(w => w).ToList();
        for (var i = 1; i < sorted.Count; i++)
        {
            if (sorted[i] != sorted[i - 1] + 1)
            {
                throw new LongHorizonPublicPreviewContractInvalidException("StructuralRoadmap weeks must be contiguous.");
            }
        }

        var phaseOrder = contract.StructuralRoadmap.OrderBy(r => r.GlobalWeek).Select(r => r.Phase).Distinct().ToList();
        var expectedOrder = new[] { LongHorizonPublicPhase.GeneralEndurance, LongHorizonPublicPhase.PreparationRunway, LongHorizonPublicPhase.Core }
            .Where(phaseOrder.Contains).ToList();
        if (!phaseOrder.SequenceEqual(expectedOrder))
        {
            throw new LongHorizonPublicPreviewContractInvalidException("StructuralRoadmap phase order must be General Endurance -> Preparation Runway -> Core.");
        }

        foreach (var row in contract.StructuralRoadmap)
        {
            var pendingLike = row.LifecycleStatus is LongHorizonPublicLifecycleStatus.Pending or LongHorizonPublicLifecycleStatus.Blocked;
            if (pendingLike && (row.IsExecutable || row.NumericDetailsAvailable))
            {
                throw new LongHorizonPublicPreviewContractInvalidException(
                    $"Roadmap week {row.GlobalWeek} is {row.LifecycleStatus} and must not claim executable/numeric details.");
            }
        }
    }

    private static void ValidateExecutableWeeks(LongHorizonPlanPreviewContract contract)
    {
        if (contract.CurrentExecutableWeeks.Count != contract.CurrentExecutableWeekCount)
        {
            throw new LongHorizonPublicPreviewContractInvalidException(
                "CurrentExecutableWeekCount must equal CurrentExecutableWeeks.Count.");
        }

        foreach (var week in contract.CurrentExecutableWeeks)
        {
            if (week.GlobalWeek < contract.CurrentWindowStartWeek || week.GlobalWeek > contract.CurrentWindowEndWeek)
            {
                throw new LongHorizonPublicPreviewContractInvalidException(
                    $"Executable week {week.GlobalWeek} falls outside the current window [{contract.CurrentWindowStartWeek},{contract.CurrentWindowEndWeek}].");
            }

            if (week.Sessions.Count == 0)
            {
                throw new LongHorizonPublicPreviewContractInvalidException($"Executable week {week.GlobalWeek} must carry at least one session.");
            }

            foreach (var session in week.Sessions)
            {
                if (session.SessionDate < week.WeekStartDate || session.SessionDate > week.WeekEndDate)
                {
                    throw new LongHorizonPublicPreviewContractInvalidException(
                        $"Session date {session.SessionDate} for week {week.GlobalWeek} falls outside [{week.WeekStartDate},{week.WeekEndDate}].");
                }
            }
        }
    }

    private static void ValidateCrossContract(LongHorizonPlanPreviewContract contract)
    {
        var executableWeekNumbers = contract.CurrentExecutableWeeks.Select(w => w.GlobalWeek).ToList();
        if (executableWeekNumbers.Distinct().Count() != executableWeekNumbers.Count)
        {
            throw new LongHorizonPublicPreviewContractInvalidException("An executable week must appear at most once.");
        }

        var roadmapByWeek = contract.StructuralRoadmap.ToDictionary(r => r.GlobalWeek);
        foreach (var globalWeek in executableWeekNumbers)
        {
            if (!roadmapByWeek.TryGetValue(globalWeek, out var row) || row.LifecycleStatus != LongHorizonPublicLifecycleStatus.Available)
            {
                throw new LongHorizonPublicPreviewContractInvalidException(
                    $"Executable week {globalWeek} must have a corresponding roadmap row with LifecycleStatus=Available.");
            }
        }

        foreach (var row in contract.StructuralRoadmap.Where(r => r.LifecycleStatus == LongHorizonPublicLifecycleStatus.Pending))
        {
            if (executableWeekNumbers.Contains(row.GlobalWeek))
            {
                throw new LongHorizonPublicPreviewContractInvalidException(
                    $"Roadmap week {row.GlobalWeek} is Pending and must not appear in CurrentExecutableWeeks.");
            }
        }
    }
}
