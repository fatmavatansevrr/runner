using System.Text.Json;
using System.Text.Json.Serialization;
using RunningApp.Application.RuntimeCatalog.Resolvers;

namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.2 — custom JSON converter for
/// <see cref="RuntimeConditionResolutionResult"/> used during snapshot
/// deserialization at confirm time.
///
/// <see cref="RuntimeConditionResolutionResult"/> has a private constructor
/// (enforcing Status/OutputValue invariants by construction) and cannot be
/// deserialized by System.Text.Json's default ObjectDefaultConverter.
/// This converter reconstructs instances via the public factory methods
/// <see cref="RuntimeConditionResolutionResult.Evaluated"/> and
/// <see cref="RuntimeConditionResolutionResult.NotEvaluated"/>, preserving
/// the same invariants enforced at generation time.
///
/// This converter is ONLY used during snapshot read at confirm time — it is
/// NOT registered in the global JSON serializer options and does NOT affect
/// any public API response serialization.
/// </summary>
public sealed class RuntimeConditionResolutionResultConverter : JsonConverter<RuntimeConditionResolutionResult>
{
    public override RuntimeConditionResolutionResult? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        string? conditionType = null;
        string? statusString = null;
        string? outputValue = null;
        string? reasonCode = null;
        IReadOnlyDictionary<string, string>? metadata = null;

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Support both snake_case (catalog serialization) and PascalCase
        if (root.TryGetProperty("condition_type", out var ct) || root.TryGetProperty("ConditionType", out ct))
            conditionType = ct.GetString();
        if (root.TryGetProperty("status", out var st) || root.TryGetProperty("Status", out st))
            statusString = st.GetString();
        if (root.TryGetProperty("output_value", out var ov) || root.TryGetProperty("OutputValue", out ov))
            outputValue = ov.GetString();
        if (root.TryGetProperty("reason_code", out var rc) || root.TryGetProperty("ReasonCode", out rc))
            reasonCode = rc.GetString();
        if (root.TryGetProperty("metadata", out var md) || root.TryGetProperty("Metadata", out md))
            metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(md.GetRawText(), options);

        conditionType ??= string.Empty;
        reasonCode ??= string.Empty;

        // Status is serialized as "NotEvaluated" or "not_evaluated" depending on options.
        var isNotEvaluated =
            string.IsNullOrEmpty(statusString) ||
            string.Equals(statusString, "NotEvaluated", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(statusString, "not_evaluated", StringComparison.OrdinalIgnoreCase);

        if (isNotEvaluated || string.IsNullOrWhiteSpace(outputValue))
        {
            return RuntimeConditionResolutionResult.NotEvaluated(
                conditionType, reasonCode, metadata: metadata);
        }
        else
        {
            return RuntimeConditionResolutionResult.Evaluated(
                conditionType, outputValue, reasonCode, metadata: metadata);
        }
    }

    public override void Write(
        Utf8JsonWriter writer,
        RuntimeConditionResolutionResult value,
        JsonSerializerOptions options)
    {
        // Serialize as a plain object: only the fields needed for later deserialization.
        // This matches the shape produced by the anonymous object in
        // CatalogPreviewSnapshotBuilder.Build's resolverResults projection.
        writer.WriteStartObject();
        writer.WriteString("condition_type", value.ConditionType);
        writer.WriteString("status", value.Status.ToString());
        if (value.OutputValue != null)
            writer.WriteString("output_value", value.OutputValue);
        else
            writer.WriteNull("output_value");
        writer.WriteString("reason_code", value.ReasonCode);
        writer.WriteStartObject("metadata");
        foreach (var kvp in value.Metadata)
        {
            writer.WriteString(kvp.Key, kvp.Value);
        }
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
