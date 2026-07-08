using PlanCatalog.Core.Models;

namespace PlanCatalog.Core.Validation;

public static class PeakVolumeBandPolicyValidator
{
    public static ValidationResult Validate(PeakVolumeBandPolicy policy)
    {
        var issues = new List<ValidationIssue>();

        var duplicateTuples = policy.Entries
            .GroupBy(e => (e.DistanceFamily, e.Experience, e.RunsPerWeek))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateTuples.Count > 0)
        {
            issues.Add(new ValidationIssue("PVB_DUPLICATE_TUPLE", ValidationSeverity.Error,
                $"Duplicate (DistanceFamily, Experience, RunsPerWeek) tuples: {string.Join("; ", duplicateTuples)}.", "$.entries"));
        }

        foreach (var entry in policy.Entries)
        {
            if (entry.MinimumKm < 0)
            {
                issues.Add(new ValidationIssue("PVB_MINIMUM_NEGATIVE", ValidationSeverity.Error,
                    $"MinimumKm must be >= 0 for {(entry.DistanceFamily, entry.Experience, entry.RunsPerWeek)}.", "$.entries"));
            }

            if (entry.MinimumKm > entry.MaximumKm)
            {
                issues.Add(new ValidationIssue("PVB_MINIMUM_EXCEEDS_MAXIMUM", ValidationSeverity.Error,
                    $"MinimumKm exceeds MaximumKm for {(entry.DistanceFamily, entry.Experience, entry.RunsPerWeek)}.", "$.entries"));
            }
        }

        return new ValidationResult(issues);
    }
}
