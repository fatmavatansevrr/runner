using PlanCatalog.Contracts.Enums;

namespace PlanCatalog.Contracts.Prescriptions;

/// <summary>Fail-closed integrity validation for already-resolved Process A→B values.</summary>
public static class ExecutableWorkoutPrescriptionValidator
{
    public static IReadOnlyList<string> Validate(ExecutableWorkoutPrescription value)
    {
        var errors = new List<string>();
        if (value.ContractSchemaVersion != 1) errors.Add("EXECUTION_CONTRACT_SCHEMA_VERSION_UNSUPPORTED");
        ValidateReference(value.SourceProfile, "EXECUTION_SOURCE_PROFILE_INVALID", errors);
        ValidateReference(value.SourceWorkout, "EXECUTION_SOURCE_WORKOUT_INVALID", errors);
        if (!Enum.IsDefined(value.DoseCategory)) errors.Add("EXECUTION_DOSE_CATEGORY_INVALID");
        if (!Enum.IsDefined(value.DistanceAccountingMode)) errors.Add("EXECUTION_DISTANCE_ACCOUNTING_MODE_INVALID");
        if (value.Components is null || value.Components.Count == 0) errors.Add("EXECUTION_COMPONENTS_EMPTY");
        else foreach (var component in value.Components) ValidateComponent(component, errors);
        return errors;
    }

    private static void ValidateComponent(ExecutablePrescriptionComponent component, List<string> errors)
    {
        if (component.SequenceOrder < 1) errors.Add("EXECUTION_COMPONENT_SEQUENCE_INVALID");
        if (!Enum.IsDefined(component.ComponentType)) errors.Add("EXECUTION_COMPONENT_TYPE_INVALID");
        if (!Enum.IsDefined(component.StructureMode)) errors.Add("EXECUTION_STRUCTURE_MODE_INVALID");
        if (component.Work is null || !Enum.IsDefined(component.Work.Unit) || component.Work.Value <= 0)
            errors.Add("EXECUTION_WORK_QUANTITY_INVALID");
        if (component.Intensity is null || !Enum.IsDefined(component.Intensity.Mode) || string.IsNullOrWhiteSpace(component.Intensity.DescriptorKey))
            errors.Add("EXECUTION_INTENSITY_INVALID");

        if (component.StructureMode == ExecutablePrescriptionStructureMode.Continuous)
        {
            if (component.RepetitionCount is not null || component.Recovery is not null)
                errors.Add("EXECUTION_CONTINUOUS_REPEATED_FIELDS_FORBIDDEN");
            return;
        }

        if (component.StructureMode != ExecutablePrescriptionStructureMode.Repeated) return;
        if (component.RepetitionCount is null or < 2) errors.Add("EXECUTION_REPETITION_COUNT_INVALID");
        if (component.Recovery is null)
        {
            errors.Add("EXECUTION_RECOVERY_REQUIRED");
            return;
        }

        var recovery = component.Recovery;
        if (!Enum.IsDefined(recovery.Unit) || recovery.Value <= 0 || !Enum.IsDefined(recovery.Mode) || !Enum.IsDefined(recovery.Placement))
            errors.Add("EXECUTION_RECOVERY_INVALID");
        if (component.RepetitionCount is >= 2 && Enum.IsDefined(recovery.Placement))
        {
            var expected = recovery.Placement == ExecutableRecoveryPlacement.BetweenRepetitions
                ? component.RepetitionCount.Value - 1
                : component.RepetitionCount.Value;
            if (recovery.RecoveryCount != expected) errors.Add("EXECUTION_RECOVERY_COUNT_INCONSISTENT");
        }
    }

    private static void ValidateReference(References.CatalogArtifactReference? reference, string code, List<string> errors)
    {
        if (reference is null || string.IsNullOrWhiteSpace(reference.DocumentType) || string.IsNullOrWhiteSpace(reference.Key) ||
            reference.Version < 1 || string.IsNullOrWhiteSpace(reference.ContentHash)) errors.Add(code);
    }
}
