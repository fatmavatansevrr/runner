using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 4L.4C -- Part 1/2 block-taxonomy classification proof. Pure,
/// DB-free coverage of every reason code in the existing, unmodified
/// LongHorizonCheckpointReasonCode (9 values) and LongHorizonJitReasonCode
/// (10 values) taxonomies, proving each maps to exactly one recovery class
/// and that corruption/contradiction reasons are never classified as
/// temporarily retryable.
/// </summary>
public sealed class LongHorizonBlockRecoveryClassificationTests
{
    [Theory]
    [InlineData("CheckpointWindowNotComplete", "RecoverableWithElapsedCalendarTime")]
    [InlineData("CheckpointEvidenceStale", "RequiresRegeneratePreview")] // never a real block reason (MaintenanceOnly outcome only); defensively classified, not retryable.
    [InlineData("ValidatedLoadUnavailable", "RequiresRegeneratePreview")]
    [InlineData("ValidatedLongRunEvidenceUnavailable", "RequiresRegeneratePreview")]
    [InlineData("AdherenceConfidenceInsufficientForGrowth", "RequiresRegeneratePreview")] // never a real block reason (MaintenanceOnly outcome only); defensively classified.
    [InlineData("MaintenanceAnchorUnavailable", "RequiresRegeneratePreview")] // never the persisted block reason (subsumed under EvidenceConflictUnresolved); defensively classified.
    [InlineData("NumericWindowInfeasible", "RequiresRegeneratePreview")]
    [InlineData("SafetyReassessmentRequired", "OperationalSupportRequired")]
    [InlineData("EvidenceConflictUnresolved", "OperationalSupportRequired")]
    [InlineData("RunwayJitContextUnavailable", "RequiresRegeneratePreview")]
    [InlineData("CoreJitContextUnavailable", "RequiresRegeneratePreview")]
    [InlineData("JitValidatedLoadUnavailable", "RequiresRegeneratePreview")]
    [InlineData("JitValidatedLongRunUnavailable", "RequiresRegeneratePreview")]
    [InlineData("JitPaceSourceUnresolved", "RequiresRegeneratePreview")]
    [InlineData("JitGoalFeasibilityUnresolved", "RequiresRegeneratePreview")]
    [InlineData("JitAvailabilityInfeasible", "RequiresRegeneratePreview")]
    [InlineData("JitEvidenceConflictUnresolved", "OperationalSupportRequired")]
    [InlineData("JitActivationBoundaryMissed", "OperationalSupportRequired")]
    [InlineData("JitSegmentTransitionInfeasible", "RequiresRegeneratePreview")]
    [InlineData("SomeFutureUnknownReasonCode", "RequiresRegeneratePreview")] // fail-closed default, not silently retryable.
    public void EveryReasonCode_ClassifiesDeterministically(string reasonCode, string expected)
    {
        Assert.Equal(expected, LongHorizonBlockRecoveryClassification.Classify(reasonCode).ToString());
    }

    [Theory]
    [InlineData("RecoverableWithElapsedCalendarTime", true)]
    [InlineData("RequiresRegeneratePreview", false)]
    [InlineData("OperationalSupportRequired", false)]
    public void OnlyElapsedCalendarTimeClass_IsRetryEligibleWithoutNewEvidence(string recoveryClassName, bool expected)
    {
        var recoveryClass = Enum.Parse<LongHorizonBlockRecoveryClass>(recoveryClassName);
        Assert.Equal(expected, LongHorizonBlockRecoveryClassification.IsRetryEligibleWithoutNewEvidence(recoveryClass));
    }

    [Fact]
    public void CorruptionAndContradictionReasons_AreNeverRetryEligible()
    {
        foreach (var code in new[] { "EvidenceConflictUnresolved", "JitEvidenceConflictUnresolved", "JitActivationBoundaryMissed" })
        {
            var recoveryClass = LongHorizonBlockRecoveryClassification.Classify(code);
            Assert.Equal(LongHorizonBlockRecoveryClass.OperationalSupportRequired, recoveryClass);
            Assert.False(LongHorizonBlockRecoveryClassification.IsRetryEligibleWithoutNewEvidence(recoveryClass));
        }
    }

    [Fact]
    public void PermanentInfeasibilityReasons_AreNeverClassifiedAsTemporary()
    {
        foreach (var code in new[] { "ValidatedLongRunEvidenceUnavailable", "ValidatedLoadUnavailable", "NumericWindowInfeasible", "JitAvailabilityInfeasible" })
        {
            var recoveryClass = LongHorizonBlockRecoveryClassification.Classify(code);
            Assert.Equal(LongHorizonBlockRecoveryClass.RequiresRegeneratePreview, recoveryClass);
            Assert.False(LongHorizonBlockRecoveryClassification.IsRetryEligibleWithoutNewEvidence(recoveryClass));
        }
    }
}
