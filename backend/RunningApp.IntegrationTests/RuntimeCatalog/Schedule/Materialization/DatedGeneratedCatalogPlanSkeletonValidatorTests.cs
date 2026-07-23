using System;
using System.Linq;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>Backend Integration Phase 4F.5 — tests for <see cref="DatedGeneratedCatalogPlanSkeletonValidator"/>.</summary>
public sealed class DatedGeneratedCatalogPlanSkeletonValidatorTests
{
    private static readonly DateOnly WednesdayStart = new(2026, 8, 5);
    private static readonly DayOfWeek[] PreferredDays = { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;

    private static DatedGeneratedCatalogPlanSkeleton BuildValidDatedSkeleton(int weekCount = 12)
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart, weekCount);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);
        return CatalogCalendarAssignmentFixtures.RealMaterializer().Materialize(context);
    }

    [Fact]
    public void Validate_WellFormedDatedSkeleton_IsValid()
    {
        var dated = BuildValidDatedSkeleton();
        var validator = CatalogCalendarAssignmentFixtures.RealValidator();

        var result = validator.Validate(dated, PreferredDays, LongRunDay);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_UnsupportedSchemaVersion_IsRejected()
    {
        var dated = BuildValidDatedSkeleton();
        var tampered = dated with { SchemaVersion = "999" };
        var validator = CatalogCalendarAssignmentFixtures.RealValidator();

        var result = validator.Validate(tampered, PreferredDays, LongRunDay);

        Assert.False(result.IsValid);
        Assert.Contains(DatedGeneratedCatalogPlanSkeletonValidationError.UnsupportedSchemaVersion, result.Errors);
    }

    [Fact]
    public void Validate_WeekCountMismatch_IsRejected()
    {
        var dated = BuildValidDatedSkeleton();
        var tampered = dated with { PlannedWeekCount = dated.PlannedWeekCount + 1 };
        var validator = CatalogCalendarAssignmentFixtures.RealValidator();

        var result = validator.Validate(tampered, PreferredDays, LongRunDay);

        Assert.False(result.IsValid);
        Assert.Contains(DatedGeneratedCatalogPlanSkeletonValidationError.ActualWeekCountMismatch, result.Errors);
    }

    [Fact]
    public void Validate_LongRunDateNotOnLongRunDayPreference_IsRejected()
    {
        var dated = BuildValidDatedSkeleton();
        var validator = CatalogCalendarAssignmentFixtures.RealValidator();

        // Validate against a DIFFERENT long-run-day-preference than the one
        // actually used to materialize -- must be rejected.
        var result = validator.Validate(dated, PreferredDays, DayOfWeek.Friday);

        Assert.False(result.IsValid);
        Assert.Contains(DatedGeneratedCatalogPlanSkeletonValidationError.LongRunDateNotOnLongRunDayPreference, result.Errors);
    }

    [Fact]
    public void Validate_SessionWeekdayNotInPreferredDays_IsRejected()
    {
        var dated = BuildValidDatedSkeleton();
        var validator = CatalogCalendarAssignmentFixtures.RealValidator();

        // Validate against a narrower PreferredDays set that excludes one of
        // the weekdays actually used.
        var narrowerDays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday };

        var result = validator.Validate(dated, narrowerDays, LongRunDay);

        Assert.False(result.IsValid);
        Assert.Contains(DatedGeneratedCatalogPlanSkeletonValidationError.SessionWeekdayNotInPreferredDays, result.Errors);
    }

    [Fact]
    public void Validate_DuplicateSessionDateWithinWeek_IsRejected()
    {
        var dated = BuildValidDatedSkeleton(weekCount: 1);
        var week = dated.Weeks[0];
        var slots = week.SessionSlots.ToList();
        // Force two slots onto the same date.
        var tamperedSlots = new[]
        {
            slots[0] with { SessionDate = slots[1].SessionDate, SessionDayOfWeek = slots[1].SessionDayOfWeek },
            slots[1], slots[2], slots[3],
        };
        var tamperedWeek = week with { SessionSlots = tamperedSlots };
        var tampered = dated with { Weeks = new[] { tamperedWeek } };
        var validator = CatalogCalendarAssignmentFixtures.RealValidator();

        var result = validator.Validate(tampered, PreferredDays, LongRunDay);

        Assert.False(result.IsValid);
        Assert.Contains(DatedGeneratedCatalogPlanSkeletonValidationError.DuplicateSessionDateWithinWeek, result.Errors);
    }

    [Fact]
    public void Validate_SessionWeekdayDateMismatch_IsRejected()
    {
        var dated = BuildValidDatedSkeleton(weekCount: 1);
        var week = dated.Weeks[0];
        var slot = week.SessionSlots[0];
        var mismatchedWeekday = (DayOfWeek)(((int)slot.SessionDayOfWeek + 1) % 7);
        var tamperedSlot = slot with { SessionDayOfWeek = mismatchedWeekday };
        var tamperedSlots = week.SessionSlots.Select((s, i) => i == 0 ? tamperedSlot : s).ToList();
        var tamperedWeek = week with { SessionSlots = tamperedSlots };
        var tampered = dated with { Weeks = new[] { tamperedWeek } };
        var validator = CatalogCalendarAssignmentFixtures.RealValidator();

        var result = validator.Validate(tampered, PreferredDays, LongRunDay);

        Assert.False(result.IsValid);
        Assert.Contains(DatedGeneratedCatalogPlanSkeletonValidationError.SessionWeekdayDateMismatch, result.Errors);
    }

    [Fact]
    public void Validate_HasNoDatabaseClockHttpResolverOrCatalogLoaderDependency()
    {
        var ctor = typeof(DatedGeneratedCatalogPlanSkeletonValidator).GetConstructors().Single();
        Assert.Empty(ctor.GetParameters());
    }
}
