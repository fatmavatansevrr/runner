using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RunningApp.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace RunningApp.Persistence.Converters;

/// <summary>
/// Running Background V2 — persistence-layer compatibility converter for
/// <see cref="RunningBackground"/>, registered explicitly (after the generic
/// <c>SnakeCaseEnumConverter&lt;T&gt;</c> loop in
/// <c>AppDbContext.OnModelCreating</c>, so this one wins) on every entity
/// property typed <see cref="RunningBackground"/> or <see cref="RunningBackground"/>?.
///
/// Write: always stores the canonical snake_case value ("beginner",
/// "intermediate", "advanced", "experienced").
///
/// Read: accepts the four canonical values AND the three legacy stored
/// values ("new_to_running", "used_to_run", "running_regularly") — retained
/// as a documented, permanent historical-compat safety net even though the
/// known local-dev legacy rows were migrated to canonical values by
/// 20260716185115_RunningBackgroundV2_1_MigrateLegacyTrainingPlanLevels
/// (320 TrainingPlans rows corrected: "running_regularly" → "intermediate").
/// This converter is NOT used at the public HTTP request boundary — see
/// <see cref="RunningApp.Domain.Enums.RunningBackgroundCanonicalJsonConverter"/>
/// for that (canonical-only, rejects legacy aliases). The identical alias
/// mapping used here is shared conceptually with
/// <see cref="RunningApp.Domain.Enums.RunningBackgroundJsonConverter"/>,
/// which serves the same historical-compat role for internal preview
/// snapshot JSON.
/// </summary>
public sealed class RunningBackgroundCompatibilityConverter : ValueConverter<RunningBackground, string>
{
    private static readonly IReadOnlyDictionary<string, RunningBackground> LegacyAliases =
        new Dictionary<string, RunningBackground>(StringComparer.Ordinal)
        {
            ["new_to_running"] = RunningBackground.Beginner,
            ["used_to_run"] = RunningBackground.Beginner,
            ["running_regularly"] = RunningBackground.Intermediate,
        };

    public RunningBackgroundCompatibilityConverter() : base(
        v => ToSnakeCase(v),
        v => FromSnakeCase(v))
    {
    }

    private static string ToSnakeCase(RunningBackground value) =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());

    private static RunningBackground FromSnakeCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return default;
        }

        foreach (var canonical in Enum.GetValues<RunningBackground>())
        {
            if (string.Equals(ToSnakeCase(canonical), value, StringComparison.OrdinalIgnoreCase))
            {
                return canonical;
            }
        }

        if (LegacyAliases.TryGetValue(value, out var legacyMapped))
        {
            return legacyMapped;
        }

        throw new ArgumentException($"Unknown RunningBackground value: {value}");
    }
}
