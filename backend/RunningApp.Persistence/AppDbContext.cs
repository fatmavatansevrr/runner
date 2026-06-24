using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence.Converters;
using System;

namespace RunningApp.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<PlanTemplate> PlanTemplates => Set<PlanTemplate>();
    public DbSet<PlanPreview> PlanPreviews => Set<PlanPreview>();
    public DbSet<TrainingPlan> TrainingPlans => Set<TrainingPlan>();
    public DbSet<TrainingWeek> TrainingWeeks => Set<TrainingWeek>();
    public DbSet<TrainingDay> TrainingDays => Set<TrainingDay>();
    public DbSet<WorkoutLog> WorkoutLogs => Set<WorkoutLog>();
    public DbSet<NotTodayDecision> NotTodayDecisions => Set<NotTodayDecision>();
    public DbSet<PendingConfirmation> PendingConfirmations => Set<PendingConfirmation>();
    public DbSet<PlanEvent> PlanEvents => Set<PlanEvent>();
    public DbSet<AdaptationEvent> AdaptationEvents => Set<AdaptationEvent>();
    public DbSet<DailyTipSet> DailyTipSets => Set<DailyTipSet>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply SnakeCaseEnumConverter to all Enum properties
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var type = property.ClrType;
                if (type.IsEnum)
                {
                    var converterType = typeof(SnakeCaseEnumConverter<>).MakeGenericType(type);
                    var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                    property.SetValueConverter(converter);
                }
                else
                {
                    var nullableType = Nullable.GetUnderlyingType(type);
                    if (nullableType != null && nullableType.IsEnum)
                    {
                        var converterType = typeof(SnakeCaseEnumConverter<>).MakeGenericType(nullableType);
                        var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                        property.SetValueConverter(converter);
                    }
                }
            }
        }

        // TrainingPlan relationships
        modelBuilder.Entity<TrainingPlan>()
            .HasMany(p => p.Weeks)
            .WithOne(w => w.Plan)
            .HasForeignKey(w => w.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrainingWeek>()
            .HasMany(w => w.Days)
            .WithOne(d => d.Week)
            .HasForeignKey(d => d.WeekId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict delete on TrainingDay to TrainingPlan connection to prevent multiple cascade paths in PostgreSQL
        modelBuilder.Entity<TrainingDay>()
            .HasOne(d => d.Plan)
            .WithMany()
            .HasForeignKey(d => d.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        modelBuilder.Entity<UserProfile>()
            .HasIndex(u => u.UserId)
            .IsUnique();

        modelBuilder.Entity<PlanTemplate>()
            .HasIndex(t => t.TemplateId)
            .IsUnique();

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var template1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var template2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var template3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

        modelBuilder.Entity<PlanTemplate>().HasData(
            new PlanTemplate
            {
                Id = template1Id,
                TemplateId = "habit_5k_beginner_3day_km_v1",
                Version = 1,
                GoalType = GoalType.Habit,
                GoalDistance = GoalDistance.FiveK,
                Level = RunningBackground.NewToRunning,
                DaysPerWeek = 3,
                Unit = DistanceUnit.Km,
                DataJson = "{\"templateId\":\"habit_5k_beginner_3day_km_v1\",\"version\":1,\"goalType\":\"habit\",\"goalDistance\":\"five_k\",\"level\":\"beginner\",\"daysPerWeek\":3,\"unit\":\"km\",\"weeks\":[{\"weekNumber\":1,\"weekType\":\"build\",\"days\":[{\"slotIndex\":1,\"dayType\":\"easy\",\"distanceKm\":2.0,\"durationMin\":20,\"intensity\":\"z2\"},{\"slotIndex\":2,\"dayType\":\"easy\",\"distanceKm\":2.5,\"durationMin\":25,\"intensity\":\"z2\"},{\"slotIndex\":3,\"dayType\":\"long_run\",\"distanceKm\":3.0,\"durationMin\":30,\"intensity\":\"z2\"}]}]}",
                CreatedAt = new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc)
            },
            new PlanTemplate
            {
                Id = template2Id,
                TemplateId = "habit_5k_beginner_4day_km_v1",
                Version = 1,
                GoalType = GoalType.Habit,
                GoalDistance = GoalDistance.FiveK,
                Level = RunningBackground.NewToRunning,
                DaysPerWeek = 4,
                Unit = DistanceUnit.Km,
                DataJson = "{\"templateId\":\"habit_5k_beginner_4day_km_v1\",\"version\":1,\"goalType\":\"habit\",\"goalDistance\":\"five_k\",\"level\":\"beginner\",\"daysPerWeek\":4,\"unit\":\"km\",\"weeks\":[{\"weekNumber\":1,\"weekType\":\"build\",\"days\":[{\"slotIndex\":1,\"dayType\":\"easy\",\"distanceKm\":2.0,\"durationMin\":20,\"intensity\":\"z2\"},{\"slotIndex\":2,\"dayType\":\"easy\",\"distanceKm\":2.0,\"durationMin\":20,\"intensity\":\"z2\"},{\"slotIndex\":3,\"dayType\":\"easy\",\"distanceKm\":2.5,\"durationMin\":25,\"intensity\":\"z2\"},{\"slotIndex\":4,\"dayType\":\"long_run\",\"distanceKm\":3.0,\"durationMin\":30,\"intensity\":\"z2\"}]}]}",
                CreatedAt = new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc)
            },
            new PlanTemplate
            {
                Id = template3Id,
                TemplateId = "race_5k_beginner_3day_km_v1",
                Version = 1,
                GoalType = GoalType.Race,
                GoalDistance = GoalDistance.FiveK,
                Level = RunningBackground.NewToRunning,
                DaysPerWeek = 3,
                Unit = DistanceUnit.Km,
                DataJson = "{\"templateId\":\"race_5k_beginner_3day_km_v1\",\"version\":1,\"goalType\":\"race\",\"goalDistance\":\"five_k\",\"level\":\"beginner\",\"daysPerWeek\":3,\"unit\":\"km\",\"weeks\":[{\"weekNumber\":1,\"weekType\":\"build\",\"days\":[{\"slotIndex\":1,\"dayType\":\"easy\",\"distanceKm\":2.5,\"durationMin\":25,\"intensity\":\"z2\"},{\"slotIndex\":2,\"dayType\":\"interval\",\"distanceKm\":3.0,\"durationMin\":30,\"intensity\":\"z4\"},{\"slotIndex\":3,\"dayType\":\"long_run\",\"distanceKm\":4.0,\"durationMin\":40,\"intensity\":\"z2\"}]}]}",
                CreatedAt = new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        modelBuilder.Entity<DailyTipSet>().HasData(
            new DailyTipSet
            {
                Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
                TipKey = "easy_run_tip_01",
                Title = "Keep it comfortable",
                Message = "Today is about showing up, not pushing hard.",
                WorkoutType = TrainingDayType.Easy,
                Language = "en",
                CreatedAt = new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc)
            },
            new DailyTipSet
            {
                Id = Guid.Parse("b2222222-2222-2222-2222-222222222222"),
                TipKey = "long_run_tip_01",
                Title = "Find your rhythm",
                Message = "A long run should feel conversational. Keep it steady.",
                WorkoutType = TrainingDayType.LongRun,
                Language = "en",
                CreatedAt = new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc)
            },
            new DailyTipSet
            {
                Id = Guid.Parse("c3333333-3333-3333-3333-333333333333"),
                TipKey = "rest_tip_01",
                Title = "Rest with intent",
                Message = "Recovery is when the magic happens. Let your body heal.",
                WorkoutType = TrainingDayType.Rest,
                Language = "en",
                CreatedAt = new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc)
            },
            new DailyTipSet
            {
                Id = Guid.Parse("d4444444-4444-4444-4444-444444444444"),
                TipKey = "missed_tip_01",
                Title = "No worries",
                Message = "Taking a day off is part of a sustainable plan.",
                WorkoutType = null,
                Language = "en",
                CreatedAt = new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc)
            },
            new DailyTipSet
            {
                Id = Guid.Parse("e5555555-5555-5555-5555-555555555555"),
                TipKey = "completed_tip_01",
                Title = "Well run!",
                Message = "Awesome job today. Remember to hydrate and stretch.",
                WorkoutType = null,
                Language = "en",
                CreatedAt = new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
