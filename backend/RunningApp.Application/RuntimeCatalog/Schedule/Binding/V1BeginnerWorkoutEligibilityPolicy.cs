namespace RunningApp.Application.RuntimeCatalog.Schedule.Binding;

/// <summary>
/// GEN.4D internal-only Beginner 4D eligibility policy. FARTLEK and
/// THRESHOLD_TEMPO remain catalog dependencies because the shared progression
/// references them, but their dosage structures are deferred and therefore
/// cannot be selected for this candidate.
/// </summary>
internal static class V1BeginnerWorkoutEligibilityPolicy
{
    public const string CandidateKey = "TEN_K__4D__BEGINNER";
    public const string DeferredFallbackWorkoutKey = "EASY_STANDARD";

    public static bool IsDeferred(string candidateKey, string workoutKey) =>
        candidateKey == CandidateKey && workoutKey is "FARTLEK" or "THRESHOLD_TEMPO";
}
