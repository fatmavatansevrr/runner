using System.Text.RegularExpressions;
using PlanCatalog.Contracts;
using PlanCatalog.Core.Models;

namespace PlanCatalog.Core.Validation;

public static partial class RuntimeConditionValueRegistryValidator
{
    public static ValidationResult Validate(RuntimeConditionValueRegistryDefinition registry)
    {
        var issues = new List<ValidationIssue>();

        if (registry.Metadata.DocumentType != DocumentTypes.RuntimeConditionValueRegistry)
        {
            issues.Add(new ValidationIssue("RCVR_DOCUMENT_TYPE_MISMATCH", ValidationSeverity.Error,
                $"Expected documentType '{DocumentTypes.RuntimeConditionValueRegistry}' but found '{registry.Metadata.DocumentType}'.", "$.metadata.documentType"));
        }

        var duplicateConditionTypes = registry.ConditionValueSets
            .GroupBy(v => v.ConditionType)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateConditionTypes.Count > 0)
        {
            issues.Add(new ValidationIssue("RCVR_DUPLICATE_CONDITION_TYPE", ValidationSeverity.Error,
                $"Each RuntimeConditionType may have at most one value set; duplicated: {string.Join(", ", duplicateConditionTypes)}.", "$.conditionValueSets"));
        }

        foreach (var valueSet in registry.ConditionValueSets)
        {
            if (valueSet.AllowedValues.Count == 0)
            {
                issues.Add(new ValidationIssue("RCVR_ALLOWED_VALUES_EMPTY", ValidationSeverity.Error,
                    $"AllowedValues for '{valueSet.ConditionType}' cannot be empty.", "$.conditionValueSets"));
            }

            var invalidFormat = valueSet.AllowedValues.Where(v => !UpperSnakeCaseRegex().IsMatch(v)).ToList();
            if (invalidFormat.Count > 0)
            {
                issues.Add(new ValidationIssue("RCVR_VALUE_NOT_UPPER_SNAKE_CASE", ValidationSeverity.Error,
                    $"Values for '{valueSet.ConditionType}' are not UPPER_SNAKE_CASE: {string.Join(", ", invalidFormat)}.", "$.conditionValueSets"));
            }
        }

        return new ValidationResult(issues);
    }

    [GeneratedRegex("^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*$")]
    private static partial Regex UpperSnakeCaseRegex();
}
