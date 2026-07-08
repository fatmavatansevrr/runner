using System.Text.Json;
using PlanCatalog.Core.Ports;

namespace PlanCatalog.Infrastructure.Serialization;

public sealed class SystemTextJsonCanonicalSerializer : ICanonicalJsonSerializer
{
    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, CanonicalJsonOptions.Canonical);

    public T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, CanonicalJsonOptions.Canonical)
        ?? throw new InvalidOperationException($"Deserialization of {typeof(T).Name} produced null.");
}
