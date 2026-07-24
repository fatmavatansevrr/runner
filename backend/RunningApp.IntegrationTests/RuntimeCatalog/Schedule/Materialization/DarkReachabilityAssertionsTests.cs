using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

public sealed class DarkReachabilityAssertionsTests
{
    [Fact]
    public void InvocationScanner_AllowsOrchestratorCallAndRejectsSyntheticLiveCaller()
    {
        var allowed = DarkReachabilityAssertions.FindInvocationsInSources("PhaseConstraintVerifier",
            new Dictionary<string, string> { ["SafetyVerificationOrchestrator.cs"] = "var x = PhaseConstraintVerifier.Verify(context.Allocation);" });
        var disallowed = DarkReachabilityAssertions.FindInvocationsInSources("PhaseConstraintVerifier",
            new Dictionary<string, string> { ["CatalogPreviewGenerator.cs"] = "var x = PhaseConstraintVerifier.Verify(allocation);" });
        Assert.Single(allowed); Assert.Single(disallowed); Assert.Contains("CatalogPreviewGenerator.cs", disallowed[0]);
    }

    [Fact]
    public void InvocationScanner_IgnoresCommentsXmlDocumentationAndStringProse()
    {
        var source = "/// PhaseConstraintVerifier.Verify(x)\n// PhaseConstraintVerifier.Verify(x)\n/* PhaseConstraintVerifier.Verify(x) */\nvar prose = \"PhaseConstraintVerifier.Verify(x)\";";
        Assert.Empty(DarkReachabilityAssertions.FindInvocationsInSources("PhaseConstraintVerifier", new Dictionary<string, string> { ["Prose.cs"] = source }));
    }

    [Fact]
    public void ActivationScanner_DetectsDiStartupReflectionButNotPlainProse()
    {
        Assert.True(DarkReachabilityAssertions.ContainsActivationInSource("PhaseConstraintVerifier", "services.AddSingleton<PhaseConstraintVerifier>();"));
        Assert.True(DarkReachabilityAssertions.ContainsActivationInSource("PhaseConstraintVerifier", "var t = typeof(PhaseConstraintVerifier);"));
        Assert.True(DarkReachabilityAssertions.ContainsActivationInSource("SafetyVerificationOrchestrator", "Type.GetType(\"X.SafetyVerificationOrchestrator\");"));
        Assert.False(DarkReachabilityAssertions.ContainsActivationInSource("PhaseConstraintVerifier", "// PhaseConstraintVerifier\nvar prose = \"PhaseConstraintVerifier\";"));
        Assert.False(DarkReachabilityAssertions.ContainsActivationInSource("SafetyVerificationOrchestrator", "// Type.GetType(\"X.SafetyVerificationOrchestrator\");"));
    }

    [Fact]
    public void RealOrchestratorIsDarkAndContainsTheAllowedCalls()
    {
        foreach (var verifier in new[] { "PhaseConstraintVerifier", "RaceSpecificCapacityVerifier", "StageReachabilityVerifier", "WorkoutExposureVerifier", "GoalPaceReachabilityVerifier", "ReadinessEligibilityVerifier", "VolumeProgressionVerifier", "LongRunProgressionVerifier", "RaceDateAlignmentVerifier" })
            DarkReachabilityAssertions.AssertVerifierIsReachableOnlyFromDarkOrchestrator(verifier);
        DarkReachabilityAssertions.AssertOrchestratorHasNoLiveActivation();
    }

    // ── Phase 4G.3B.4b.1 -- method-group/delegate detection gap fix ─────────
    // A prior independent validation pass confirmed the original,
    // invocation-only regex (requiring an immediately-following "(") missed
    // a bare method-group reference to a verifier's Verify method. These
    // tests prove the strengthened detector now catches it, using
    // test-local synthetic source only -- no real file is mutated.

    [Fact]
    public void InvocationScanner_DetectsMethodGroupAssignment()
    {
        var result = DarkReachabilityAssertions.FindInvocationsInSources("PhaseConstraintVerifier",
            new Dictionary<string, string> { ["Fake.cs"] = "var f = PhaseConstraintVerifier.Verify;" });
        Assert.Single(result);
    }

    [Fact]
    public void InvocationScanner_DetectsMethodGroupAsArgument()
    {
        var result = DarkReachabilityAssertions.FindInvocationsInSources("PhaseConstraintVerifier",
            new Dictionary<string, string> { ["Fake.cs"] = "var results = items.Select(PhaseConstraintVerifier.Verify).ToList();" });
        Assert.Single(result);
    }

    [Fact]
    public void InvocationScanner_DetectsMethodGroupAssignedToExplicitDelegateType()
    {
        var result = DarkReachabilityAssertions.FindInvocationsInSources("PhaseConstraintVerifier",
            new Dictionary<string, string> { ["Fake.cs"] = "Func<PhaseAllocationResult, PhaseConstraintVerificationResult> f = PhaseConstraintVerifier.Verify;" });
        Assert.Single(result);

        var actionCase = DarkReachabilityAssertions.FindInvocationsInSources("PhaseConstraintVerifier",
            new Dictionary<string, string> { ["Fake.cs"] = "Action<PhaseAllocationResult> a = x => PhaseConstraintVerifier.Verify(x);" });
        Assert.Single(actionCase);
    }

