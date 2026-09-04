using System.Text.Json;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

public sealed class PreparationRunwayWeekMaterializerTests
{
    private static string RepoRoot() => RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
    private static string CatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");
    private static ICatalogWorkoutDefinitionLoader Loader() =>
        new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() }));

    [Theory]
    [InlineData(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3, "Consistency=1,GeneralEndurance=1,AerobicStrength=0,PreSpecificTransition=1")]
    [InlineData(PreparationRunwayAllocationProfile.ConsistencyNeeded, 4, "Consistency=1,GeneralEndurance=2,AerobicStrength=0,PreSpecificTransition=1")]
    [InlineData(PreparationRunwayAllocationProfile.ConsistencyNeeded, 5, "Consistency=2,GeneralEndurance=2,AerobicStrength=0,PreSpecificTransition=1")]
    [InlineData(PreparationRunwayAllocationProfile.ConsistencyNeeded, 6, "Consistency=2,GeneralEndurance=3,AerobicStrength=0,PreSpecificTransition=1")]
    [InlineData(PreparationRunwayAllocationProfile.ConsistencyNeeded, 7, "Consistency=2,GeneralEndurance=4,AerobicStrength=0,PreSpecificTransition=1")]
    [InlineData(PreparationRunwayAllocationProfile.ConsistencyNeeded, 8, "Consistency=2,GeneralEndurance=5,AerobicStrength=0,PreSpecificTransition=1")]
    [InlineData(PreparationRunwayAllocationProfile.CoreEntryReady, 3, "Consistency=0,GeneralEndurance=1,AerobicStrength=1,PreSpecificTransition=1")]
    [InlineData(PreparationRunwayAllocationProfile.CoreEntryReady, 4, "Consistency=0,GeneralEndurance=2,AerobicStrength=1,PreSpecificTransition=1")]
    [InlineData(PreparationRunwayAllocationProfile.CoreEntryReady, 5, "Consistency=0,GeneralEndurance=2,AerobicStrength=2,PreSpecificTransition=1")]
    [InlineData(PreparationRunwayAllocationProfile.CoreEntryReady, 6, "Consistency=0,GeneralEndurance=3,AerobicStrength=2,PreSpecificTransition=1")]
    [InlineData(PreparationRunwayAllocationProfile.CoreEntryReady, 7, "Consistency=0,GeneralEndurance=4,AerobicStrength=2,PreSpecificTransition=1")]
    [InlineData(PreparationRunwayAllocationProfile.CoreEntryReady, 8, "Consistency=0,GeneralEndurance=5,AerobicStrength=2,PreSpecificTransition=1")]
    internal async Task EveryApprovedMatrix_MaterializesCanonicalUndatedWeeks(
        PreparationRunwayAllocationProfile profile,
        int runwayWeeks,
        string expectedAllocation)
    {
        var (request, allocations) = await BuildRealRequestAsync(profile, runwayWeeks);
        Assert.Equal(expectedAllocation, string.Join(",", allocations.Select(a => $"{a.BlockKey}={a.AllocatedWeeks}")));

        var result = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());

        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.Equal(runwayWeeks, result.TotalWeekCount);
        var weeks = result.Weeks!;
        Assert.Equal(Enumerable.Range(1, runwayWeeks), weeks.Select(w => w.RunwayWeekNumber));
        Assert.Equal(allocations.Where(a => a.AllocatedWeeks > 0).Select(a => a.BlockKey),
            weeks.Select(w => w.BlockType).Distinct());

        foreach (var allocation in allocations.Where(a => a.AllocatedWeeks > 0))
        {
            var blockWeeks = weeks.Where(w => w.BlockType == allocation.BlockKey).ToArray();
            Assert.Equal(allocation.AllocatedWeeks, blockWeeks.Length);
            Assert.Equal(Enumerable.Range(1, allocation.AllocatedWeeks), blockWeeks.Select(w => w.BlockWeekOrdinal));
            Assert.Equal(Enumerable.Range(1, allocation.AllocatedWeeks), blockWeeks.Select(w => w.ProgressionStepNumber));
        }

        Assert.All(weeks, AssertCanonicalWeek);
    }

    [Fact]
    public async Task AllBlockSteps_UseTheApprovedAnchorRoleExactlyOnce()
    {
        var consistency = await MaterializeAsync(PreparationRunwayAllocationProfile.ConsistencyNeeded, 8);
        var ready = await MaterializeAsync(PreparationRunwayAllocationProfile.CoreEntryReady, 8);
        var weeks = consistency.Concat(ready).ToArray();

        foreach (var week in weeks)
        {
            var anchor = Assert.Single(week.OrderedWorkoutSlots, s => s.SourceKind == PreparationRunwayWorkoutSlotSource.Anchor);
            var expectedRole = week.BlockType switch
            {
                PreparationRunwayBlockType.Consistency when week.ProgressionStepNumber == 1 => PreparationRunwaySlotRole.KeySession,
                PreparationRunwayBlockType.Consistency => PreparationRunwaySlotRole.LongRun,
                PreparationRunwayBlockType.GeneralEndurance => PreparationRunwaySlotRole.LongRun,
                PreparationRunwayBlockType.AerobicStrength => PreparationRunwaySlotRole.KeySession,
                PreparationRunwayBlockType.PreSpecificTransition => PreparationRunwaySlotRole.KeySession,
                _ => throw new InvalidOperationException(),
            };
            Assert.Equal(expectedRole, anchor.SlotRole);
        }
    }

    [Fact]
    public async Task EasyAndLongRunSupportDefaults_FillOnlyRolesNotOccupiedByAnchor()
    {
        var weeks = (await MaterializeAsync(PreparationRunwayAllocationProfile.ConsistencyNeeded, 8))
            .Concat(await MaterializeAsync(PreparationRunwayAllocationProfile.CoreEntryReady, 8));

        foreach (var week in weeks)
        {
            Assert.All(week.OrderedWorkoutSlots.Where(s => s.SourceKind == PreparationRunwayWorkoutSlotSource.SupportPolicy), slot =>
            {
                var expected = slot.SlotRole == PreparationRunwaySlotRole.LongRun ? "LONG_RUN_STANDARD" : "EASY_STANDARD";
                Assert.Equal(expected, slot.WorkoutId);
                Assert.Equal(5, slot.WorkoutVersion);
            });
        }
    }

    [Fact]
    public async Task MissingPositiveBlockBinding_IsRejectedWithoutPartialWeeks()
    {
        var (request, _) = await BuildRealRequestAsync(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3);
        request = request with { OrderedBlockBindings = request.OrderedBlockBindings.Skip(1).ToArray() };
        await AssertFailure(request, PreparationRunwayWeekMaterializationFailureCode.MissingBlockBinding);
    }

    [Fact]
    public async Task BindingCountMismatch_IsRejectedWithoutPartialWeeks()
    {
        var (request, _) = await BuildRealRequestAsync(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3);
        var first = request.OrderedBlockBindings[0];
        var brokenBinding = first.Binding with { AllocatedWeeks = first.Binding.AllocatedWeeks + 1 };
        request = request with { OrderedBlockBindings = [first with { Binding = brokenBinding }, .. request.OrderedBlockBindings.Skip(1)] };
        await AssertFailure(request, PreparationRunwayWeekMaterializationFailureCode.BindingCountMismatch);
    }

    [Fact]
    public async Task ClaimedAndActualBindingBlockMismatch_IsRejected()
    {
        var (request, _) = await BuildRealRequestAsync(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3);
        var first = request.OrderedBlockBindings[0];
        var wrong = first.Binding with { BlockKey = PreparationRunwayBlockType.AerobicStrength };
        request = request with { OrderedBlockBindings = [first with { Binding = wrong }, .. request.OrderedBlockBindings.Skip(1)] };
        await AssertFailure(request, PreparationRunwayWeekMaterializationFailureCode.BlockBindingMismatch);
    }

    [Fact]
    public async Task MissingPositiveBlockRolePolicy_IsRejected()
    {
        var (request, _) = await BuildRealRequestAsync(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3);
        request = request with { BlockRolePolicies = request.BlockRolePolicies.Where(p => p.BlockKey != PreparationRunwayBlockType.Consistency).ToArray() };
        await AssertFailure(request, PreparationRunwayWeekMaterializationFailureCode.UnsupportedBlockRolePolicy);
    }

    [Fact]
    public async Task RoleIncompatibleAnchor_IsRejected()
    {
        var (request, _) = await BuildRealRequestAsync(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3);
        var policies = request.BlockRolePolicies.Select(p => p.BlockKey == PreparationRunwayBlockType.Consistency
            ? p with { AnchorRoleByProgressionStep = new Dictionary<int, PreparationRunwaySlotRole> { [1] = PreparationRunwaySlotRole.LongRun, [2] = PreparationRunwaySlotRole.LongRun } }
            : p).ToArray();
        request = request with { BlockRolePolicies = policies };
        await AssertFailure(request, PreparationRunwayWeekMaterializationFailureCode.AnchorRoleIncompatible);
    }

    [Fact]
    public async Task InvalidSupportWorkoutReference_IsRejected()
    {
        var (request, _) = await BuildRealRequestAsync(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3);
        request = request with
        {
            SupportWorkoutPolicy = request.SupportWorkoutPolicy with
            {
                EasySupportDefault = new PreparationRunwayWorkoutReference("DOES_NOT_EXIST", 1),
            },
        };
        await AssertFailure(request, PreparationRunwayWeekMaterializationFailureCode.SupportWorkoutReferenceInvalid);
    }

    [Fact]
    public async Task DuplicateAllocation_IsRejected()
    {
        var (request, _) = await BuildRealRequestAsync(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3);
        request = request with { OrderedBlockAllocations = [.. request.OrderedBlockAllocations, request.OrderedBlockAllocations[0]] };
        await AssertFailure(request, PreparationRunwayWeekMaterializationFailureCode.DuplicateBlockAllocation);
    }

    [Fact]
    public async Task InvalidCanonicalOrder_IsRejected()
    {
        var (request, _) = await BuildRealRequestAsync(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3);
        request = request with
        {
            OrderedBlockAllocations = request.OrderedBlockAllocations.Select(a => a.BlockKey == PreparationRunwayBlockType.Consistency
                ? a with { CanonicalOrder = 9 }
                : a).ToArray(),
        };
        await AssertFailure(request, PreparationRunwayWeekMaterializationFailureCode.InvalidBlockOrder);
    }

    [Fact]
    public async Task NonCanonicalFourRoleLayout_IsRejected()
    {
        // Phase 10K-GEN.9: [KEY, EASY, LONG] (1 EASY_SUPPORT) was this test's
        // original "non-canonical" example, but GEN.7/GEN.9 legitimately
        // approved and dark-activated exactly this shape for Advanced x3D
        // Runway (1K+1E+1L) -- see PreparationRunwayWeeklyShape's own widened
        // ApprovedEasySupportCounts. Replaced with a shape that remains
        // genuinely non-canonical (zero EASY_SUPPORT slots), preserving this
        // test's real intent (reject cardinality violations) rather than
        // weakening it.
        var (request, _) = await BuildRealRequestAsync(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3);
        request = request with
        {
            CanonicalWeeklyLayout = request.CanonicalWeeklyLayout with
            {
                OrderedRoles = [PreparationRunwaySlotRole.KeySession, PreparationRunwaySlotRole.LongRun],
            },
        };
        await AssertFailure(request, PreparationRunwayWeekMaterializationFailureCode.WeekRoleCardinalityViolation);
    }

    [Fact]
    public async Task InputOrderDoesNotAffectCanonicalResult()
    {
        var (request, _) = await BuildRealRequestAsync(PreparationRunwayAllocationProfile.CoreEntryReady, 8);
        var first = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        var reordered = request with
        {
            OrderedBlockAllocations = request.OrderedBlockAllocations.Reverse().ToArray(),
            OrderedBlockBindings = request.OrderedBlockBindings.Reverse().ToArray(),
            BlockRolePolicies = request.BlockRolePolicies.Reverse().ToArray(),
        };
        var second = await PreparationRunwayWeekMaterializer.MaterializeAsync(reordered, Loader());
        Assert.Equal(JsonSerializer.Serialize(first.Weeks), JsonSerializer.Serialize(second.Weeks));
    }

    [Fact]
    public async Task RepeatedCallsAreValueIdentical()
    {
        var (request, _) = await BuildRealRequestAsync(PreparationRunwayAllocationProfile.ConsistencyNeeded, 8);
        var first = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        var second = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
    }

    [Fact]
    public async Task SyntheticNonTenKBlockPolicy_ProvesStructuralGenericity()
    {
        var request = new PreparationRunwayWeekMaterializationRequest<string>(
            "SYNTHETIC_PROFILE", "SYNTHETIC_CANDIDATE", 1, "SYNTHETIC_ALLOCATION", 1,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout(4),
            [new PreparationRunwayBlockAllocationOutcome<string>("CUSTOM_BLOCK", 1, 7)],
            [new PreparationRunwayMaterializationBlockBinding<string>(
                "CUSTOM_BLOCK",
                new PreparationRunwayBlockWorkoutBinding<string>("CUSTOM_BLOCK", 1, [new PreparationRunwayWorkoutReference("EASY_STANDARD", 5)]),
                "CUSTOM_PROGRESSION", 1, [1])],
            [new PreparationRunwayBlockWeekRolePolicy<string>("CUSTOM_BLOCK", 7, "SYNTHETIC_ROLE_POLICY", 1,
                new Dictionary<int, PreparationRunwaySlotRole> { [1] = PreparationRunwaySlotRole.KeySession })],
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildSupportPolicy());

        var result = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.Equal("CUSTOM_BLOCK", Assert.Single(result.Weeks!).BlockType);
    }

    [Fact]
    public void ContractsAndEngine_AreProductionOwnedUndatedAndPrescriptionFree()
    {
        Assert.False(typeof(PreparationRunwayWeekMaterializer).IsPublic);
        var contractTypes = new[]
        {
            typeof(PreparationRunwayMaterializedWeek<>),
            typeof(PreparationRunwayMaterializedWorkoutSlot<>),
            typeof(PreparationRunwayWeekMaterializationRequest<>),
        };
        foreach (var type in contractTypes)
        {
            var propertyNames = type.GetProperties().Select(p => p.Name).ToArray();
            Assert.DoesNotContain(type.GetProperties(), p => p.PropertyType == typeof(DateOnly) || p.Name.EndsWith("Date", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(propertyNames, n => n.Contains("Distance", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(propertyNames, n => n.Contains("Duration", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(propertyNames, n => n.Contains("Pace", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Materializer_RemainsAbsentFromPublicPersistenceAndPreviewRoutingCode()
    {
        foreach (var relative in new[] { "backend/RunningApp.Api", "backend/RunningApp.Persistence", "backend/RunningApp.Application/RuntimeCatalog/PreviewRouting" })
        {
            var root = Path.Combine(RepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar));
            var sources = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                            !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
            Assert.All(sources, source => Assert.DoesNotContain("PreparationRunwayWeekMaterializer", File.ReadAllText(source)));
        }
    }

    private static void AssertCanonicalWeek(PreparationRunwayMaterializedWeek<PreparationRunwayBlockType> week)
    {
        Assert.Equal(new[]
        {
            PreparationRunwaySlotRole.KeySession,
            PreparationRunwaySlotRole.EasySupport,
            PreparationRunwaySlotRole.EasySupport,
            PreparationRunwaySlotRole.LongRun,
        }, week.OrderedWorkoutSlots.Select(s => s.SlotRole));
        Assert.Equal(Enumerable.Range(1, 4), week.OrderedWorkoutSlots.Select(s => s.SlotOrdinal));
        Assert.Single(week.OrderedWorkoutSlots, s => s.SlotRole == PreparationRunwaySlotRole.KeySession);
        Assert.Equal(2, week.OrderedWorkoutSlots.Count(s => s.SlotRole == PreparationRunwaySlotRole.EasySupport));
        Assert.Single(week.OrderedWorkoutSlots, s => s.SlotRole == PreparationRunwaySlotRole.LongRun);
        Assert.Single(week.OrderedWorkoutSlots, s => s.SourceKind == PreparationRunwayWorkoutSlotSource.Anchor);
        Assert.All(week.OrderedWorkoutSlots, s =>
        {
            Assert.Equal(week.BlockType, s.SourceBlockType);
            Assert.Equal(week.ProgressionId, s.SourceProgressionId);
            Assert.Equal(week.ProgressionVersion, s.SourceProgressionVersion);
            Assert.Equal(week.ProgressionStepNumber, s.SourceProgressionStep);
        });
        Assert.Equal(TenKPreparationRunwayWeekMaterializationPolicyFactory.CandidateKey, week.Provenance.CandidateKey);
        Assert.Equal(new PlanCatalogReference("RUN_LAYOUT_4D", 2), week.Provenance.SourceLayout);
    }

    private static async Task AssertFailure(
        PreparationRunwayWeekMaterializationRequest<PreparationRunwayBlockType> request,
        PreparationRunwayWeekMaterializationFailureCode code)
    {
        var result = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        Assert.False(result.IsSuccess);
        Assert.Equal(code, result.FailureCode);
        Assert.Null(result.Weeks);
        Assert.Null(result.TotalWeekCount);
    }

    private static async Task<IReadOnlyList<PreparationRunwayMaterializedWeek<PreparationRunwayBlockType>>> MaterializeAsync(
        PreparationRunwayAllocationProfile profile,
        int runwayWeeks)
    {
        var (request, _) = await BuildRealRequestAsync(profile, runwayWeeks);
        var result = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        Assert.True(result.IsSuccess, result.FailureReason);
        return result.Weeks!;
    }

    private static async Task<(PreparationRunwayWeekMaterializationRequest<PreparationRunwayBlockType> Request,
        IReadOnlyList<PreparationRunwayBlockAllocationOutcome<PreparationRunwayBlockType>> Allocations)> BuildRealRequestAsync(
        PreparationRunwayAllocationProfile profile,
        int runwayWeeks)
    {
        var allocationResult = PreparationRunwayBlockAllocationEngine.Allocate(
            runwayWeeks, TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(profile));
        Assert.True(allocationResult.IsSuccess, allocationResult.FailureReason);

        var bindings = new List<PreparationRunwayMaterializationBlockBinding<PreparationRunwayBlockType>>();
        foreach (var allocation in allocationResult.Allocations!.Where(a => a.AllocatedWeeks > 0))
        {
            var (progressionId, version) = ProgressionFor(allocation.BlockKey);
            var catalogDefinition = await PreparationRunwayBlockProgressionCatalogReader.LoadAsync(CatalogRoot(), progressionId, version);
            var typedDefinition = new PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType>(
                catalogDefinition.ProgressionId,
                catalogDefinition.Version,
                ParseCatalogBlockKey(catalogDefinition.BlockKey),
                catalogDefinition.OrderedSteps);
            var bindingResult = PreparationRunwayBlockWorkoutBindingEngine.Bind(
                new PreparationRunwayBlockWorkoutBindingRequest<PreparationRunwayBlockType>(
                    allocation.BlockKey, allocation.AllocatedWeeks, typedDefinition));
            Assert.True(bindingResult.IsSuccess, bindingResult.FailureReason);
            bindings.Add(new PreparationRunwayMaterializationBlockBinding<PreparationRunwayBlockType>(
                allocation.BlockKey,
                bindingResult.Binding!,
                typedDefinition.ProgressionId,
                typedDefinition.Version,
                Enumerable.Range(1, allocation.AllocatedWeeks).ToArray()));
        }

        var request = new PreparationRunwayWeekMaterializationRequest<PreparationRunwayBlockType>(
            profile.ToString(),
            TenKPreparationRunwayWeekMaterializationPolicyFactory.CandidateKey,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.CandidateVersion,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyId,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyVersion,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout(4),
            allocationResult.Allocations!,
            bindings,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies(4),
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildSupportPolicy());
        return (request, allocationResult.Allocations!);
    }

    private static (string Id, int Version) ProgressionFor(PreparationRunwayBlockType block) => block switch
    {
        PreparationRunwayBlockType.Consistency => ("TEN_K_CONSISTENCY_PROGRESSION", 1),
        PreparationRunwayBlockType.GeneralEndurance => ("TEN_K_GENERAL_ENDURANCE_PROGRESSION", 1),
        PreparationRunwayBlockType.AerobicStrength => ("TEN_K_AEROBIC_STRENGTH_PROGRESSION", 1),
        PreparationRunwayBlockType.PreSpecificTransition => ("TEN_K_PRE_SPECIFIC_TRANSITION_PROGRESSION", 1),
        _ => throw new ArgumentOutOfRangeException(nameof(block)),
    };

    private static PreparationRunwayBlockType ParseCatalogBlockKey(string key) => key switch
    {
        "CONSISTENCY" => PreparationRunwayBlockType.Consistency,
        "GENERAL_ENDURANCE" => PreparationRunwayBlockType.GeneralEndurance,
        "AEROBIC_STRENGTH" => PreparationRunwayBlockType.AerobicStrength,
        "PRE_SPECIFIC_TRANSITION" => PreparationRunwayBlockType.PreSpecificTransition,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unsupported catalog runway block key."),
    };
}
