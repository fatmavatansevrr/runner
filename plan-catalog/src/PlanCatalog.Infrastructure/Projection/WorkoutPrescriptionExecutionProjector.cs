using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.Prescriptions;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Infrastructure.Publishing;

namespace PlanCatalog.Infrastructure.Projection;

/// <summary>Selection-free, deterministic compiler from exact Core sources to the execution boundary.</summary>
public sealed class WorkoutPrescriptionExecutionProjector
{
    public ExecutableWorkoutPrescription Project(WorkoutPrescriptionProfile profile, WorkoutDefinition workout)
    {
        if (profile.WorkoutDefinitionRef.Key != workout.Metadata.Key || profile.WorkoutDefinitionRef.Version != workout.Metadata.Version)
            throw Fail("EXECUTION_PROJECTION_WORKOUT_REFERENCE_MISMATCH");
        if (profile.Metadata.ContentHash is null || workout.Metadata.ContentHash is null)
            throw Fail("EXECUTION_PROJECTION_SOURCE_HASH_MISSING");
        if (profile.Components.Count == 0 || profile.Components.Any(x => x.SequenceOrder < 1) ||
            profile.Components.GroupBy(x => x.SequenceOrder).Any(x => x.Count() != 1))
            throw Fail("EXECUTION_PROJECTION_COMPONENT_ORDER_INVALID");

        var result = new ExecutableWorkoutPrescription
        {
            ContractSchemaVersion = 1,
            SourceProfile = CatalogArtifactReferences.ToRef(profile.Metadata),
            SourceWorkout = CatalogArtifactReferences.ToRef(workout.Metadata),
            DoseCategory = MapDose(profile.DoseCategory),
            DistanceAccountingMode = profile.DistanceAccountingMode,
            Components = profile.Components.OrderBy(x => x.SequenceOrder).Select(ProjectComponent).ToList()
        };

        var errors = ExecutableWorkoutPrescriptionValidator.Validate(result);
        if (errors.Count != 0) throw Fail($"EXECUTION_PROJECTION_BOUNDARY_INVALID: {string.Join(",", errors)}");
        return result;
    }

    private static ExecutablePrescriptionComponent ProjectComponent(PrescriptionProfileComponent source)
    {
        if (source.WorkQuantity is null) throw Fail("EXECUTION_PROJECTION_WORK_MISSING");
        var structure = MapStructure(source.StructureMode);
        var work = MapQuantity(source.WorkQuantity.DurationSeconds, source.WorkQuantity.DistanceMeters, "WORK");
        var intensity = MapIntensity(source.IntensityTarget);

        if (structure == ExecutablePrescriptionStructureMode.Continuous)
        {
            if (source.WorkQuantity.RepetitionCount is not null || source.RecoveryQuantity is not null || source.RecoveryPlacement is not null)
                throw Fail("EXECUTION_PROJECTION_CONTINUOUS_STATE_INVALID");
            return new ExecutablePrescriptionComponent
            {
                SequenceOrder = source.SequenceOrder, ComponentType = source.ComponentType, StructureMode = structure,
                Work = work, Intensity = intensity
            };
        }

        if (source.WorkQuantity.RepetitionCount is not >= 2 || source.RecoveryQuantity is null || source.RecoveryPlacement is null)
            throw Fail("EXECUTION_PROJECTION_REPEATED_STATE_INVALID");
        var placement = MapPlacement(source.RecoveryPlacement.Value);
        var recoveryQuantity = MapQuantity(source.RecoveryQuantity.DurationSeconds, source.RecoveryQuantity.DistanceMeters, "RECOVERY");
        return new ExecutablePrescriptionComponent
        {
            SequenceOrder = source.SequenceOrder,
            ComponentType = source.ComponentType,
            StructureMode = structure,
            Work = work,
            RepetitionCount = source.WorkQuantity.RepetitionCount,
            Recovery = new ExecutableRecovery
            {
                Unit = recoveryQuantity.Unit,
                Value = recoveryQuantity.Value,
                Mode = MapRecoveryMode(source.RecoveryQuantity.Mode),
                Placement = placement,
                RecoveryCount = PrescriptionRecoveryCardinality.Derive(source.WorkQuantity.RepetitionCount.Value, source.RecoveryPlacement.Value)
            },
            Intensity = intensity
        };
    }

