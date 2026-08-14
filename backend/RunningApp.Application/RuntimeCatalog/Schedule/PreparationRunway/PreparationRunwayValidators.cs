namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;

internal static class PreparationRunwayContextValidator
{
    public static PreparationRunwayValidationResult Validate(PreparationRunwayContext context)
    {
        var findings = new List<PreparationRunwayPlanningFinding>();
        void Add(PreparationRunwayPlanningReason reason, string detail) => findings.Add(new(reason, detail));

        if (context.RaceDate < context.StartDate) Add(PreparationRunwayPlanningReason.InvalidDateRange, "RaceDate must be on or after StartDate.");
        if (context.PreferredCoreWeeks <= 0) Add(PreparationRunwayPlanningReason.InvalidDerivedDuration, "PreferredCoreWeeks must be positive.");
        if (context.TargetRunsPerWeek <= 0 || context.TargetRunsPerWeek > 7) Add(PreparationRunwayPlanningReason.InvalidDerivedDuration, "TargetRunsPerWeek must be in 1..7.");
        if (context.RecentWeeklyVolumeKm < 0 || context.RecentLongestRunKm < 0 || context.RecentRunsPerWeek < 0)
            Add(PreparationRunwayPlanningReason.InvalidDerivedDuration, "Optional evidence values cannot be negative.");
        if (string.IsNullOrWhiteSpace(context.Experience.Value)) Add(PreparationRunwayPlanningReason.InvalidDerivedDuration, "Experience value is required.");

        if (context.PreferredCoreWeeks > 0)
        {
            // Phase 4G.6A.1 date-authority fix: CoreStartDate must sit exactly
            // PreferredCoreWeeks*7 EXCLUSIVE days before RaceDate -- the same
            // convention the canonical horizon-classification authority uses
            // for its own elapsed-day arithmetic (RaceDate.DayNumber minus
            // StartDate.DayNumber, no +/-1 adjustment). The prior formula,
            // `RaceDate.AddDays(-(PreferredCoreWeeks*7-1))`,
            // anchored an INCLUSIVE 84-calendar-day span (both the core-start
            // day and RaceDate counted), which disagreed with the canonical
            // exclusive horizon authority by exactly one day (e.g. a real
            // 15w0d exclusive-elapsed horizon previously validated a 22-day/
            // 3w1d runway instead of the correct 21-day/3w0d runway). This is
            // a pure day-count correction; RunwayDays' own check below
            // (CoreStartDate minus StartDate) was already exclusive and
            // correct, and is unchanged.
            var expectedCoreStart = context.RaceDate.AddDays(-(context.PreferredCoreWeeks * 7));
            if (context.CoreStartDate != expectedCoreStart) Add(PreparationRunwayPlanningReason.InvalidDateRange, "CoreStartDate does not match the canonical exclusive-day core duration ending on RaceDate.");
        }

        if (context.CompositionType == RacePlanCompositionType.PreparationRunwayPlusCore && context.RunwayDays < 0)
            Add(PreparationRunwayPlanningReason.InvalidDateRange, "RunwayDays cannot be negative for runway composition.");

        // TD-RUNWAY-VALIDATOR-EXHAUSTIVENESS-001 fix (Phase 4G.4B.1): this
        // check runs unconditionally for every caller of this method,
        // including every status via RacePlanCompositionMetadataValidator's
        // unconditional delegation from PreparationRunwayPlanningResultValidator
        // -- it is therefore NOT status-scoped and applies identically to
        // Planned/DecisionRequired/Unsupported/InvalidInput/NotApplicable
        // alike, closing the gap the cross-phase audit found (a
        // DecisionRequired/Unsupported/InvalidInput result could previously
        // carry CompositionType=StandaloneCore paired with a nonzero
        // RunwayDays, or the reverse, undetected). The rule matches every
        // existing test helper's own established convention (`Context()`/
        // `Metadata()` in PreparationRunwayContractsTests.cs already derive
        // CompositionType from runwayDays via this exact same binary rule) --
        // it is not a new invented semantic, only its enforcement is new.
        if (context.CompositionType == RacePlanCompositionType.PreparationRunwayPlusCore && context.RunwayDays == 0)
            Add(PreparationRunwayPlanningReason.InvalidDerivedDuration, "PreparationRunwayPlusCore composition requires a positive RunwayDays value; zero runway days is inconsistent with a runway-plus-core composition.");
        if (context.CompositionType != RacePlanCompositionType.PreparationRunwayPlusCore && context.RunwayDays > 0)
            Add(PreparationRunwayPlanningReason.InvalidDerivedDuration, "A positive RunwayDays value requires PreparationRunwayPlusCore composition; no other CompositionType may claim a nonzero runway.");
        if (context.RunwayDays >= 0)
        {
            var expectedRunwayDays = context.CoreStartDate.DayNumber - context.StartDate.DayNumber;
            if (context.RunwayDays != expectedRunwayDays) Add(PreparationRunwayPlanningReason.InvalidDerivedDuration, "RunwayDays does not match CoreStartDate minus StartDate.");
            if (context.FullRunwayWeeks != context.RunwayDays / 7) Add(PreparationRunwayPlanningReason.InvalidDerivedDuration, "FullRunwayWeeks does not match integer division of RunwayDays.");
            if (context.LeadingPartialDays != context.RunwayDays % 7) Add(PreparationRunwayPlanningReason.InvalidDerivedDuration, "LeadingPartialDays does not match RunwayDays remainder.");
        }
        if (context.LeadingPartialDays is < 0 or > 6) Add(PreparationRunwayPlanningReason.InvalidDerivedDuration, "LeadingPartialDays must be in 0..6.");
        return findings.Count == 0 ? PreparationRunwayValidationResult.Valid() : PreparationRunwayValidationResult.Invalid(findings);
    }
}

