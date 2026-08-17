using System.Text.Json;
using System.Text.Json.Serialization;
using PlanCatalog.Contracts.Bundles;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Execution;

/// <summary>
/// Phase 10K-FREQ.6D.3D — converts a PascalCase enum member name to UPPER_SNAKE_CASE.
/// Deliberately duplicated (not referenced) from
/// <c>PlanCatalog.Infrastructure.Serialization.UpperSnakeCaseNamingPolicy</c>: RunningApp.Application
/// must not reference PlanCatalog.Infrastructure (see RunningAppPlanCatalogDependencyTests). This is
/// a generic JSON-naming utility, not execution/prescription domain logic, so duplicating it does not
/// create a second execution-authority source — it only needs to byte-for-byte match the canonical
/// naming convention Process A already publishes bundles with.
/// </summary>
internal sealed class ExecutionBoundaryUpperSnakeCaseNamingPolicy : JsonNamingPolicy
{
    public static readonly ExecutionBoundaryUpperSnakeCaseNamingPolicy Instance = new();

    public override string ConvertName(string name)
    {
        var builder = new System.Text.StringBuilder(name.Length * 2);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
            {
                var previous = name[i - 1];
                var isNewWordStart = char.IsLower(previous) || char.IsDigit(previous) ||
                    (char.IsUpper(previous) && i + 1 < name.Length && char.IsLower(name[i + 1]));

                if (isNewWordStart)
                {
                    builder.Append('_');
                }
            }

            builder.Append(char.ToUpperInvariant(c));
        }

        return builder.ToString();
    }
}

/// <summary>
/// Phase 10K-FREQ.6D.3D — the Process A→B JSON crossing point for a <see cref="PublishedTemplateBundle"/>.
/// Deserializes exactly the immutable Contracts shape; performs no field mapping, no flattening, no
/// profile resolution. Options mirror <c>PlanCatalog.Infrastructure.Serialization.CanonicalJsonOptions</c>
/// (camelCase properties, UPPER_SNAKE_CASE enum members) so a bundle published by the real Process A
/// pipeline round-trips through this reader unchanged.
/// </summary>
internal static class PublishedTemplateBundleJsonReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(ExecutionBoundaryUpperSnakeCaseNamingPolicy.Instance) },
    };

    public static PublishedTemplateBundle Read(string json)
    {
        return JsonSerializer.Deserialize<PublishedTemplateBundle>(json, Options)
            ?? throw new ExecutionPrescriptionContractInvalidException("Published template bundle JSON deserialized to null.");
    }

    /// <summary>Test/fixture-only symmetric writer, using the same options, for round-trip proofs. Never used by production ingestion (which only ever reads real published bundles).</summary>
    internal static string Write(PublishedTemplateBundle bundle) => JsonSerializer.Serialize(bundle, Options);
}
