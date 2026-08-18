using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Enums;

namespace PlanCatalog.Core.Validation;

public static class WorkoutPrescriptionProfileValidator
{
    /// <summary>
    /// Phase 10K-FREQ.6D.4C.2: <paramref name="capabilityOverlay"/> is the exact-version overlay (if
    /// any) matching <paramref name="workout"/>'s own (Key, Version) — resolved by the caller via
    /// <c>CatalogSourceSnapshot.FindCapabilityOverlay</c>, never auto-selected here. Omitting it (the
    /// default) preserves prior behavior: an absent WorkoutDefinition capability still fails closed.
    /// </summary>
    public static ValidationResult Validate(WorkoutPrescriptionProfile profile, WorkoutDefinition? workout, WorkoutDefinitionCapabilityOverlay? capabilityOverlay = null)
    {
        var issues = new List<ValidationIssue>();

        if (profile.Metadata.DocumentType != DocumentTypes.WorkoutPrescriptionProfile)
            Add(issues, "PROFILE_DOCUMENT_TYPE_INVALID", "metadata.documentType must be WORKOUT_PRESCRIPTION_PROFILE.", "$.metadata.documentType");
        if (profile.Metadata.SchemaVersion != 1)
            Add(issues, "PROFILE_SCHEMA_VERSION_UNSUPPORTED", $"PrescriptionProfile schemaVersion {profile.Metadata.SchemaVersion} is unsupported.", "$.metadata.schemaVersion");
        if (profile.Metadata.Version < 1)
            Add(issues, "PROFILE_VERSION_INVALID", "Profile version must be >= 1.", "$.metadata.version");
        if (string.IsNullOrWhiteSpace(profile.Metadata.Key))
            Add(issues, "PROFILE_KEY_INVALID", "Profile key cannot be empty.", "$.metadata.key");
        if (!Enum.IsDefined(profile.DoseCategory))
            Add(issues, "PROFILE_DOSE_CATEGORY_INVALID", "Dose category is not supported.", "$.doseCategory");
        if (!Enum.IsDefined(profile.DistanceAccountingMode))
            Add(issues, "PROFILE_DISTANCE_ACCOUNTING_MODE_INVALID", "Distance accounting mode is not supported.", "$.distanceAccountingMode");
        if (profile.WorkoutDefinitionRef.DocumentType != DocumentTypes.WorkoutDefinition || profile.WorkoutDefinitionRef.Version < 1 || string.IsNullOrWhiteSpace(profile.WorkoutDefinitionRef.Key))
            Add(issues, "PROFILE_WORKOUT_REFERENCE_INVALID", "workoutDefinitionRef must exactly identify WORKOUT_DEFINITION by non-empty key and positive version.", "$.workoutDefinitionRef");
        if (workout is null)
            Add(issues, "PROFILE_WORKOUT_REFERENCE_NOT_FOUND", "The exact referenced WorkoutDefinition key/version does not exist.", "$.workoutDefinitionRef");
        if (profile.Components.Count == 0)
            Add(issues, "PROFILE_COMPONENTS_EMPTY", "PrescriptionProfile components cannot be empty.", "$.components");

        var duplicateOrders = profile.Components.GroupBy(x => x.SequenceOrder).Where(x => x.Count() > 1).Select(x => x.Key);
        foreach (var order in duplicateOrders)
            Add(issues, "PROFILE_COMPONENT_SEQUENCE_DUPLICATE", $"Component sequenceOrder {order} is duplicated.", "$.components");

        foreach (var component in profile.Components)
            ValidateComponent(component, issues);

        if (workout is not null)
        {
            if (workout.Components is null)
            {
                Add(issues, "PROFILE_WORKOUT_COMPONENT_SKELETON_MISSING", "Referenced WorkoutDefinition has no component skeleton and cannot own a PrescriptionProfile without a new WorkoutDefinition version.", "$.workoutDefinitionRef");
            }
            else
            {
                var expected = workout.Components.OrderBy(x => x.SequenceOrder).Select(x => (x.SequenceOrder, x.ComponentType));
                var actual = profile.Components.OrderBy(x => x.SequenceOrder).Select(x => (x.SequenceOrder, x.ComponentType));
                if (!expected.SequenceEqual(actual))
                    Add(issues, "PROFILE_COMPONENT_SKELETON_MISMATCH", "Profile components must match the referenced WorkoutDefinition component order and types exactly.", "$.components");
            }

            // Phase 10K-FREQ.6D.4C.2 (M3): distance-accounting capability is resolved through the one
            // canonical seam (WorkoutCapabilityResolver), never compared to raw WorkoutDefinition state
            // inline here. An overlay only ever fills a genuine absence; both sources present at once is
            // a fail-closed conflict, never resolved by precedence.
            var capability = WorkoutCapabilityResolver.ResolveDistanceAccountingCapability(workout, capabilityOverlay);
            switch (capability.Source)
            {
                case EffectiveDistanceAccountingCapabilitySource.Conflict:
                    Add(issues, "PROFILE_CAPABILITY_OVERLAY_CONFLICTS_WITH_EXPLICIT_METADATA",
                        "The referenced WorkoutDefinition explicitly declares AllowedDistanceAccountingModes; a capability overlay for the same exact version must not also supply it.", "$.distanceAccountingMode");
                    break;
                case EffectiveDistanceAccountingCapabilitySource.Unresolved:
                    Add(issues, "PROFILE_DISTANCE_ACCOUNTING_MODE_NOT_ALLOWED", "Profile distanceAccountingMode is not allowed by the referenced WorkoutDefinition.", "$.distanceAccountingMode");
                    break;
                default:
                    if (!capability.AllowedModes!.Contains(profile.DistanceAccountingMode))
                        Add(issues, "PROFILE_DISTANCE_ACCOUNTING_MODE_NOT_ALLOWED", "Profile distanceAccountingMode is not allowed by the referenced WorkoutDefinition.", "$.distanceAccountingMode");
                    break;
            }

            // Phase 10K-FREQ.6D.4C.2 (M4): the prior cross-check here compared this component's typed
            // PrescriptionIntensityMode (PaceBased/EffortBased/HeartRateBased - a per-component execution
            // targeting mechanism) against WorkoutDefinition.AllowedPrescriptionModes (a session-level
            // legacy dosage descriptor whose only confirmed real value for these workouts is MIXED, never
            // a wildcard for typed modes - FREQ.6D.4C.1 SEMANTIC_AXIS_MISMATCH finding). That comparison
            // is deliberately removed, not widened: internal profile consistency (PROFILE_INTENSITY_MODE_INVALID,
            // PROFILE_INTENSITY_MODE_DESCRIPTOR_MISMATCH, both still enforced in ValidateComponent above)
            // is the correct, sufficient replacement invariant. AllowedPrescriptionModes remains fully
            // intact and unmodified for its real, original consumer (legacy backend runtime prescription
            // mode selection) - this validator simply no longer asks it a question it cannot correctly answer.
        }

        return new ValidationResult(issues);
    }

