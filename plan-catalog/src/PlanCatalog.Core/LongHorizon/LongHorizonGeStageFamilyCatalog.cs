using System.Text.Json;

namespace PlanCatalog.Core.LongHorizon;

/// <summary>
/// Typed, minimal parse of a <c>LONG_HORIZON_GE_STAGE_FAMILY_CATALOG</c>
/// document (schema: <c>schemas/long-horizon-ge-stage-family.schema.json</c>).
/// Deliberately its own small parser rather than reusing the full
/// <c>FileSystemCatalogSourceRepository</c> pipeline, since that repository's
/// <c>LoadSnapshot</c> only ever scans a fixed, hardcoded list of subfolders
/// (never <c>long-horizon-progressions</c>) -- exactly the inertness this
/// phase's containment requirement relies on (see the Preparation Runway's
/// own identical <c>preparation-runway-progressions</c> precedent).
/// </summary>
public sealed record GeWorkoutCandidate(string Key, int Version);

public sealed record GeRoleAssignment(string Role, string Profile, IReadOnlyList<GeWorkoutCandidate> WorkoutCandidates);

public sealed record GeStageFamilyDefinition(
    string StageFamilyKey,
    IReadOnlyList<string> EligibleContexts,
    bool RecoveryCompatible,
    int MaxConsecutiveMesocycles,
    IReadOnlyList<GeRoleAssignment> RoleAssignments);

public sealed record LongHorizonGeStageFamilyCatalogDocument(
    string Key,
    int Version,
    string DistanceFamily,
    IReadOnlyList<GeStageFamilyDefinition> StageFamilies);

public static class LongHorizonGeStageFamilyCatalogLoader
{
    public static LongHorizonGeStageFamilyCatalogDocument Load(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var metadata = root.GetProperty("metadata");

        var stageFamilies = root.GetProperty("stageFamilies").EnumerateArray()
            .Select(ParseStageFamily)
            .ToList();

        return new LongHorizonGeStageFamilyCatalogDocument(
            Key: metadata.GetProperty("key").GetString()!,
            Version: metadata.GetProperty("version").GetInt32(),
            DistanceFamily: root.GetProperty("distanceFamily").GetString()!,
            StageFamilies: stageFamilies);
    }

    private static GeStageFamilyDefinition ParseStageFamily(JsonElement element) => new(
        StageFamilyKey: element.GetProperty("stageFamilyKey").GetString()!,
        EligibleContexts: element.GetProperty("eligibleContexts").EnumerateArray().Select(e => e.GetString()!).ToList(),
        RecoveryCompatible: element.GetProperty("recoveryCompatible").GetBoolean(),
        MaxConsecutiveMesocycles: element.GetProperty("maxConsecutiveMesocycles").GetInt32(),
        RoleAssignments: element.GetProperty("roleAssignments").EnumerateArray().Select(ParseRoleAssignment).ToList());

    private static GeRoleAssignment ParseRoleAssignment(JsonElement element) => new(
        Role: element.GetProperty("role").GetString()!,
        Profile: element.GetProperty("profile").GetString()!,
        WorkoutCandidates: element.GetProperty("workoutCandidates").EnumerateArray()
            .Select(w => new GeWorkoutCandidate(w.GetProperty("key").GetString()!, w.GetProperty("version").GetInt32()))
            .ToList());
}
