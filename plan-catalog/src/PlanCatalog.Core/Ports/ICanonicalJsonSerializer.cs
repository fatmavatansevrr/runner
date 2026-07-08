namespace PlanCatalog.Core.Ports;

/// <summary>
/// Produces deterministic canonical JSON: ordinal dictionary-key ordering, invariant decimals,
/// UPPER_SNAKE_CASE enums, no CLR type names, null optional fields omitted.
/// </summary>
public interface ICanonicalJsonSerializer
{
    string Serialize<T>(T value);

    T Deserialize<T>(string json);
}