    private static ExecutableWorkQuantity MapQuantity(int? seconds, int? meters, string label) => (seconds, meters) switch
    {
        (> 0, null) => new ExecutableWorkQuantity { Unit = ExecutableQuantityUnit.Seconds, Value = seconds.Value },
        (null, > 0) => new ExecutableWorkQuantity { Unit = ExecutableQuantityUnit.Meters, Value = meters.Value },
        _ => throw Fail($"EXECUTION_PROJECTION_{label}_QUANTITY_INVALID")
    };

    private static ExecutableIntensityTarget MapIntensity(PrescriptionIntensityTarget source)
    {
        var (mode, descriptor) = source.Mode switch
        {
            PrescriptionIntensityMode.PaceBased => (ExecutableIntensityMode.PaceBased, source.PaceDescriptorKey),
            PrescriptionIntensityMode.EffortBased => (ExecutableIntensityMode.EffortBased, source.EffortDescriptorKey),
            PrescriptionIntensityMode.HeartRateBased => (ExecutableIntensityMode.HeartRateBased, source.HeartRateZoneKey),
            _ => throw Fail("EXECUTION_PROJECTION_INTENSITY_MODE_UNSUPPORTED")
        };
        if (string.IsNullOrWhiteSpace(descriptor)) throw Fail("EXECUTION_PROJECTION_INTENSITY_DESCRIPTOR_INVALID");
        return new ExecutableIntensityTarget { Mode = mode, DescriptorKey = descriptor };
    }

    private static ExecutablePrescriptionStructureMode MapStructure(PrescriptionStructureMode value) => value switch
    {
        PrescriptionStructureMode.Continuous => ExecutablePrescriptionStructureMode.Continuous,
        PrescriptionStructureMode.Repeated => ExecutablePrescriptionStructureMode.Repeated,
        _ => throw Fail("EXECUTION_PROJECTION_STRUCTURE_MODE_UNSUPPORTED")
    };

    private static ExecutablePrescriptionDoseCategory MapDose(PrescriptionDoseCategory value) => value switch
    {
        PrescriptionDoseCategory.Primary => ExecutablePrescriptionDoseCategory.Primary,
        PrescriptionDoseCategory.SecondaryControlled => ExecutablePrescriptionDoseCategory.SecondaryControlled,
        _ => throw Fail("EXECUTION_PROJECTION_DOSE_CATEGORY_UNSUPPORTED")
    };

    private static ExecutableRecoveryMode MapRecoveryMode(PrescriptionRecoveryMode value) => value switch
    {
        PrescriptionRecoveryMode.Jog => ExecutableRecoveryMode.Jog,
        PrescriptionRecoveryMode.Walk => ExecutableRecoveryMode.Walk,
        PrescriptionRecoveryMode.Stationary => ExecutableRecoveryMode.Stationary,
        _ => throw Fail("EXECUTION_PROJECTION_RECOVERY_MODE_UNSUPPORTED")
    };

    private static ExecutableRecoveryPlacement MapPlacement(PrescriptionRecoveryPlacement value) => value switch
    {
        PrescriptionRecoveryPlacement.BetweenRepetitions => ExecutableRecoveryPlacement.BetweenRepetitions,
        PrescriptionRecoveryPlacement.AfterEachRepetition => ExecutableRecoveryPlacement.AfterEachRepetition,
        _ => throw Fail("EXECUTION_PROJECTION_RECOVERY_PLACEMENT_UNSUPPORTED")
    };

    private static InvalidOperationException Fail(string code) => new(code);
}
