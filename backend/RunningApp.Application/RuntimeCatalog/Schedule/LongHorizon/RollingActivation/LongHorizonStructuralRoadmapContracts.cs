using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>Phase 4K.5 Part 1 — the three Long-Horizon segments, in their fixed, unchanged order.</summary>
internal enum LongHorizonStructuralSegmentType
{
    GeneralEndurance,
    PreparationRunway,
    Core,
}

/// <summary>
/// Phase 4K.5 Part 1 — a typed representation of the complete 21-52 week
/// structural roadmap approved by Phase 4I.1/4K.1. Deliberately does not
/// replace <c>LongHorizonCompositionDecision</c> (Phase 4I.3, still the
/// authoritative composition/eligibility record) -- this is an additive,
/// rolling-lifecycle-aware projection of it, carrying per-week numeric
/// lifecycle state (Phase 4K.1) that the composition decision itself has no
/// reason to carry.
/// </summary>
internal sealed record LongHorizonStructuralRoadmap
{
    public required int TotalWeeks { get; init; }
    public required int GeneralEnduranceWeeks { get; init; }
    public required int PreparationRunwayWeeks { get; init; }
    public required int CoreWeeks { get; init; }
    public required IReadOnlyList<LongHorizonStructuralSegment> Segments { get; init; }
    public required IReadOnlyList<int> GlobalWeekNumbers { get; init; }
    public IReadOnlyList<LongHorizonStructuralWeek> Weeks { get; init; } = [];
    public required DateOnly RaceDate { get; init; }
    public required ReadinessProfile Profile { get; init; }
    public required string StructuralStatus { get; init; }
}

/// <summary>Phase 4K.5 Part 1 — one contiguous segment of the structural roadmap.</summary>
internal sealed record LongHorizonStructuralSegment
{
    public required LongHorizonStructuralSegmentType SegmentType { get; init; }
    public required int StartGlobalWeek { get; init; }
    public required int EndGlobalWeek { get; init; }
    public int WeekCount => EndGlobalWeek - StartGlobalWeek + 1;
    public required string Provenance { get; init; }
}

/// <summary>
/// Phase 4K.5 Part 1 — one globally-numbered structural week. Carries no
/// numeric distance/pace fields itself (those belong to
/// <see cref="ActivatedNumericWeek"/> once activated) -- only structural
/// shape plus the current <see cref="LongHorizonNumericLifecycleState"/>.
/// </summary>
internal sealed record LongHorizonStructuralWeek
{
    public required int GlobalWeekNumber { get; init; }
    public required LongHorizonStructuralSegmentType SegmentType { get; init; }
    public required string Stage { get; init; }
    public required IReadOnlyList<string> StructuralWorkoutRoles { get; init; }
    public (DateOnly Start, DateOnly End)? CalendarRange { get; init; }
    public required LongHorizonNumericLifecycleState NumericLifecycleState { get; init; }
}

/// <summary>
/// Phase 4K.5 Part 1 — validates the required structural invariants.
/// Reuses Phase 4I.1's own arithmetic (TotalWeeks = GE + 8 + 12,
/// GE = TotalWeeks - 20) rather than restating a second formula.
/// </summary>
internal static class LongHorizonStructuralRoadmapValidator
{
    private const int PreparationRunwayFixedWeeks = 8;
    private const int CoreFixedWeeks = 12;
    private const int MinimumTotalWeeks = 21;
    private const int MaximumTotalWeeks = 52;

