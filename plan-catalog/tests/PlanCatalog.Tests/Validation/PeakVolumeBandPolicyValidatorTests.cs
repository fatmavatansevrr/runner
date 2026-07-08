using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

public sealed class PeakVolumeBandPolicyValidatorTests
{
    private static PeakVolumeBandPolicy Valid() => new()
    {
        Metadata = Meta.Of(DocumentTypes.PeakVolumeBandPolicy, "PEAK_VOLUME_BANDS_V1"),
        Entries =
        [
            new PeakVolumeBandEntry { DistanceFamily = DistanceFamily.TenK, Experience = RunningExperience.Intermediate, RunsPerWeek = 4, MinimumKm = 30m, MaximumKm = 45m }
        ]
    };

    [Fact]
    public void Valid_Passes()
    {
        Assert.True(PeakVolumeBandPolicyValidator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void DuplicateTuple_Fails()
    {
        var policy = Valid() with
        {
            Entries =
            [
                new PeakVolumeBandEntry { DistanceFamily = DistanceFamily.TenK, Experience = RunningExperience.Intermediate, RunsPerWeek = 4, MinimumKm = 30m, MaximumKm = 45m },
                new PeakVolumeBandEntry { DistanceFamily = DistanceFamily.TenK, Experience = RunningExperience.Intermediate, RunsPerWeek = 4, MinimumKm = 32m, MaximumKm = 46m }
            ]
        };

        var result = PeakVolumeBandPolicyValidator.Validate(policy);
        Assert.Contains(result.Issues, i => i.Code == "PVB_DUPLICATE_TUPLE");
    }

    [Fact]
    public void MinimumExceedsMaximum_Fails()
    {
        var policy = Valid() with { Entries = [Valid().Entries[0] with { MinimumKm = 50m, MaximumKm = 40m }] };
        var result = PeakVolumeBandPolicyValidator.Validate(policy);
        Assert.Contains(result.Issues, i => i.Code == "PVB_MINIMUM_EXCEEDS_MAXIMUM");
    }
}
