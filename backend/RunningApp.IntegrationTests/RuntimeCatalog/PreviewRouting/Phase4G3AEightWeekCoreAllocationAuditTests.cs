using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Phase 4G.3A — audit-protection tests only. Proves two of the audit's
/// central findings so they cannot silently drift: (1) the real catalog
/// artifact's actual phase min/preferred/max bounds (the authoritative
/// source for the reconciliation table in
/// PHASE4G_3A_EIGHT_WEEK_CORE_ALLOCATION_AUDIT.md §3-5), and (2)
/// <see cref="CatalogPhaseAllocationResolver"/>'s current, unchanged
/// inability to receive a target cycle length or readiness signal — the
/// exact gap that audit document classifies as the Phase 4G.3B
/// implementation blocker. No allocator behavior is implemented or changed
/// by this file. Current fail-closed 8-week behavior is already fully
/// protected by <see cref="RunningApp.IntegrationTests.Sw13ExactTwelveWeekOnlyEndToEndTests"/>
/// and <see cref="RunningApp.IntegrationTests.PlanGeneration.LongHorizonFailClosedTests"/> —
/// not duplicated here.
/// </summary>
public sealed class Phase4G3AEightWeekCoreAllocationAuditTests
{
    // ── Audit Question A: actual catalog phase bounds (raw artifact) ────────

    [Fact]
    public void RealCatalogArtifact_TenKMasterV6_PhaseBounds_MatchAuditReconciliationTable()
    {
        var path = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog", "templates", "ten-k-master.v6.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var phases = doc.RootElement.GetProperty("phases");

        var byKey = phases.EnumerateArray().ToDictionary(p => p.GetProperty("phaseKey").GetString()!, p => p);

        // Audit table (PHASE4G_3A §3): Foundation min=2/preferred=3/max=4,
        // Build min=3/preferred=4/max=5, RaceSpecific min=2/preferred=4/max=4,
        // Taper min=1/preferred=1/max=1 (isCompressionProtected=true).
        AssertPhase(byKey["FOUNDATION"], min: 2, preferred: 3, max: 4, compressionPriority: 1, protectedPhase: false);
        AssertPhase(byKey["BUILD"], min: 3, preferred: 4, max: 5, compressionPriority: 2, protectedPhase: false);
        AssertPhase(byKey["RACE_SPECIFIC"], min: 2, preferred: 4, max: 4, compressionPriority: 3, protectedPhase: false);
        AssertPhase(byKey["TAPER"], min: 1, preferred: 1, max: 1, compressionPriority: 4, protectedPhase: true);

        static void AssertPhase(JsonElement phase, int min, int preferred, int max, int compressionPriority, bool protectedPhase)
        {
            Assert.Equal(min, phase.GetProperty("minimumWeeks").GetInt32());
            Assert.Equal(preferred, phase.GetProperty("preferredWeeks").GetInt32());
            Assert.Equal(max, phase.GetProperty("maximumWeeks").GetInt32());
            Assert.Equal(compressionPriority, phase.GetProperty("compressionPriority").GetInt32());
            Assert.Equal(protectedPhase, phase.GetProperty("isCompressionProtected").GetBoolean());
        }
    }

    // ── Audit Question B: sum of actual catalog minimums == 8, exactly ──────

    [Fact]
    public void RealCatalogArtifact_SumOfPhaseMinimums_EqualsEightExactly_NoSpareWeeks()
    {
        var path = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog", "templates", "ten-k-master.v6.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var phases = doc.RootElement.GetProperty("phases");

        var sumOfMinimums = phases.EnumerateArray().Sum(p => p.GetProperty("minimumWeeks").GetInt32());

        // The audit's central structural finding: 2+3+2+1 = 8, exactly the
        // target horizon, with zero spare weeks to distribute. The
        // "Foundation may compress to 1 only if ready" question (task's
        // Canonical Input C-03) is therefore moot for exactly 8 weeks — no
        // allocation reaching 8 weeks ever needs Foundation below its own
        // catalog-declared minimum of 2. See audit §5/§8.
        Assert.Equal(8, sumOfMinimums);
    }