internal static class PreparationRunwayAllocationValidator
{
    public static PreparationRunwayValidationResult Validate(PreparationRunwayAllocation allocation, PreparationRunwayLeadingPartialSpan? partialSpan = null)
    {
        var findings = new List<PreparationRunwayPlanningFinding>();
        void Invalid(string detail) => findings.Add(new(PreparationRunwayPlanningReason.InvalidBlockAllocation, detail));

        if (allocation.FullRunwayWeeks < 0) Invalid("FullRunwayWeeks cannot be negative.");
        if (allocation.LeadingPartialDays is < 0 or > 6) Invalid("LeadingPartialDays must be in 0..6.");
        if (allocation.RunwayDays != allocation.FullRunwayWeeks * 7 + allocation.LeadingPartialDays) Invalid("RunwayDays must equal full weeks times seven plus leading partial days.");
        if (allocation.Blocks.Any(b => b.FullWeekCount <= 0)) Invalid("Every block must contain at least one full week.");
        if (allocation.Blocks.Any(b => b.SequenceIndex < 0)) Invalid("SequenceIndex cannot be negative.");
        var indices = allocation.Blocks.Select(b => b.SequenceIndex).OrderBy(i => i).ToArray();
        if (indices.Distinct().Count() != indices.Length || !indices.SequenceEqual(Enumerable.Range(0, indices.Length))) Invalid("Sequence indices must be unique and contiguous from zero.");
        if (allocation.Blocks.Sum(b => b.FullWeekCount) != allocation.FullRunwayWeeks) Invalid("Block full-week sum must equal FullRunwayWeeks; partial days cannot compensate for a missing full week.");
        if (allocation.FullRunwayWeeks == 0 && allocation.Blocks.Count != 0) Invalid("Zero full runway weeks cannot contain blocks.");
        foreach (var block in allocation.Blocks)
        {
            if (block.PrescriptionProfile == PreparationRunwayPrescriptionProfile.Light && block.BlockType != PreparationRunwayBlockType.AerobicStrength)
                Invalid("Light is representable only as an unresolved AerobicStrength prescription profile, never as an independent block.");
            if (block.MinimumWeeks is < 0 || block.MaximumWeeks is < 0 || block.MinimumWeeks > block.MaximumWeeks) Invalid("Supplied duration constraints are invalid.");
            if (block.MinimumWeeks is int min && block.FullWeekCount < min) Invalid("Block is below its supplied minimum.");
            if (block.MaximumWeeks is int max && block.FullWeekCount > max) Invalid("Block exceeds its supplied maximum.");
        }

        if (allocation.LeadingPartialDays == 0 && partialSpan is not null) Invalid("Leading partial span must be absent when LeadingPartialDays is zero.");
        if (allocation.LeadingPartialDays > 0 && partialSpan is null) Invalid("Leading partial span is required when LeadingPartialDays is nonzero.");
        if (partialSpan is not null)
        {
            if (partialSpan.DayCount is < 1 or > 6 || partialSpan.DayCount != allocation.LeadingPartialDays) Invalid("Partial span DayCount must equal LeadingPartialDays and be in 1..6.");
            if (partialSpan.EndDate.DayNumber - partialSpan.StartDate.DayNumber + 1 != partialSpan.DayCount) Invalid("Partial span dates must match its inclusive DayCount.");
            if (!partialSpan.AllowsZeroSessions || partialSpan.AllowsQualityProgression) Invalid("Partial spans must allow zero sessions and prohibit quality progression.");
            var first = allocation.Blocks.OrderBy(b => b.SequenceIndex).FirstOrDefault();
            if (first is null || first.BlockType != partialSpan.InheritedBlockType || first.PrescriptionProfile != partialSpan.InheritedPrescriptionProfile)
                Invalid("Partial span must inherit the first full-week block without reducing or replacing it.");
        }
        return findings.Count == 0 ? PreparationRunwayValidationResult.Valid() : PreparationRunwayValidationResult.Invalid(findings);
    }
}

