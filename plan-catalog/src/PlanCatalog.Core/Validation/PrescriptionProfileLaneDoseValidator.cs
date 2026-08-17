using PlanCatalog.Core.Enums;
using PlanCatalog.Contracts;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Models;

namespace PlanCatalog.Core.Validation;

/// <summary>Single publish-time authority for the frozen lane-to-dose mapping.</summary>
public static class PrescriptionProfileLaneDoseValidator
{
    public static ValidationResult Validate(int laneOrdinal, VersionedCatalogReference profileRef, CatalogSourceSnapshot snapshot)
    {
        if (profileRef.DocumentType != DocumentTypes.WorkoutPrescriptionProfile || profileRef.Version < 1 || string.IsNullOrWhiteSpace(profileRef.Key))
            return Error("PROFILE_REFERENCE_INVALID", "Lane profile reference must exactly identify WORKOUT_PRESCRIPTION_PROFILE by non-empty key and positive version.");

        var profile = snapshot.FindPrescriptionProfile(profileRef.Key, profileRef.Version);
        if (profile is null)
            return Error("PROFILE_REFERENCE_NOT_FOUND", $"Exact profile '{profileRef.Key}' v{profileRef.Version} was not found.");

        return Validate(laneOrdinal, profile);
    }

    public static ValidationResult Validate(int laneOrdinal, WorkoutPrescriptionProfile profile)
    {
        var expected = laneOrdinal switch
        {
            0 => PrescriptionDoseCategory.Primary,
            1 => PrescriptionDoseCategory.SecondaryControlled,
            _ => (PrescriptionDoseCategory?)null
        };

        if (expected is null)
            return Error("PROFILE_LANE_ORDINAL_UNSUPPORTED", $"Lane ordinal {laneOrdinal} is unsupported; only 0 and 1 have frozen dose semantics.");
        if (profile.DoseCategory != expected)
            return Error("PROFILE_LANE_DOSE_CATEGORY_MISMATCH", $"Lane ordinal {laneOrdinal} requires dose category {expected} but profile is {profile.DoseCategory}.");
        return ValidationResult.Empty;
    }

    private static ValidationResult Error(string code, string message) =>
        new([new ValidationIssue(code, ValidationSeverity.Error, message, "$.phaseProgressions[].lanes[]")]);
}
