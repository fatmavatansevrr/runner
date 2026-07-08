using PlanCatalog.Core.Validation;

namespace PlanCatalog.Core.Ports;

/// <summary>Validates a canonical JSON document against the JSON Schema for its documentType.</summary>
public interface IJsonSchemaValidator
{
    ValidationResult Validate(string documentType, string json);
}