internal static class RacePlanCompositionMetadataValidator
{
    public static PreparationRunwayValidationResult Validate(RacePlanCompositionMetadata metadata)
    {
        var context = new PreparationRunwayContext(
            default, new(PreparationRunwayExperienceVocabulary.PlanCatalogRunningExperience, "UNRESOLVED"),
            new(PreparationRunwayNeedLevel.NotEvaluated, PreparationRunwayNeedLevel.NotEvaluated, PreparationRunwayNeedLevel.NotEvaluated, PreparationRunwayNeedLevel.NotEvaluated, PreparationRunwayNeedLevel.NotEvaluated, PreparationRunwayNeedLevel.NotEvaluated),
            null, null, null, 1, metadata.StartDate, metadata.CoreStartDate, metadata.RaceDate,
            metadata.RunwayDays, metadata.FullRunwayWeeks, metadata.LeadingPartialDays, metadata.PreferredCoreWeeks, metadata.CompositionType);
        var findings = PreparationRunwayContextValidator.Validate(context).Findings.ToList();
        if (metadata.PreferredCoreDays != metadata.PreferredCoreWeeks * 7) findings.Add(new(PreparationRunwayPlanningReason.InvalidDerivedDuration, "PreferredCoreDays must equal PreferredCoreWeeks times seven."));
        if (metadata.TotalAvailableDays != metadata.RaceDate.DayNumber - metadata.StartDate.DayNumber + 1) findings.Add(new(PreparationRunwayPlanningReason.InvalidDerivedDuration, "TotalAvailableDays must be inclusive StartDate through RaceDate."));
        if ((metadata.LeadingPartialDays == 0) != (metadata.LeadingPartialSpan is null)) findings.Add(new(PreparationRunwayPlanningReason.InvalidDerivedDuration, "Partial-span presence must match LeadingPartialDays."));
        return findings.Count == 0 ? PreparationRunwayValidationResult.Valid() : PreparationRunwayValidationResult.Invalid(findings);
    }
}

internal static class PreparationRunwayPlanningResultValidator
{
    public static PreparationRunwayValidationResult Validate(PreparationRunwayPlanningResult result)
    {
        var findings = new List<PreparationRunwayPlanningFinding>();
        void Invalid(string detail) => findings.Add(new(PreparationRunwayPlanningReason.InvalidBlockAllocation, detail));
        var decisionReasons = new[] { PreparationRunwayPlanningReason.ExperienceMappingUnresolved, PreparationRunwayPlanningReason.AerobicStrengthLightSemanticsUnresolved, PreparationRunwayPlanningReason.ShortRunwayRouteSelectionUnresolved, PreparationRunwayPlanningReason.BlockCountRouteConflict, PreparationRunwayPlanningReason.ReadinessRoutingUnresolved };
        var unsupportedReasons = new[] { PreparationRunwayPlanningReason.LongRunwayCapacityExceeded, PreparationRunwayPlanningReason.LongRunwayContinuationPolicyMissing, PreparationRunwayPlanningReason.InsufficientEffectiveRunway };
        var invalidReasons = new[] { PreparationRunwayPlanningReason.InvalidDateRange, PreparationRunwayPlanningReason.InvalidDerivedDuration, PreparationRunwayPlanningReason.InvalidBlockAllocation };

        if (result.Status == PreparationRunwayPlanningStatus.Planned)
        {
            if (result.Allocation is null) Invalid("Planned requires an allocation.");
            else findings.AddRange(PreparationRunwayAllocationValidator.Validate(result.Allocation, result.Composition.LeadingPartialSpan).Findings);
            if (result.Findings.Count != 0) Invalid("Planned cannot carry unresolved findings.");
        }
        else if (result.Allocation is not null) Invalid("Non-Planned outcomes cannot claim an executable allocation.");

        if (result.Status == PreparationRunwayPlanningStatus.DecisionRequired && !result.Findings.Any(f => decisionReasons.Contains(f.Reason))) Invalid("DecisionRequired needs a decision-required reason.");
        if (result.Status == PreparationRunwayPlanningStatus.Unsupported && !result.Findings.Any(f => unsupportedReasons.Contains(f.Reason))) Invalid("Unsupported needs an unsupported reason.");
        if (result.Status == PreparationRunwayPlanningStatus.InvalidInput && !result.Findings.Any(f => invalidReasons.Contains(f.Reason))) Invalid("InvalidInput needs a validation reason.");
        if (result.Status == PreparationRunwayPlanningStatus.NotApplicable && (result.Allocation is not null || result.Composition.CompositionType == RacePlanCompositionType.PreparationRunwayPlusCore || result.Composition.RunwayDays != 0)) Invalid("NotApplicable cannot claim runway days, runway composition, or allocation.");
        findings.AddRange(RacePlanCompositionMetadataValidator.Validate(result.Composition).Findings);
        return findings.Count == 0 ? PreparationRunwayValidationResult.Valid() : PreparationRunwayValidationResult.Invalid(findings);
    }
}
