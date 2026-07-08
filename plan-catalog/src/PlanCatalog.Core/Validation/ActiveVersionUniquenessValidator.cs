using PlanCatalog.Core.Models;
using PlanCatalog.Core.Ports;

namespace PlanCatalog.Core.Validation;

/// <summary>
/// Milestone G (Part 3 preparation only) — see artifacts/audits/part3-retirement-and-release-plan.md.
/// Fails when more than one non-retired, publish-eligible version of the same combination key is
/// presented for a new full-catalog release. This is a standalone policy component: it is NOT wired into
/// <c>CatalogPublisher</c> in Part 2 and does not run against the real catalog automatically — the real
/// catalog intentionally remains ambiguous (TEN_K__4D__INTERMEDIATE v1/v2/v3/v4 all non-retired) until
/// Part 3 retires the superseded roots. Exercised here only against isolated test fixtures.
/// </summary>
public static class ActiveVersionUniquenessValidator
{
    public static ValidationResult Validate(IEnumerable<TemplateCombinationDefinition> combinations, IRetirementLedger? retirementLedger = null)
    {
        var retirement = retirementLedger ?? NullRetirementLedger.Instance;
        var issues = new List<ValidationIssue>();

        var eligibleByKey = combinations
            .Where(c => !retirement.IsRetired(c.Metadata.DocumentType, c.Metadata.Key, c.Metadata.Version))
            .GroupBy(c => c.Metadata.Key);

        foreach (var group in eligibleByKey)
        {
            var versions = group.Select(c => c.Metadata.Version).OrderBy(v => v).ToList();
            if (versions.Count > 1)
            {
                issues.Add(new ValidationIssue("ACTIVE_COMBINATION_VERSION_NOT_UNIQUE", ValidationSeverity.Error,
                    $"Combination key '{group.Key}' has {versions.Count} non-retired, publish-eligible versions ({string.Join(", ", versions)}); exactly one active version is required for a new full-catalog release.", "$"));
            }
        }

        return new ValidationResult(issues);
    }
}
