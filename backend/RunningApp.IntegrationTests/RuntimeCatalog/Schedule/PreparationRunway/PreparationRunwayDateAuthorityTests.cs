using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunway;

/// <summary>
/// Backend Integration Phase 4G.6A.1 — tests the new, generic
/// <see cref="PreparationRunwayDateAuthority.Derive"/> arithmetic against the
/// canonical exclusive-day values <see cref="CoreHorizonClassifier"/> already
/// produces, across every horizon named by this phase's own required test
/// matrix (14w0d through 53w0d). Confirms the generic contract never knows
/// or branches on any specific week-count policy boundary, and that the
/// prior one-day inclusive/exclusive mismatch cannot return.
/// </summary>
public sealed class PreparationRunwayDateAuthorityTests
{
    private static readonly DateOnly StartDate = new(2026, 1, 5);
    private const int PreferredCoreWeeksForPilot = 12;

    private static CoreHorizonDecision ClassifyDays(int availableDays) =>
        CoreHorizonClassifier.Classify(new CoreHorizonContext(StartDate, StartDate.AddDays(availableDays), 8, PreferredCoreWeeksForPilot, 200));

    // ── Required test matrix: generic authority relationship ────────────────

    [Theory]
    // weeks, days, expectedRunwayFullWeeks, expectedRunwayPartialDays
    [InlineData(14, 0, 2, 0)]
    [InlineData(14, 1, 2, 1)]
    [InlineData(14, 6, 2, 6)]
    [InlineData(15, 0, 3, 0)]
    [InlineData(15, 1, 3, 1)]
    [InlineData(15, 6, 3, 6)]
    [InlineData(20, 0, 8, 0)]
    [InlineData(20, 6, 8, 6)]
    // 21w0d and 52w6d: the generic arithmetic may derive a value, but this
    // phase must not interpret, allocate, or activate it -- these rows only
    // prove the arithmetic itself, never a policy decision.
    [InlineData(21, 0, 9, 0)]
    [InlineData(52, 6, 40, 6)]
    public void Derive_MatchesRequiredMatrix_GenericArithmeticOnly(int weeks, int days, int expectedRunwayFullWeeks, int expectedRunwayPartialDays)
    {
        var availableDays = (weeks * 7) + days;
        var decision = ClassifyDays(availableDays);

        Assert.Equal(weeks, decision.AvailableFullWeeks);
        Assert.Equal(days, decision.LeadingPartialDays);

        var result = PreparationRunwayDateAuthority.Derive(StartDate, decision.AvailableFullWeeks, decision.LeadingPartialDays, PreferredCoreWeeksForPilot);

        Assert.Equal(expectedRunwayFullWeeks, result.RunwayFullWeeks);
        Assert.Equal(expectedRunwayPartialDays, result.RunwayPartialDays);
        Assert.Equal((expectedRunwayFullWeeks * 7) + expectedRunwayPartialDays, result.RunwayDays);
        Assert.Equal(StartDate.AddDays(result.RunwayDays), result.CoreStartDate);

        // Closure check: CoreStartDate + PreferredCoreWeeks*7 (exclusive)
        // must land exactly on the original RaceDate -- the same boundary
        // condition CoreHorizonClassifier itself uses for PreferredCore.
        var raceDate = StartDate.AddDays(availableDays);
        Assert.Equal(raceDate, result.CoreStartDate.AddDays(PreferredCoreWeeksForPilot * 7));
    }

