using PlanCatalog.Core.Enums;

namespace PlanCatalog.Core.Models;

/// <summary>Canonical authoring-to-execution recovery-count derivation.</summary>
public static class PrescriptionRecoveryCardinality
{
    public static int Derive(int repetitionCount, PrescriptionRecoveryPlacement placement)
    {
        if (repetitionCount < 2)
            throw new ArgumentOutOfRangeException(nameof(repetitionCount), "Repetition count must be at least 2.");
        if (!Enum.IsDefined(placement))
            throw new ArgumentOutOfRangeException(nameof(placement), "Recovery placement is unsupported.");

        return placement switch
        {
            PrescriptionRecoveryPlacement.BetweenRepetitions => repetitionCount - 1,
            PrescriptionRecoveryPlacement.AfterEachRepetition => repetitionCount,
            _ => throw new ArgumentOutOfRangeException(nameof(placement), "Recovery placement is unsupported.")
        };
    }
}
