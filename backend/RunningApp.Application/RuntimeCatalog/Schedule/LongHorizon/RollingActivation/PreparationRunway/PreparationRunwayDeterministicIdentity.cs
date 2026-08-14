using System.Security.Cryptography;
using System.Text;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;

/// <summary>
/// Phase 4K.8B Part 5/Part 20 — the same deterministic SHA-256-based stable
/// identity convention already used by <c>LongHorizonRollingCheckpointRuntime</c>
/// and <c>LongHorizonRollingInitialActivationRuntime</c>. No random GUID or
/// wall clock enters a prescription/slice identity.
/// </summary>
internal static class PreparationRunwayDeterministicIdentity
{
    public static Guid StableGuid(string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, 16));
}

/// <summary>Phase 4K.8B Part 5 — deterministic identity of one full Runway prescription.</summary>
internal sealed record PreparationRunwayPrescriptionId(Guid Value);

/// <summary>Phase 4K.8B Part 5 — deterministic version of one full Runway prescription, wrapping the existing <see cref="LongHorizonContextVersion"/> primitive.</summary>
internal sealed record PreparationRunwayPrescriptionVersion(LongHorizonContextVersion Version);