    public static void Validate(LongHorizonStructuralRoadmap roadmap)
    {
        if (roadmap.TotalWeeks is < MinimumTotalWeeks or > MaximumTotalWeeks)
        {
            throw new LongHorizonStructuralRoadmapInvalidException(
                $"TotalWeeks must be {MinimumTotalWeeks}-{MaximumTotalWeeks}, was {roadmap.TotalWeeks}.");
        }

        if (roadmap.PreparationRunwayWeeks != PreparationRunwayFixedWeeks)
        {
            throw new LongHorizonStructuralRoadmapInvalidException(
                $"PreparationRunwayWeeks must be fixed at {PreparationRunwayFixedWeeks}, was {roadmap.PreparationRunwayWeeks}.");
        }

        if (roadmap.CoreWeeks != CoreFixedWeeks)
        {
            throw new LongHorizonStructuralRoadmapInvalidException(
                $"CoreWeeks must be fixed at {CoreFixedWeeks}, was {roadmap.CoreWeeks}.");
        }

        var expectedGeWeeks = roadmap.TotalWeeks - MinimumTotalWeeks + 1;
        if (roadmap.GeneralEnduranceWeeks != expectedGeWeeks)
        {
            throw new LongHorizonStructuralRoadmapInvalidException(
                $"GeneralEnduranceWeeks must equal TotalWeeks - 20 ({expectedGeWeeks}), was {roadmap.GeneralEnduranceWeeks}.");
        }

        if (roadmap.GeneralEnduranceWeeks + roadmap.PreparationRunwayWeeks + roadmap.CoreWeeks != roadmap.TotalWeeks)
        {
            throw new LongHorizonStructuralRoadmapInvalidException(
                "GeneralEnduranceWeeks + PreparationRunwayWeeks + CoreWeeks must equal TotalWeeks.");
        }

        ValidateGlobalWeekNumbersContiguousAndUnique(roadmap.GlobalWeekNumbers, roadmap.TotalWeeks);
        ValidateSegmentOrder(roadmap.Segments);

        if (roadmap.Weeks.Count > 0)
        {
            if (roadmap.Weeks.Count != roadmap.TotalWeeks
                || !roadmap.Weeks.Select(w => w.GlobalWeekNumber).SequenceEqual(roadmap.GlobalWeekNumbers))
            {
                throw new LongHorizonStructuralRoadmapInvalidException(
                    "When populated, structural Weeks must cover the complete ordered GlobalWeekNumbers sequence.");
            }
        }
    }

    private static void ValidateGlobalWeekNumbersContiguousAndUnique(IReadOnlyList<int> globalWeekNumbers, int totalWeeks)
    {
        if (globalWeekNumbers.Count != totalWeeks)
        {
            throw new LongHorizonStructuralRoadmapInvalidException(
                $"GlobalWeekNumbers must contain exactly TotalWeeks ({totalWeeks}) entries, had {globalWeekNumbers.Count}.");
        }

        if (globalWeekNumbers.Distinct().Count() != globalWeekNumbers.Count)
        {
            throw new LongHorizonStructuralRoadmapInvalidException("GlobalWeekNumbers must be unique.");
        }

        var sorted = globalWeekNumbers.OrderBy(w => w).ToList();
        for (var i = 1; i < sorted.Count; i++)
        {
            if (sorted[i] != sorted[i - 1] + 1)
            {
                throw new LongHorizonStructuralRoadmapInvalidException("GlobalWeekNumbers must be contiguous.");
            }
        }
    }

    private static void ValidateSegmentOrder(IReadOnlyList<LongHorizonStructuralSegment> segments)
    {
        var expectedOrder = new[]
        {
            LongHorizonStructuralSegmentType.GeneralEndurance,
            LongHorizonStructuralSegmentType.PreparationRunway,
            LongHorizonStructuralSegmentType.Core,
        };

        var actualOrder = segments.Select(s => s.SegmentType).ToList();
        if (!actualOrder.SequenceEqual(expectedOrder))
        {
            throw new LongHorizonStructuralRoadmapInvalidException(
                "Segment order must be General Endurance -> Preparation Runway -> Core.");
        }

        for (var i = 1; i < segments.Count; i++)
        {
            if (segments[i].StartGlobalWeek != segments[i - 1].EndGlobalWeek + 1)
            {
                throw new LongHorizonStructuralRoadmapInvalidException(
                    $"Segment {segments[i].SegmentType} must start immediately after segment {segments[i - 1].SegmentType} ends.");
            }
        }
    }
}
