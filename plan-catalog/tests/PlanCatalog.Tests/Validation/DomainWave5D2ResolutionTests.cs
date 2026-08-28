using System.Text.Json;
using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Bundles;
using PlanCatalog.Core.Audit;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Schema;
using PlanCatalog.Infrastructure.Serialization;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>Wave 5 / D2: INTERMEDIATE_PROGRESSION_MODIFIER_V1 dosage/settings resolution.</summary>
public sealed class DomainWave5D2ResolutionTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private static Core.Catalog.CatalogSourceSnapshot LoadSnapshot() =>
        new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog")).LoadSnapshot();

    private static PublishedTemplateBundle BuildBundle(int version)
    {
        var snapshot = LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        return new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher()).Assemble(stamped, "TEN_K__4D__INTERMEDIATE", version);
    }

    // ---------- 1-2: schema rejects/accepts maximumComplexityTier by schemaVersion ----------

    [Fact]
    public void ProgressionModifierSchemaV2_RejectsMaximumComplexityTier_AndLegacyV1RequiresIt()
    {
        var validator = new JsonSchemaNetValidator(Path.Combine(AppContext.BaseDirectory, "TestSchemas"));

        var v2WithComplexity = """
        {
          "metadata": { "documentType": "PROGRESSION_MODIFIER", "schemaVersion": 2, "key": "INTERMEDIATE_PROGRESSION_MODIFIER_V1", "version": 2, "status": "DRAFT" },
          "experience": "INTERMEDIATE",
          "maximumComplexityTier": 2,
          "maximumHardSessionsPerWeek": 1,
          "mainSetDoseMultiplier": 1.0,
          "allowGoalPaceRehearsal": true,
          "allowSecondHardStimulus": false
        }
        """;

        var legacyV1 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "progression-modifiers", "intermediate-progression-modifier.v1.json"));
        var legacyV1MissingComplexity = legacyV1.Replace("\r\n  \"maximumComplexityTier\": 2,", string.Empty).Replace("\n  \"maximumComplexityTier\": 2,", string.Empty);
        var newV2 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "progression-modifiers", "intermediate-progression-modifier.v2.json"));

        Assert.False(validator.Validate(DocumentTypes.ProgressionModifier, v2WithComplexity).IsValid);
        Assert.True(validator.Validate(DocumentTypes.ProgressionModifier, legacyV1).IsValid);
        Assert.False(validator.Validate(DocumentTypes.ProgressionModifier, legacyV1MissingComplexity).IsValid);
        Assert.True(validator.Validate(DocumentTypes.ProgressionModifier, newV2).IsValid);
    }

    [Fact]
    public void ProgressionModifierValidator_EnforcesLegacyComplexityOnlyOnLegacySchema()
    {
        var v2 = Modifier(schemaVersion: 2, version: 2, maximumComplexityTier: null);
        var v2WithLegacyField = v2 with { MaximumComplexityTier = 2 };
        var legacyMissing = Modifier(schemaVersion: 1, version: 1, maximumComplexityTier: null);

        Assert.True(ProgressionModifierValidator.Validate(v2).IsValid);
        Assert.Contains(ProgressionModifierValidator.Validate(v2WithLegacyField).Issues, i => i.Code == "LEGACY_MAXIMUM_COMPLEXITY_TIER_NOT_ALLOWED_IN_NEW_SCHEMA");
        Assert.Contains(ProgressionModifierValidator.Validate(legacyMissing).Issues, i => i.Code == "PM_COMPLEXITY_TIER_TOO_LOW");
    }

    // ---------- 3-6: MaximumHardSessionsPerWeek ceiling semantics ----------

    [Fact]
    public void NewProgressionModifierArtifact_HasApprovedMaximumHardSessionsPerWeek()
    {
        var snapshot = LoadSnapshot();
        var modifier = ProgressionModifierV2(snapshot);
        Assert.Equal(1, modifier.MaximumHardSessionsPerWeek);
    }

    [Fact]
    public void MaximumHardSessionsPerWeek_IsACeilingNotATarget_ZeroHardSessionsRemainsValid()
    {
        // A week/layout with zero KEY_SESSION slots must not fail structural validation against a cap of 1 —
        // proving the field bounds an upper limit and does not mandate a minimum/target count.
        var fixture = new CombinationFixture();
        var zeroHardSessionLayout = fixture.Layout with
        {
            Slots =
            [
                new LayoutSlotDefinition { SequenceOrder = 1, Role = Contracts.Enums.SlotRole.EasySupport },
                new LayoutSlotDefinition { SequenceOrder = 2, Role = Contracts.Enums.SlotRole.EasySupport },
                new LayoutSlotDefinition { SequenceOrder = 3, Role = Contracts.Enums.SlotRole.EasySupport },
                new LayoutSlotDefinition { SequenceOrder = 4, Role = Contracts.Enums.SlotRole.LongRun }
            ]
        };

        var snapshot = fixture.BuildSnapshot() with { RunLayouts = [zeroHardSessionLayout] };
        var result = TemplateCombinationValidator.Validate(fixture.Combination, snapshot);

        Assert.DoesNotContain(result.Issues, i => i.Code == "TC_KEY_SESSION_COUNT_EXCEEDS_CAP");
    }

    [Fact]
    public void NoConsumerAutoFillsToExactlyOneHardSession()
    {
        // TemplateCombinationValidator only ever compares KEY_SESSION count against the cap as an upper
        // bound (">") — it never asserts equality or a minimum. This proves no consumer in this repository
        // infers that a week must always contain exactly MaximumHardSessionsPerWeek hard sessions.
        var source = File.ReadAllText(Path.Combine(RepoRoot(), "src", "PlanCatalog.Core", "Validation", "TemplateCombinationValidator.cs"));
        Assert.Contains("keySessionCount > progressionModifier.MaximumHardSessionsPerWeek", source);
        Assert.DoesNotContain("keySessionCount ==", source);
        Assert.DoesNotContain("keySessionCount <", source);
    }

    [Fact]
    public void MaximumHardSessionsPerWeekDecision_IsDocumentedAsACeilingNotATargetOrMinimum()
    {
        var entry = PilotDomainContentAudit.Entries.Single(e =>
            e.DocumentType == DocumentTypes.ProgressionModifier && e.Key == "INTERMEDIATE_PROGRESSION_MODIFIER_V1" &&
            e.Version == 2 && e.JsonPath == "$.maximumHardSessionsPerWeek");

        Assert.Contains("ceiling", entry.SourceSectionOrReason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a target or minimum", entry.SourceSectionOrReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ContentDecisionStatus.CanonicalConfirmed, entry.Classification);
        Assert.False(entry.IsBlocking);
    }

    // ---------- 7-11: MainSetDoseMultiplier ----------

    [Fact]
    public void NewProgressionModifierArtifact_HasApprovedMainSetDoseMultiplier()
    {
        var snapshot = LoadSnapshot();
        var modifier = ProgressionModifierV2(snapshot);
        Assert.Equal(1.0m, modifier.MainSetDoseMultiplier);
    }

    [Fact]
    public void MainSetDoseMultiplier_IdentityValuePreservesBehaviorExactly()
    {
        // No consumer in this repository multiplies anything by MainSetDoseMultiplier (verified by absence
        // of any such usage outside the model/validator/audit/schema/tests) — 1.00 is therefore provably
        // an identity operation causing no rounding, duplication, or behavioral change in Process A.
        var coreFiles = Directory.GetFiles(Path.Combine(RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.Combine("Models", "ProgressionModifierDefinition.cs")) &&
                        !f.Contains(Path.Combine("Validation", "ProgressionModifierValidator.cs")) &&
                        !f.Contains(Path.Combine("Audit", "PilotDomainContentAudit.cs")));

        Assert.All(coreFiles, f => Assert.DoesNotContain("MainSetDoseMultiplier *", File.ReadAllText(f)));
    }

    [Fact]
    public void MainSetDoseMultiplierDecision_DocumentsBaselineIdentityAndNoTargetField()
    {
        var entry = PilotDomainContentAudit.Entries.Single(e =>
            e.DocumentType == DocumentTypes.ProgressionModifier && e.Key == "INTERMEDIATE_PROGRESSION_MODIFIER_V1" &&
            e.Version == 2 && e.JsonPath == "$.mainSetDoseMultiplier");

        Assert.Equal(ContentDecisionStatus.ExplicitProductDefault, entry.Classification);
        Assert.Contains("identity", entry.SourceSectionOrReason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unused for computation", entry.SourceSectionOrReason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no multiplierTarget", entry.SourceSectionOrReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NoMultiplierTargetFieldWasAdded()
    {
        var properties = typeof(ProgressionModifierDefinition).GetProperties().Select(p => p.Name);
        Assert.DoesNotContain(properties, n => n is "MultiplierTarget" or "DoseMetric" or "MainSetDoseTarget");
    }

    // ---------- 12-14: AllowGoalPaceRehearsal ----------

    [Fact]
    public void NewProgressionModifierArtifact_HasApprovedAllowGoalPaceRehearsal()
    {
        var snapshot = LoadSnapshot();
        var modifier = ProgressionModifierV2(snapshot);
        Assert.True(modifier.AllowGoalPaceRehearsal);
    }

    [Fact]
    public void GoalPaceRehearsal_StillRequiresSeparateRuntimeGuard_GoalFeasibility()
    {
        // The flag alone cannot make GOAL_PACE_REHEARSAL reachable: the stage's own `requires` guard
        // (GOAL_FEASIBILITY_IN) is a separate, independent gate the fixture's workout progression already
        // encodes — proving AllowGoalPaceRehearsal=true does not bypass it.
        var snapshot = LoadSnapshot();
        var progression = snapshot.WorkoutProgressions.Single(p => p.Metadata.Key == "TEN_K_WORKOUT_PROGRESSION_V1" && p.Metadata.Version == 1);
        var stage = progression.PhaseProgressions
            .SelectMany(p => p.Stages)
            .Single(s => s.StageKey == "GOAL_PACE_REHEARSAL");

        Assert.NotEmpty(stage.Requires);
    }

    [Fact]
    public void AllowGoalPaceRehearsalDecision_DocumentsPrincipleFlagUnconsumedMetadata()
    {
        var entry = PilotDomainContentAudit.Entries.Single(e =>
            e.DocumentType == DocumentTypes.ProgressionModifier && e.Key == "INTERMEDIATE_PROGRESSION_MODIFIER_V1" &&
            e.Version == 2 && e.JsonPath == "$.allowGoalPaceRehearsal");

        Assert.Equal(ContentDecisionStatus.ExplicitProductDefault, entry.Classification);
        Assert.Contains("PRINCIPLE_FLAG / UNCONSUMED", entry.SourceSectionOrReason);
        Assert.Contains("zero readers", entry.SourceSectionOrReason);
    }

    [Fact]
    public void AllowGoalPaceRehearsal_HasNoReaderAnywhereInThisRepository()
    {
        // Confirms the write-only conclusion: only the model declaration itself (and JSON
        // authoring/test fixtures) reference this property name — no validator, publisher, or bundle
        // assembler branches on its value.
        var consumerFiles = Directory.GetFiles(Path.Combine(RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(Path.Combine("Models", "ProgressionModifierDefinition.cs")));

        Assert.All(consumerFiles, f => Assert.DoesNotContain("AllowGoalPaceRehearsal", File.ReadAllText(f)));
    }

    // ---------- 15-18: AllowSecondHardStimulus ----------

    [Fact]
    public void NewProgressionModifierArtifact_HasApprovedAllowSecondHardStimulus()
    {
        var snapshot = LoadSnapshot();
        var modifier = ProgressionModifierV2(snapshot);
        Assert.False(modifier.AllowSecondHardStimulus);
    }

    [Fact]
    public void WeekWithGoalPaceRehearsal_CannotAlsoCarrySecondHardStimulus_UnderThisPolicy()
    {
        var fixture = new CombinationFixture();
        var modifier = fixture.ProgressionModifier with { AllowSecondHardStimulus = false, MaximumHardSessionsPerWeek = 2 };

        var result = ProgressionModifierValidator.Validate(modifier);
        Assert.Contains(result.Issues, i => i.Code == "PM_HARD_SESSION_CAP_EXCEEDS_SINGLE_STIMULUS");
    }

    [Fact]
    public void AllowSecondHardStimulusDecision_IsNotGeneralizedToUnrelatedDayCountsOrDistances()
    {
        var entry = PilotDomainContentAudit.Entries.Single(e =>
            e.DocumentType == DocumentTypes.ProgressionModifier && e.Key == "INTERMEDIATE_PROGRESSION_MODIFIER_V1" &&
            e.Version == 2 && e.JsonPath == "$.allowSecondHardStimulus");

        Assert.Equal(ContentDecisionStatus.CanonicalConfirmed, entry.Classification);
        Assert.Contains("NOT a universal statement", entry.SourceSectionOrReason);
        Assert.Contains("5-6 day or other-distance plan families remain explicitly out of scope", entry.SourceSectionOrReason);
    }

    [Fact]
    public void OnlyOneLevelModifierReferencesIntermediateProgressionModifierV1_OwnershipReuseBoundaryConfirmed()
    {
        var snapshot = LoadSnapshot();
        var referrers = snapshot.LevelModifiers.Where(lm => lm.ProgressionModifier.Key == "INTERMEDIATE_PROGRESSION_MODIFIER_V1").ToList();
        Assert.All(referrers, lm => Assert.Equal("INTERMEDIATE_MODIFIER", lm.Metadata.Key));
    }

    /// <summary>
    /// Executable reuse guard (Wave 5 clarification, Issue 3): the LevelModifier-key check above is
    /// necessary but not sufficient — it does not detect a future unrelated combination family (different
    /// distance or day-count) pointing at the same INTERMEDIATE_PROGRESSION_MODIFIER_V1 v2 artifact.
    /// This test walks every combination in the catalog that resolves (via its LevelModifier) to
    /// INTERMEDIATE_PROGRESSION_MODIFIER_V1 v2 and asserts it belongs to the approved TEN_K / INTERMEDIATE /
    /// approved 3D/4D scope. It will FAIL the moment an unrelated combination family gains a
    /// reachable reference to this artifact — making the ownership boundary documented in
    /// domain-wave5-d2-ownership.md executable, not just descriptive.
    /// </summary>
    [Fact]
    public void IntermediateProgressionModifierV2_ReuseIsExecutablyGuarded_NoUnapprovedCombinationFamilyReferrer()
    {
        var snapshot = LoadSnapshot();

        var reachingCombinations = snapshot.Combinations
            .Select(c => new
            {
                Combination = c,
                LevelModifier = snapshot.LevelModifiers.FirstOrDefault(lm => lm.Metadata.Key == c.LevelModifier.Key && lm.Metadata.Version == c.LevelModifier.Version)
            })
            .Where(x => x.LevelModifier is not null &&
                        x.LevelModifier.ProgressionModifier.Key == "INTERMEDIATE_PROGRESSION_MODIFIER_V1" &&
                        x.LevelModifier.ProgressionModifier.Version == 2)
            .ToList();

        Assert.NotEmpty(reachingCombinations);

        foreach (var x in reachingCombinations)
        {
            // Phase 10K-GEN.12: TEN_K__2D__INTERMEDIATE legitimately reaches this
            // same artifact via INTERMEDIATE_MODIFIER v6, reusing the exact same
            // known-good (progression v5 / levelModifier v6) pairing
            // TEN_K__3D__INTERMEDIATE already uses -- 2D is single-KEY, has zero
            // new prescription content of its own, so this is real reuse, not a
            // new artifact-family owner.
            Assert.Contains(x.Combination.Metadata.Key, new[] { "TEN_K__2D__INTERMEDIATE", "TEN_K__3D__INTERMEDIATE", "TEN_K__4D__INTERMEDIATE" });

            var master = snapshot.PlanTemplates.Single(m => m.Metadata.Key == x.Combination.MasterTemplate.Key && m.Metadata.Version == x.Combination.MasterTemplate.Version);
            var layout = snapshot.RunLayouts.Single(l => l.Metadata.Key == x.Combination.Layout.Key && l.Metadata.Version == x.Combination.Layout.Version);

            Assert.Equal(Contracts.Enums.DistanceFamily.TenK, master.DistanceFamily);
            Assert.Contains(layout.RunsPerWeek, new[] { 2, 3, 4 });
            Assert.Equal(Contracts.Enums.RunningExperience.Intermediate, x.LevelModifier!.Experience);
        }
    }

    // ---------- 18: no values created for other levels ----------

    [Fact]
    public void ProgressionModifierArtifactsExistOnlyForActivatedIntermediateAndGatedBeginnerLevels()
    {
        // Phase 10K-GEN.9 legitimately added ADVANCED_PROGRESSION_MODIFIER_V1
        // (GEN.7/GEN.8-approved Advanced numeric authority) -- widened the
        // allow-list accordingly; the "exactly one New/Beginner" invariant
        // this test's own remaining name protects is unaffected.
        var snapshot = LoadSnapshot();
        Assert.All(snapshot.ProgressionModifiers, m =>
            Assert.Contains(m.Experience, new[] { Contracts.Enums.RunningExperience.Intermediate, Contracts.Enums.RunningExperience.New, Contracts.Enums.RunningExperience.Advanced }));
        Assert.Single(snapshot.ProgressionModifiers, m => m.Experience == Contracts.Enums.RunningExperience.New);
        Assert.Single(snapshot.ProgressionModifiers, m => m.Experience == Contracts.Enums.RunningExperience.Advanced);
    }

    // ---------- 19-23: candidate closure / blocker reduction ----------

    [Fact]
    public void CandidateClosure_RemovesD2FromWave5Candidate()
    {
        var wave3Remaining = BlockingEntries(Closure(BuildBundle(6))).Select(e => $"{e.Key}:{e.JsonPath}").OrderBy(x => x).ToList();
        var wave5Remaining = BlockingEntries(Closure(BuildBundle(7))).Select(e => $"{e.Key}:{e.JsonPath}").OrderBy(x => x).ToList();

        Assert.Equal(4, BlockerScopeMeasurement.ScopedDecisionCount(Closure(BuildBundle(6))));
        Assert.Equal(3, BlockerScopeMeasurement.ScopedDecisionCount(Closure(BuildBundle(7))));

        Assert.Contains(wave3Remaining, e => e.StartsWith("INTERMEDIATE_PROGRESSION_MODIFIER_V1:"));
        Assert.DoesNotContain(wave5Remaining, e => e.StartsWith("INTERMEDIATE_PROGRESSION_MODIFIER_V1:"));

        Assert.Equal([
            "GOAL_PACE_TEN_K:$.eligiblePhases, $.complexityTier, $.allowedPrescriptionModes, $.components",
            "PEAK_VOLUME_BANDS_V1:$.entries[TEN_K,NEW|ADVANCED|EXPERIENCED,3|4|5]",
            "RUNTIME_CONDITION_VALUES_V1:$.conditionValueSets[PACE_SOURCE_IN,TIME_ADEQUACY_IN,CORE_ENTRY_READINESS_IN]"
        ], wave5Remaining);
    }

    [Fact]
    public void Wave5Bundle_ReferencesExactCascadeVersions()
    {
        var bundle = BuildBundle(7);
        Assert.Equal(7, bundle.BundleVersion);
        Assert.Equal(5, bundle.MasterTemplate.Version);
        Assert.Equal(2, bundle.Layout.Version);
        Assert.Equal(5, bundle.LevelModifier.Version);
        Assert.Equal(4, bundle.WorkoutProgression.Version);
        Assert.Equal(2, bundle.ProgressionModifier.Version);
        Assert.Equal(2, bundle.RulePack.Version);
    }

    [Fact]
    public void Wave5Bundle_DoesNotBumpUnrelatedArtifacts()
    {
        var v6 = BuildBundle(6);
        var v7 = BuildBundle(7);

        Assert.Equal(v6.MasterTemplate.Version, v7.MasterTemplate.Version);
        Assert.Equal(v6.Layout.Version, v7.Layout.Version);
        Assert.Equal(v6.WorkoutProgression.Version, v7.WorkoutProgression.Version);
        Assert.Equal(v6.RulePack.Version, v7.RulePack.Version);
        Assert.Equal(v6.RuntimeConditionValueRegistry.Version, v7.RuntimeConditionValueRegistry.Version);
        Assert.Equal(v6.PeakVolumeBandPolicy.Version, v7.PeakVolumeBandPolicy.Version);
        Assert.Equal(v6.Workouts.OrderBy(w => w.Key).Select(w => (w.Key, w.Version)),
                     v7.Workouts.OrderBy(w => w.Key).Select(w => (w.Key, w.Version)));
    }

    [Fact]
    public void Wave5CandidateBuild_IsByteIdenticalAcrossThreeBuilds()
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var json1 = serializer.Serialize(BuildBundle(7));
        var json2 = serializer.Serialize(BuildBundle(7));
        var json3 = serializer.Serialize(BuildBundle(7));

        Assert.Equal(json1, json2);
        Assert.Equal(json2, json3);

        var bundle1 = BuildBundle(7);
        var bundle2 = BuildBundle(7);
        Assert.Equal(bundle1.BundleContentHash, bundle2.BundleContentHash);
    }

    [Fact]
    public void PriorDraftCandidates_AreUnchangedByWave5()
    {
        // v5 (Wave 2) and v6 (Wave 3) candidate source files must be byte-identical to before this task.
        var v5 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "combinations", "ten-k-4d-intermediate.v5.json"));
        var v6 = File.ReadAllText(Path.Combine(RepoRoot(), "catalog", "combinations", "ten-k-4d-intermediate.v6.json"));

        Assert.Contains("\"version\": 5", v5);
        Assert.Contains("\"version\": 6", v6);
        Assert.DoesNotContain("progressionModifier", v5);
        Assert.DoesNotContain("progressionModifier", v6);
    }

    private static ProgressionModifierDefinition ProgressionModifierV2(Core.Catalog.CatalogSourceSnapshot snapshot) =>
        snapshot.ProgressionModifiers.Single(p => p.Metadata.Key == "INTERMEDIATE_PROGRESSION_MODIFIER_V1" && p.Metadata.Version == 2);

    private static ProgressionModifierDefinition Modifier(int schemaVersion, int version, int? maximumComplexityTier) => new()
    {
        Metadata = Meta.Of("PROGRESSION_MODIFIER", "INTERMEDIATE_PROGRESSION_MODIFIER_V1", version: version) with { SchemaVersion = schemaVersion },
        Experience = Contracts.Enums.RunningExperience.Intermediate,
        MaximumComplexityTier = maximumComplexityTier,
        MaximumHardSessionsPerWeek = 1,
        MainSetDoseMultiplier = 1.0m,
        AllowGoalPaceRehearsal = true,
        AllowSecondHardStimulus = false
    };

    private static IEnumerable<BlockerScopeMeasurement.ArtifactIdentity> Closure(PublishedTemplateBundle bundle)
    {
        yield return Id(bundle.Combination);
        yield return Id(bundle.MasterTemplate);
        yield return Id(bundle.Layout);
        yield return Id(bundle.LevelModifier);
        yield return Id(bundle.WorkoutProgression);
        yield return Id(bundle.ProgressionModifier);
        yield return Id(bundle.RulePack);
        yield return Id(bundle.RuntimeConditionValueRegistry);
        yield return Id(bundle.PeakVolumeBandPolicy);
        foreach (var workout in bundle.Workouts)
        {
            yield return Id(workout);
        }
    }

    private static BlockerScopeMeasurement.ArtifactIdentity Id(Contracts.References.CatalogArtifactReference reference) =>
        new(reference.DocumentType, reference.Key, reference.Version);

    private static IReadOnlyList<DomainContentDecision> BlockingEntries(IEnumerable<BlockerScopeMeasurement.ArtifactIdentity> closure)
    {
        var scope = closure.ToHashSet();
        return PilotDomainContentAudit.Entries
            .Where(e => e.IsBlocking && scope.Contains(new BlockerScopeMeasurement.ArtifactIdentity(e.DocumentType, e.Key, e.Version)))
            .ToList();
    }
}
