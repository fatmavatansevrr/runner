namespace PlanCatalog.Contracts.Manifests;

/// <summary>Records that a released artifact contains domain content not yet confirmed against a canonical source.</summary>
public sealed record UnconfirmedContentWarning
{
    public required string DocumentType { get; init; }
    public required string Key { get; init; }
    public required int Version { get; init; }
    public required string Message { get; init; }
}
