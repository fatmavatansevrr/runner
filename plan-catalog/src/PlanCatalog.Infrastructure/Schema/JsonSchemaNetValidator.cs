using System.Text.Json.Nodes;
using Json.Schema;
using PlanCatalog.Core.Validation;
using PlanCatalog.Core.Ports;

namespace PlanCatalog.Infrastructure.Schema;

public sealed class JsonSchemaNetValidator : IJsonSchemaValidator
{
    private readonly Dictionary<string, JsonSchema> _schemasByDocumentType = new(StringComparer.Ordinal);

    public JsonSchemaNetValidator(string schemasDirectory)
    {
        foreach (var (documentType, fileName) in SchemaFileMap.ByDocumentType)
        {
            var path = Path.Combine(schemasDirectory, fileName);
            if (File.Exists(path))
            {
                _schemasByDocumentType[documentType] = JsonSchema.FromFile(path);
            }
        }
    }

    public ValidationResult Validate(string documentType, string json)
    {
        if (!_schemasByDocumentType.TryGetValue(documentType, out var schema))
        {
            return new ValidationResult(
            [
                new ValidationIssue("SCHEMA_NOT_FOUND", ValidationSeverity.Error, $"No JSON Schema registered for documentType '{documentType}'.")
            ]);
        }

        var node = JsonNode.Parse(json);
        var evaluation = schema.Evaluate(node, new EvaluationOptions { OutputFormat = OutputFormat.List });

        if (evaluation.IsValid)
        {
            return ValidationResult.Empty;
        }

        var issues = new List<ValidationIssue>();
        CollectErrors(evaluation, issues);
        return new ValidationResult(issues);
    }

    private static void CollectErrors(EvaluationResults results, List<ValidationIssue> issues)
    {
        if (!results.IsValid && results.Errors is not null)
        {
            foreach (var (key, message) in results.Errors)
            {
                issues.Add(new ValidationIssue("SCHEMA_VALIDATION_ERROR", ValidationSeverity.Error, $"{key}: {message}", results.InstanceLocation.ToString()));
            }
        }

        if (results.Details is not null)
        {
            foreach (var detail in results.Details)
            {
                CollectErrors(detail, issues);
            }
        }
    }
}