    [Fact]
    public void Derive_53w0d_ArithmeticOnly_NoRejectionOrBranchInThisPass()
    {
        // The generic contract may still compute a difference for 53w0d --
        // it must not know this is "above the outer boundary" and must not
        // throw, reject, or otherwise implement any 52/53-week policy
        // branch. This test proves only that the arithmetic runs and
        // produces a value; it asserts nothing about whether that value is
        // eligible for anything.
        var availableDays = 53 * 7;
        var decision = ClassifyDays(availableDays);

        var result = PreparationRunwayDateAuthority.Derive(StartDate, decision.AvailableFullWeeks, decision.LeadingPartialDays, PreferredCoreWeeksForPilot);

        Assert.Equal(53 - PreferredCoreWeeksForPilot, result.RunwayFullWeeks);
        Assert.Equal(0, result.RunwayPartialDays);
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(9, 3)]
    [InlineData(11, 6)]
    [InlineData(12, 0)]
    [InlineData(13, 2)]
    [InlineData(14, 5)]
    public void Derive_NeverBranchesOnAnyPolicyWeekCount_SameFormulaForEveryHorizon(int weeks, int days)
    {
        // Structural proof of genericity: an 8-14-week standalone-core
        // horizon (which has no meaningful runway -- RunwayFullWeeks comes
        // out negative or zero) is run through the exact same Derive call
        // as every runway-eligible horizon above. There is no special case,
        // no early return, and no different code path for any specific
        // week count -- confirmed by using the identical method for both
        // categories in this same test class.
        var availableDays = (weeks * 7) + days;
        var decision = ClassifyDays(availableDays);
        var result = PreparationRunwayDateAuthority.Derive(StartDate, decision.AvailableFullWeeks, decision.LeadingPartialDays, PreferredCoreWeeksForPilot);

        Assert.Equal(weeks - PreferredCoreWeeksForPilot, result.RunwayFullWeeks);
        Assert.Equal(days, result.RunwayPartialDays);
    }

    // ── Regression: the prior inclusive one-day mismatch cannot return ──────

    [Fact]
    public void Validator_RejectsThePreviousInclusiveOneDayMismatchValue()
    {
        // Reproduces the exact original finding: a real 15w0d exclusive
        // horizon. The OLD (removed) formula, RaceDate.AddDays(-(PreferredCoreWeeks*7-1)),
        // would have accepted CoreStartDate = StartDate + 22 days (3w1d
        // runway). The corrected formula requires exactly StartDate + 21
        // days (3w0d). This test constructs the OLD buggy value directly
        // (bypassing PreparationRunwayDateAuthority entirely) and confirms
        // the corrected validator now rejects it, proving the mismatch
        // cannot silently return.
        var raceDate = StartDate.AddDays(15 * 7);
        var oldBuggyCoreStart = raceDate.AddDays(-((PreferredCoreWeeksForPilot * 7) - 1)); // StartDate + 22
        var correctCoreStart = raceDate.AddDays(-(PreferredCoreWeeksForPilot * 7)); // StartDate + 21

        Assert.Equal(StartDate.AddDays(22), oldBuggyCoreStart);
        Assert.Equal(StartDate.AddDays(21), correctCoreStart);
        Assert.NotEqual(oldBuggyCoreStart, correctCoreStart);

        var contextWithOldBuggyValue = BuildContext(oldBuggyCoreStart, raceDate, runwayDays: 22);
        var contextWithCorrectValue = BuildContext(correctCoreStart, raceDate, runwayDays: 21);

        Assert.False(PreparationRunwayContextValidator.Validate(contextWithOldBuggyValue).IsValid);
        Assert.True(PreparationRunwayContextValidator.Validate(contextWithCorrectValue).IsValid);

        // And the generic derivation itself only ever produces the correct value.
        var decision = ClassifyDays(15 * 7);
        var derived = PreparationRunwayDateAuthority.Derive(StartDate, decision.AvailableFullWeeks, decision.LeadingPartialDays, PreferredCoreWeeksForPilot);
        Assert.Equal(correctCoreStart, derived.CoreStartDate);
        Assert.NotEqual(oldBuggyCoreStart, derived.CoreStartDate);
    }

