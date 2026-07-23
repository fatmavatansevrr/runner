using RunningApp.Application.Common;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.Common;

public sealed class WeekdayCsvTests
{
    [Fact]
    public void ToCsv_List_MapsToFullDayNamesInOrder()
    {
        var csv = WeekdayCsv.ToCsv(new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun });
        Assert.Equal("Monday,Wednesday,Friday,Sunday", csv);
    }

    [Fact]
    public void ToCsv_NullList_ReturnsNull() => Assert.Null(WeekdayCsv.ToCsv((System.Collections.Generic.IReadOnlyList<Weekday>?)null));

    [Fact]
    public void ToCsv_EmptyList_ReturnsNull() => Assert.Null(WeekdayCsv.ToCsv(System.Array.Empty<Weekday>()));

    [Fact]
    public void ToCsv_SingleDay_MapsToFullDayName() => Assert.Equal("Sunday", WeekdayCsv.ToCsv(Weekday.Sun));

    [Fact]
    public void ToCsv_NullSingleDay_ReturnsNull() => Assert.Null(WeekdayCsv.ToCsv((Weekday?)null));
}
