using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlanCatalog.Infrastructure.Serialization;

/// <summary>Serializes string sets in ordinal sorted order so canonical hashing is deterministic regardless of insertion order.</summary>
public sealed class ReadOnlyStringSetConverter : JsonConverter<IReadOnlySet<string>>
{
    public override IReadOnlySet<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var items = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
        return new HashSet<string>(items, StringComparer.Ordinal);
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlySet<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value.OrderBy(x => x, StringComparer.Ordinal))
        {
            writer.WriteStringValue(item);
        }

        writer.WriteEndArray();
    }
}
