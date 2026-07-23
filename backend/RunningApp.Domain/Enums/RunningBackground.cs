using System.Text.Json.Serialization;

namespace RunningApp.Domain.Enums;

/// <summary>
/// Running Background V2 — the canonical four-level running-experience
/// self-description collected at onboarding. Wire values (JSON, EF/Postgres
/// storage) are the snake_case member names: "beginner", "intermediate",
/// "advanced", "experienced". These four values are the entire active
/// product contract.
///
/// Supersedes the prior three-value model
/// {NewToRunning, UsedToRun, RunningRegularly}. As of Running Background
/// V2.1, "new_to_running" / "used_to_run" / "running_regularly" are NOT
/// current product options and are rejected at the public HTTP request
/// boundary (<see cref="RunningBackgroundCanonicalJsonConverter"/>, the
/// type-level default below). They remain readable ONLY at two narrowly
/// scoped historical boundaries — EF/Postgres persistence
/// (<c>RunningBackgroundCompatibilityConverter</c>) and internal preview
/// snapshot JSON (<see cref="RunningBackgroundJsonConverter"/>, applied via
/// an explicit property-level override on <c>ResolverInputSnapshot.Level</c>
/// only) — see RUNNING_BACKGROUND_V2_FRONTEND_BACKEND_ALIGNMENT.md and
/// RUNNING_BACKGROUND_V2_1_INTERMEDIATE_PILOT_CLOSURE.md for the full
/// compatibility mapping, rationale, and exact retained-row evidence. New
/// code must never emit a legacy alias; only these four canonical values are
/// ever written, anywhere.
/// </summary>
[JsonConverter(typeof(RunningBackgroundCanonicalJsonConverter))]
public enum RunningBackground
{
    Beginner,
    Intermediate,
    Advanced,
    Experienced
}
