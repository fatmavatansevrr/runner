using System.Reflection;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Models;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>Guards against reintroducing fields/values the brief explicitly deprecates — see brief §Source of truth.</summary>
public sealed class DeprecatedFieldRegressionTests
{
    [Fact]
    public void LevelModifierDefinition_DoesNotContainProgressionProfileKey()
    {
        AssertNoProperty<LevelModifierDefinition>("ProgressionProfileKey");
    }

    [Fact]
    public void LevelModifierDefinition_DoesNotContainPeakVolumeBandKey()
    {
        AssertNoProperty<LevelModifierDefinition>("PeakVolumeBandKey");
    }

    [Fact]
    public void LevelModifierDefinition_DoesNotContainMaximumHardSessionsPerWeek()
    {
        AssertNoProperty<LevelModifierDefinition>("MaximumHardSessionsPerWeek");
    }

    [Fact]
    public void ProgressionModifierDefinition_IsSoleOwnerOfMaximumHardSessionsPerWeek()
    {
        Assert.NotNull(typeof(ProgressionModifierDefinition).GetProperty("MaximumHardSessionsPerWeek"));
    }

    [Fact]
    public void WorkoutProgressionStageDefinition_ContainsNoConcreteWeekField()
    {
        var names = typeof(WorkoutProgressionStageDefinition).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name);
        var forbidden = new[] { "Week", "WeekNumber", "StartWeek", "EndWeek", "CalendarDate", "ScheduledDate", "Date" };
        Assert.Empty(names.Intersect(forbidden, StringComparer.Ordinal));
    }

    [Fact]
    public void CatalogStatus_DoesNotContainUnsupported()
    {
        var names = Enum.GetNames<CatalogStatus>();
        Assert.DoesNotContain(names, n => string.Equals(n, "Unsupported", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(new[] { "Draft", "Validated", "Published", "Retired" }, names);
    }

    private static void AssertNoProperty<T>(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.Null(property);
    }
}
