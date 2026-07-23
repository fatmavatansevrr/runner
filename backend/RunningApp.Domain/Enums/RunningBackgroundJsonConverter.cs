using System.Text.Json;
using System.Text.Json.Serialization;

namespace RunningApp.Domain.Enums;

/// <summary>
/// Running Background V2.1 — HISTORICAL-COMPATIBILITY-ONLY JSON converter for
/// <see cref="RunningBackground"/>. Do not attach this to any public request
/// or response DTO — new public HTTP requests must reject legacy aliases
/// with a typed validation error (see <see cref="RunningBackgroundCanonicalJsonConverter"/>,
/// which owns that boundary via <c>GeneratePreviewRequest.Level</c>'s
/// property-level <c>[JsonConverter]</c> attribute).
///
/// This converter exists solely for reading the internal, immutable
/// <c>ResolverInputSnapshot.Level</c> field embedded in already-persisted
/// <c>PlanPreviews.PreviewPayloadJson</c> snapshot rows created before
/// Running Background V2 (confirmed present in the real local dev database:
/// 166 of 221 stored preview rows contain the legacy wire value
/// "running_regularly" in their embedded <c>normalized_input.level</c> —
/// all already expired/unconfirmable, but the snapshot JSON is hash-verified
/// and must never be mutated in place, so a narrowly-scoped legacy reader is
/// the only safe way to keep it deserializable). It must never be applied to
/// any type that participates in a new public request or response contract.
///
/// Write: always emits the canonical snake_case value ("beginner",
/// "intermediate", "advanced", "experienced") — legacy aliases are never
/// emitted by new code, even when read from a legacy-tagged snapshot.
///
/// Read: accepts the four canonical values AND the three legacy aliases
/// ("new_to_running", "used_to_run", "running_regularly") for backward
/// compatibility with pre-V2 stored snapshot JSON. Any other string is a
/// typed <see cref="JsonException"/> — compatibility parsing never silently
/// reinterprets an unrelated string.
///
/// Legacy alias mapping (historical only — see
/// RUNNING_BACKGROUND_V2_FRONTEND_BACKEND_ALIGNMENT.md and
/// RUNNING_BACKGROUND_V2_1_INTERMEDIATE_PILOT_CLOSURE.md for the full
/// rationale). None of these three values is a current product option:
/// <list type="bullet">
/// <item>"new_to_running" → Beginner</item>
/// <item>"used_to_run" → Beginner (the approved Beginner label text is
/// "I'm new to running or getting back into it." — "getting back into it"
/// is the exact prior semantic of "used_to_run" / "Returning after a
/// break")</item>
/// <item>"running_regularly" → Intermediate (preserves the pre-existing
/// Intermediate ↔ catalog INTERMEDIATE pilot equivalence)</item>
/// </list>
/// </summary>
public sealed class RunningBackgroundJsonConverter : JsonConverter<RunningBackground>
{
    private static readonly IReadOnlyDictionary<string, RunningBackground> LegacyAliases =
        new Dictionary<string, RunningBackground>(StringComparer.Ordinal)
        {
            ["new_to_running"] = RunningBackground.Beginner,
            ["used_to_run"] = RunningBackground.Beginner,
            ["running_regularly"] = RunningBackground.Intermediate,
        };

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

        if (LegacyAliases.TryGetValue(raw, out var legacyMapped))
        {
            return legacyMapped;
        }

        throw new JsonException(
            $"Unknown RunningBackground value '{raw}'. Expected one of: beginner, intermediate, advanced, experienced " +
            "(legacy aliases new_to_running, used_to_run, running_regularly are also accepted here for historical " +
            "snapshot-read compatibility only — this is not a public API contract).");
    }

    public override void Write(Utf8JsonWriter writer, RunningBackground value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString()));
    }
}