    private static void ValidateComponent(PrescriptionProfileComponent component, List<ValidationIssue> issues)
    {
        if (!Enum.IsDefined(component.StructureMode))
            Add(issues, "PROFILE_STRUCTURE_MODE_INVALID", "Structure mode is not supported.", "$.components[].structureMode");
        if (!Enum.IsDefined(component.ComponentType))
            Add(issues, "PROFILE_COMPONENT_TYPE_INVALID", "Component type is not supported.", "$.components[].componentType");
        if (component.SequenceOrder < 1)
            Add(issues, "PROFILE_COMPONENT_SEQUENCE_INVALID", "sequenceOrder must be >= 1.", "$.components[].sequenceOrder");
        if (component.WorkQuantity is null)
            Add(issues, "PROFILE_WORK_QUANTITY_REQUIRED", "Every executable profile component requires workQuantity.", "$.components[].workQuantity");
        else
            ValidateWorkQuantity(component.StructureMode, component.WorkQuantity, issues);

        if (component.StructureMode == PrescriptionStructureMode.Continuous && component.RecoveryQuantity is not null)
            Add(issues, "PROFILE_CONTINUOUS_RECOVERY_FORBIDDEN", "CONTINUOUS components cannot declare recoveryQuantity.", "$.components[].recoveryQuantity");
        if (component.StructureMode == PrescriptionStructureMode.Continuous && component.RecoveryPlacement is not null)
            Add(issues, "PROFILE_CONTINUOUS_RECOVERY_PLACEMENT_FORBIDDEN", "CONTINUOUS components cannot declare recoveryPlacement.", "$.components[].recoveryPlacement");
        if (component.StructureMode == PrescriptionStructureMode.Repeated && component.RecoveryQuantity is null)
            Add(issues, "PROFILE_REPEATED_RECOVERY_REQUIRED", "REPEATED components require recoveryQuantity.", "$.components[].recoveryQuantity");
        if (component.StructureMode == PrescriptionStructureMode.Repeated && component.RecoveryPlacement is null)
            Add(issues, "PROFILE_REPEATED_RECOVERY_PLACEMENT_REQUIRED", "REPEATED components require explicit recoveryPlacement.", "$.components[].recoveryPlacement");
        if (component.RecoveryPlacement is not null && !Enum.IsDefined(component.RecoveryPlacement.Value))
            Add(issues, "PROFILE_RECOVERY_PLACEMENT_INVALID", "Recovery placement is not supported.", "$.components[].recoveryPlacement");
        if (component.RecoveryPlacement is not null && component.RecoveryQuantity is null)
            Add(issues, "PROFILE_RECOVERY_PLACEMENT_WITHOUT_RECOVERY", "recoveryPlacement cannot be declared without recoveryQuantity.", "$.components[].recoveryPlacement");
        if (component.RecoveryQuantity is not null)
            ValidateRecovery(component.RecoveryQuantity, issues);

        if (!Enum.IsDefined(component.IntensityTarget.Mode))
            Add(issues, "PROFILE_INTENSITY_MODE_INVALID", "Intensity mode is not supported.", "$.components[].intensityTarget.mode");

        var descriptors = new[] { component.IntensityTarget.PaceDescriptorKey, component.IntensityTarget.EffortDescriptorKey, component.IntensityTarget.HeartRateZoneKey };
        if (descriptors.Count(x => !string.IsNullOrWhiteSpace(x)) != 1)
            Add(issues, "PROFILE_INTENSITY_DESCRIPTOR_INVALID", "Exactly one intensity descriptor is required.", "$.components[].intensityTarget");
        var correct = component.IntensityTarget.Mode switch
        {
            PrescriptionIntensityMode.PaceBased => !string.IsNullOrWhiteSpace(component.IntensityTarget.PaceDescriptorKey),
            PrescriptionIntensityMode.EffortBased => !string.IsNullOrWhiteSpace(component.IntensityTarget.EffortDescriptorKey),
            PrescriptionIntensityMode.HeartRateBased => !string.IsNullOrWhiteSpace(component.IntensityTarget.HeartRateZoneKey),
            _ => false
        };
        if (!correct)
            Add(issues, "PROFILE_INTENSITY_MODE_DESCRIPTOR_MISMATCH", "Intensity descriptor must match intensity mode.", "$.components[].intensityTarget");
    }