    [Fact]
    public void InvocationScanner_DoesNotFalselyMatchADifferentlyNamedMemberSharingThePrefix()
    {
        var result = DarkReachabilityAssertions.FindInvocationsInSources("PhaseConstraintVerifier",
            new Dictionary<string, string> { ["Fake.cs"] = "var x = PhaseConstraintVerifier.VerifyOrDefault(allocation);" });
        Assert.Empty(result);
    }

    // ── Regression: all previously-passing adversarial cases still pass ────

    [Fact]
    public void InvocationScanner_StillDetectsPlainInvocation_UnusualWhitespace_AndMultiLineSplit()
    {
        Assert.Single(DarkReachabilityAssertions.FindInvocationsInSources("PhaseConstraintVerifier",
            new Dictionary<string, string> { ["Fake.cs"] = "var x = PhaseConstraintVerifier.Verify(allocation);" }));

        Assert.Single(DarkReachabilityAssertions.FindInvocationsInSources("PhaseConstraintVerifier",
            new Dictionary<string, string> { ["Fake.cs"] = "var x = PhaseConstraintVerifier  .  Verify  (allocation);" }));

        Assert.Single(DarkReachabilityAssertions.FindInvocationsInSources("PhaseConstraintVerifier",
            new Dictionary<string, string> { ["Fake.cs"] = "var x = PhaseConstraintVerifier\n    .Verify(allocation);" }));
    }

    [Fact]
    public void InvocationScanner_StillIgnoresCommentsXmlDocumentationAndStringProse_NoRegression()
    {
        var source = "/// PhaseConstraintVerifier.Verify(x)\n// PhaseConstraintVerifier.Verify(x)\n/* PhaseConstraintVerifier.Verify(x) */\nvar prose = \"PhaseConstraintVerifier.Verify(x)\";\nvar prose2 = \"PhaseConstraintVerifier.Verify\";";
        Assert.Empty(DarkReachabilityAssertions.FindInvocationsInSources("PhaseConstraintVerifier", new Dictionary<string, string> { ["Prose.cs"] = source }));
    }

    [Fact]
    public void InvocationScanner_StillHandlesVerbatimStringsWithoutDesynchronizing_NoRegression()
    {
        var source = "var s = @\"PhaseConstraintVerifier.Verify(x) \"\"nested\"\" more text\"; var real = PhaseConstraintVerifier.Verify(allocation);";
        Assert.Single(DarkReachabilityAssertions.FindInvocationsInSources("PhaseConstraintVerifier", new Dictionary<string, string> { ["Fake.cs"] = source }));
    }

    [Fact]
    public void InvocationScanner_StillOverInclusiveForPreprocessorDisabledBlocks_NoRegression()
    {
        var source = "#if NEVER_DEFINED\nvar x = PhaseConstraintVerifier.Verify(allocation);\n#endif";
        Assert.Single(DarkReachabilityAssertions.FindInvocationsInSources("PhaseConstraintVerifier", new Dictionary<string, string> { ["Fake.cs"] = source }));
    }

    [Fact]
    public void ActivationScanner_StillDetectsDiStartupReflectionButNotPlainProse_NoRegression()
    {
        Assert.True(DarkReachabilityAssertions.ContainsActivationInSource("PhaseConstraintVerifier", "services.AddSingleton<PhaseConstraintVerifier>();"));
        Assert.True(DarkReachabilityAssertions.ContainsActivationInSource("PhaseConstraintVerifier", "var t = typeof(PhaseConstraintVerifier);"));
        Assert.True(DarkReachabilityAssertions.ContainsActivationInSource("SafetyVerificationOrchestrator", "Type.GetType(\"X.SafetyVerificationOrchestrator\");"));
        Assert.False(DarkReachabilityAssertions.ContainsActivationInSource("PhaseConstraintVerifier", "// PhaseConstraintVerifier\nvar prose = \"PhaseConstraintVerifier\";"));
        Assert.False(DarkReachabilityAssertions.ContainsActivationInSource("SafetyVerificationOrchestrator", "// Type.GetType(\"X.SafetyVerificationOrchestrator\");"));
    }

    [Fact]
    public void RealOrchestratorIsStillDarkAndContainsExactlyOneAllowedReference_WithStrengthenedDetector()
    {
        // Re-run the real check against all nine verifiers + the
        // orchestrator with the strengthened (method-group-aware) detector
        // -- proves it still reports exactly one real orchestrator
        // reference per verifier and zero disallowed callers against the
        // real, unchanged codebase (no new false positives introduced).
        foreach (var verifier in new[] { "PhaseConstraintVerifier", "RaceSpecificCapacityVerifier", "StageReachabilityVerifier", "WorkoutExposureVerifier", "GoalPaceReachabilityVerifier", "ReadinessEligibilityVerifier", "VolumeProgressionVerifier", "LongRunProgressionVerifier", "RaceDateAlignmentVerifier" })
            DarkReachabilityAssertions.AssertVerifierIsReachableOnlyFromDarkOrchestrator(verifier);
        DarkReachabilityAssertions.AssertOrchestratorHasNoLiveActivation();
    }
}