    // ── Audit Question C: CatalogPhaseAllocationResolver cannot receive ─────
    // ── cycle length or readiness — the Phase 4G.3B implementation blocker ──

    [Fact]
    public void CatalogPhaseAllocationResolver_Resolve_CandidateOnlyOverload_StillTakesOnlyCandidate_NoCycleLengthOrReadinessParameter()
    {
        // Compile-time proof, not a behavior test: the ORIGINAL, candidate-only
        // ICatalogPhaseAllocationResolver.Resolve(PlanCatalogCandidateSummary)
        // overload still has exactly one parameter and still ignores horizon/
        // readiness. Phase 4G.3B.2 deliberately added a SECOND overload,
        // Resolve(PlanCatalogCandidateSummary, int targetWeekCount) -- exactly
        // the "future phase adds a second parameter" this test's original
        // comment predicted -- but as an ADDITIONAL overload, not a change to
        // this one. Disambiguated by parameter types so this keeps asserting
        // the single-candidate overload specifically, not whichever overload
        // reflection happens to find first.
        var method = typeof(ICatalogPhaseAllocationResolver).GetMethod("Resolve", new[] { typeof(PlanCatalogCandidateSummary) })!;
        var parameters = method.GetParameters();

        Assert.Single(parameters);
        Assert.Equal(typeof(PlanCatalogCandidateSummary), parameters[0].ParameterType);
    }

    [Fact]
    public async Task CatalogPhaseAllocationResolver_Resolve_IsPureFunctionOfCandidate_OutputIdenticalRegardlessOfWouldBeRequestContext()
    {
        // CatalogPhaseAllocationResolver.Resolve(candidate) has no way to
        // vary its output by request horizon or CORE_ENTRY_READINESS_IN --
        // this test proves it by calling it twice against the same candidate
        // (the only input it accepts) and asserting byte-identical output.
        // A future horizon-aware resolver would legitimately break this
        // test by design -- that is the expected, deliberate signal that
        // Phase 4G.3B has landed.
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        var candidate = await loader.LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);

        var resolver = new CatalogPhaseAllocationResolver();
        var first = resolver.Resolve(candidate);
        var second = resolver.Resolve(candidate);

        Assert.Equal(first.TotalWeeks, second.TotalWeeks);
        Assert.Equal(
            first.Entries.Select(e => (e.PhaseKey, e.PhaseWeekCount)),
            second.Entries.Select(e => (e.PhaseKey, e.PhaseWeekCount)));

        // Today's fixed output: the candidate's preferredWeeks per phase,
        // summing to 12 -- never 8, regardless of any 8-week request.
        Assert.Equal(12, first.TotalWeeks);
    }

    // ── Audit Question C.4: PlanCatalogCandidateSummary does not carry the ──
    // ── phase min/max/compressionPriority fields a horizon-aware allocator ──
    // ── would need -- confirmed structural blocker, not just missing logic ──

    [Fact]
    public void PlanCatalogPhaseAllocation_CarriesCompleteCatalogConstraintContract()
    {
        // Phase 4G.3B.1 closes the typed-transport blocker recorded by 4G.3A.
        // Allocator consumption of these fields remains deliberately deferred.
        var properties = typeof(PlanCatalogPhaseAllocation).GetProperties().Select(p => p.Name).ToList();

        Assert.Contains("PhaseKey", properties);
        Assert.Contains("MinimumWeeks", properties);
        Assert.Contains("PreferredWeeks", properties);
        Assert.Contains("MaximumWeeks", properties);
        Assert.Contains("CompressionPriority", properties);
        Assert.Contains("ExtensionPriority", properties);
        Assert.Contains("IsCompressionProtected", properties);
    }
}