    private static void ValidateWorkQuantity(PrescriptionStructureMode mode, PrescriptionWorkQuantity quantity, List<ValidationIssue> issues)
    {
        var units = (quantity.DurationSeconds.HasValue ? 1 : 0) + (quantity.DistanceMeters.HasValue ? 1 : 0);
        if (units != 1)
            Add(issues, "PROFILE_WORK_UNIT_AMBIGUOUS", "Exactly one of durationSeconds or distanceMeters must be set.", "$.components[].workQuantity");
        if (quantity.DurationSeconds is <= 0 || quantity.DistanceMeters is <= 0)
            Add(issues, "PROFILE_WORK_QUANTITY_NON_POSITIVE", "Work duration/distance must be positive.", "$.components[].workQuantity");
        if (mode == PrescriptionStructureMode.Continuous && quantity.RepetitionCount is not null)
            Add(issues, "PROFILE_CONTINUOUS_REPETITIONS_FORBIDDEN", "CONTINUOUS work cannot declare repetitionCount.", "$.components[].workQuantity.repetitionCount");
        if (mode == PrescriptionStructureMode.Repeated && quantity.RepetitionCount is null or < 2)
            Add(issues, "PROFILE_REPEATED_COUNT_INVALID", "REPEATED work requires repetitionCount >= 2.", "$.components[].workQuantity.repetitionCount");
    }

    private static void ValidateRecovery(PrescriptionRecoveryQuantity recovery, List<ValidationIssue> issues)
    {
        if (!Enum.IsDefined(recovery.Mode))
            Add(issues, "PROFILE_RECOVERY_MODE_INVALID", "Recovery mode is not supported.", "$.components[].recoveryQuantity.mode");
        var units = (recovery.DurationSeconds.HasValue ? 1 : 0) + (recovery.DistanceMeters.HasValue ? 1 : 0);
        if (units != 1)
            Add(issues, "PROFILE_RECOVERY_UNIT_AMBIGUOUS", "Exactly one recovery duration or distance must be set.", "$.components[].recoveryQuantity");
        if (recovery.DurationSeconds is <= 0 || recovery.DistanceMeters is <= 0)
            Add(issues, "PROFILE_RECOVERY_QUANTITY_NON_POSITIVE", "Recovery duration/distance must be positive.", "$.components[].recoveryQuantity");
    }

    private static void Add(List<ValidationIssue> issues, string code, string message, string path) =>
        issues.Add(new ValidationIssue(code, ValidationSeverity.Error, message, path));
}
