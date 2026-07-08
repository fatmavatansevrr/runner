using PlanCatalog.Contracts;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Models;

namespace PlanCatalog.Core.Validation;

public static class RulePackValidator
{
    public static ValidationResult Validate(RulePackDefinition rulePack, CatalogSourceSnapshot snapshot)
    {
        var issues = new List<ValidationIssue>();

        if (snapshot.FindPeakVolumeBandPolicy(rulePack.PeakVolumeBandPolicy) is null)
        {
            issues.Add(new ValidationIssue("RP_PEAK_VOLUME_BAND_POLICY_MISSING", ValidationSeverity.Error,
                $"Referenced PeakVolumeBandPolicy '{rulePack.PeakVolumeBandPolicy.Key}' v{rulePack.PeakVolumeBandPolicy.Version} was not found.", "$.peakVolumeBandPolicy"));
        }

        if (snapshot.FindRuntimeConditionValueRegistry(rulePack.RuntimeConditionValueRegistry) is null)
        {
            issues.Add(new ValidationIssue("RP_RUNTIME_CONDITION_VALUE_REGISTRY_MISSING", ValidationSeverity.Error,
                $"Referenced RuntimeConditionValueRegistry '{rulePack.RuntimeConditionValueRegistry.Key}' v{rulePack.RuntimeConditionValueRegistry.Version} was not found.", "$.runtimeConditionValueRegistry"));
        }

        var allRefs = rulePack.Policies.Concat(rulePack.Rules).ToList();
        var duplicates = allRefs
            .GroupBy(r => (r.DocumentType, r.Key, r.Version))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            issues.Add(new ValidationIssue("RP_DUPLICATE_REFERENCE", ValidationSeverity.Error,
                $"Duplicate policy/rule references: {string.Join("; ", duplicates)}.", "$.policies|$.rules"));
        }

        foreach (var reference in allRefs)
        {
            var status = snapshot.FindStatus(reference);
            if (status is null)
            {
                issues.Add(new ValidationIssue("RP_REFERENCE_MISSING", ValidationSeverity.Error,
                    $"Referenced artifact '{reference.DocumentType}/{reference.Key}' v{reference.Version} was not found.", "$.policies|$.rules"));
            }
            else if (status < CatalogStatus.Validated)
            {
                issues.Add(new ValidationIssue("RP_REFERENCE_NOT_VALIDATED", ValidationSeverity.Error,
                    $"Referenced artifact '{reference.DocumentType}/{reference.Key}' v{reference.Version} must be at least VALIDATED (found {status}).", "$.policies|$.rules"));
            }
        }

        if (HasCircularRulePackDependency(rulePack, snapshot, new HashSet<string>(StringComparer.Ordinal)))
        {
            issues.Add(new ValidationIssue("RP_CIRCULAR_DEPENDENCY", ValidationSeverity.Error,
                $"Rule pack '{rulePack.Metadata.Key}' participates in a circular rule-pack dependency chain.", "$.policies|$.rules"));
        }

        return new ValidationResult(issues);
    }

    private static bool HasCircularRulePackDependency(RulePackDefinition rulePack, CatalogSourceSnapshot snapshot, HashSet<string> visited)
    {
        var selfId = $"{rulePack.Metadata.Key}@{rulePack.Metadata.Version}";
        if (!visited.Add(selfId))
        {
            return true;
        }

        var nestedRulePackRefs = rulePack.Policies.Concat(rulePack.Rules)
            .Where(r => r.DocumentType == DocumentTypes.RulePack);

        foreach (var nestedRef in nestedRulePackRefs)
        {
            var nested = snapshot.FindRulePack(new VersionedCatalogReference
            {
                DocumentType = nestedRef.DocumentType,
                Key = nestedRef.Key,
                Version = nestedRef.Version
            });

            if (nested is not null && HasCircularRulePackDependency(nested, snapshot, visited))
            {
                return true;
            }
        }

        return false;
    }
}
