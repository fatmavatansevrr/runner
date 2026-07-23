using System.Text.Json;
using System.Text.Json.Serialization;

namespace RunningApp.Domain.Enums;

/// <summary>
/// Running Background V2.1 — the PUBLIC-BOUNDARY JSON converter for
/// <see cref="RunningBackground"/>. This is the type-level converter (via
/// <c>[JsonConverter]</c> on the enum declaration), so it is the default for
/// any new DTO, request, response, or Swagger/OpenAPI example unless a type
/// explicitly opts into historical compatibility via
/// <see cref="RunningBackgroundJsonConverter"/> (internal-snapshot-read use
/// only — see that type's docs).
///
/// Only the four canonical values are accepted or ever emitted:
/// "beginner", "intermediate", "advanced", "experienced". The three legacy
/// aliases ("new_to_running", "used_to_run", "running_regularly") are NOT
/// current product options and are rejected here with a typed
/// <see cref="JsonException"/> (surfaced as an HTTP 400 model-validation
/// error) — the public API contract accepts canonical values only, full
/// stop. There is no versioned legacy endpoint that accepts the old values.
/// </summary>
public sealed class RunningBackgroundCanonicalJsonConverter : JsonConverter<RunningBackground>
{
    public override RunningBackground Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new JsonException("RunningBackground value must not be null or empty.");
        }

        foreach (var canonical in Enum.GetValues<RunningBackground>())
        {
            if (string.Equals(JsonNamingPolicy.SnakeCaseLower.ConvertName(canonical.ToString()), raw, StringComparison.Ordinal))
            {
                return canonical;
            }
        }

        throw new JsonException(
            $"Unknown RunningBackground value '{raw}'. Expected one of: beginner, intermediate, advanced, experienced. " +
            "The values new_to_running, used_to_run, and running_regularly were removed in Running Background V2.1 " +
            "and are no longer accepted by this endpoint.");
    }

    public override void Write(Utf8JsonWriter writer, RunningBackground value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString()));
    }
}
