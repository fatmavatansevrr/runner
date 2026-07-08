using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlanCatalog.Infrastructure.Serialization;

public static class CanonicalJsonOptions
{
    public static JsonSerializerOptions Create(bool indented)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = indented,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        options.Converters.Add(new JsonStringEnumConverter(UpperSnakeCaseNamingPolicy.Instance));
        options.Converters.Add(new ReadOnlyStringSetConverter());

        return options;
    }

    /// <summary>The single canonical (non-indented) options instance used for hashing and published output.</summary>
    public static JsonSerializerOptions Canonical { get; } = Create(indented: false);

    /// <summary>Indented options for human-edited authoring source files.</summary>
    public static JsonSerializerOptions Pretty { get; } = Create(indented: true);
}
