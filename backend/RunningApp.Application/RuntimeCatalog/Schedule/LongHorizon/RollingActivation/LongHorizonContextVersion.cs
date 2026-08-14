namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.5 Part 13 — minimal version/value semantics for a checkpoint or
/// JIT decision. Immutable: <see cref="Next"/> returns a new instance, it
/// never mutates an existing version in place, matching the "prior context
/// versions are not mutated" requirement.
/// </summary>
internal sealed record LongHorizonContextVersion
{
    public required Guid VersionId { get; init; }
    public required int Sequence { get; init; }
    public DateTime? CreatedAt { get; init; }

    public static LongHorizonContextVersion Initial(DateTime? createdAt = null) => new()
    {
        VersionId = Guid.NewGuid(),
        Sequence = 1,
        CreatedAt = createdAt,
    };

    /// <summary>
    /// Phase 4K.6 deterministic initial-activation seam. The caller derives
    /// the stable identity from immutable request/catalog inputs; no random
    /// identifier enters the dark domain result.
    /// </summary>
    public static LongHorizonContextVersion Initial(Guid versionId, DateTime? createdAt = null) => new()
    {
        VersionId = versionId,
        Sequence = 1,
        CreatedAt = createdAt,
    };

    public LongHorizonContextVersion Next(DateTime? createdAt = null) => new()
    {
        VersionId = Guid.NewGuid(),
        Sequence = Sequence + 1,
        CreatedAt = createdAt,
    };

    public LongHorizonContextVersion Next(Guid versionId, DateTime? createdAt = null) => new()
    {
        VersionId = versionId,
        Sequence = Sequence + 1,
        CreatedAt = createdAt,
    };
}