    private static PreparationRunwayContext BuildContext(DateOnly coreStartDate, DateOnly raceDate, int runwayDays)
    {
        var unknownNeeds = new PreparationNeedProfile(
            PreparationRunwayNeedLevel.NotEvaluated, PreparationRunwayNeedLevel.NotEvaluated,
            PreparationRunwayNeedLevel.NotEvaluated, PreparationRunwayNeedLevel.NotEvaluated,
            PreparationRunwayNeedLevel.NotEvaluated, PreparationRunwayNeedLevel.NotEvaluated);

        return new PreparationRunwayContext(
            RunningApp.Domain.Enums.GoalDistance.TenK,
            new PreparationRunwayExperienceReference(PreparationRunwayExperienceVocabulary.PlanCatalogRunningExperience, "New"),
            unknownNeeds, null, null, null, 4,
            coreStartDate.AddDays(-runwayDays), coreStartDate, raceDate,
            runwayDays, runwayDays / 7, runwayDays % 7, PreferredCoreWeeksForPilot,
            RacePlanCompositionType.PreparationRunwayPlusCore);
    }

    // ── Structural proof: the runway path no longer subtracts StartDate/RaceDate to derive duration ──

    [Fact]
    public void PreparationRunwayDateAuthority_Derive_HasNoRaceDateParameter()
    {
        // Structural/code-level proof (via reflection, not prose): the new
        // generic derivation method's parameter list contains no DateOnly
        // parameter representing a race date at all -- it cannot
        // reintroduce RaceDate-anchored arithmetic even by accident, because
        // RaceDate is never passed to it.
        var method = typeof(PreparationRunwayDateAuthority).GetMethod(nameof(PreparationRunwayDateAuthority.Derive))!;
        var parameters = method.GetParameters();

        Assert.Equal(4, parameters.Length);
        Assert.Equal("startDate", parameters[0].Name);
        Assert.Equal(typeof(DateOnly), parameters[0].ParameterType);
        Assert.DoesNotContain(parameters.Skip(1), p => p.ParameterType == typeof(DateOnly));
    }

    [Fact]
    public void ProductionSource_NoLongerContainsTheRemovedInclusiveCoreStartFormula()
    {
        // Source-level proof that the specific removed formula
        // (RaceDate.AddDays(-(PreferredCoreWeeks*7 - 1)), i.e. any
        // "PreferredCoreWeeks * 7) - 1" or "PreferredCoreWeeks*7-1" pattern)
        // is gone from the two production Preparation Runway files.
        var repo = TestPlanServicesFactory.RepoRoot();
        var root = Path.Combine(repo, "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "PreparationRunway");
        var source = string.Join('\n', Directory.GetFiles(root, "*.cs").Select(File.ReadAllText));

        Assert.DoesNotContain("7) - 1)", source);
        Assert.DoesNotContain("7 - 1)", source);
        Assert.DoesNotContain("7)-1)", source);
    }

    // ── PreferredCoreWeeks candidate authority (Part 2, section 4) ──────────

    [Fact]
    public async Task RealCandidate_TenKFourDayIntermediate_PreferredCoreWeeksIsTwelve()
    {
        // Confirms the existing, already-candidate-scoped authority
        // (PlanCatalogCoreCycle.DefaultWeeks) resolves to 12 for the current
        // pilot candidate -- this is the value a caller must supply to
        // PreparationRunwayDateAuthority.Derive; it is never hard-coded
        // inside the Preparation Runway component itself.
        var bundleLoader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        var candidate = await gate.LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);

        Assert.Equal(12, candidate.CoreCycle.DefaultWeeks);

        // And feeding that real, candidate-resolved value through the
        // generic derivation produces the documented 10K-pilot instantiation
        // (CompositionEntryBoundary = 12 + 3 = 15).
        var decision = ClassifyDays(15 * 7);
        var derived = PreparationRunwayDateAuthority.Derive(StartDate, decision.AvailableFullWeeks, decision.LeadingPartialDays, candidate.CoreCycle.DefaultWeeks);
        Assert.Equal(3, derived.RunwayFullWeeks);
        Assert.Equal(0, derived.RunwayPartialDays);
    }
}
