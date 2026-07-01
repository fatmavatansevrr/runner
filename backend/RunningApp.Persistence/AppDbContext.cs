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

    public DbSet<User> Users => Set<User>();
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

        // ── Users ────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.ExternalAuthProvider, u.ExternalUserId })
            .IsUnique()
            .HasDatabaseName("IX_Users_Provider_ExternalId");

        // ── UserProfile ──────────────────────────────────────────────────────
        modelBuilder.Entity<UserProfile>()
            .HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(p => p.InternalUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // ── TrainingPlan relationships ────────────────────────────────────────
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

        modelBuilder.Entity<TrainingDay>()
            .HasOne(d => d.Plan)
            .WithMany()
            .HasForeignKey(d => d.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Self-reference for adaptation tracking (AdaptedFromId).
        // SET NULL on delete so removing the source day doesn't cascade-delete derived days.
        modelBuilder.Entity<TrainingDay>()
            .HasOne(d => d.AdaptedFrom)
            .WithMany()
            .HasForeignKey(d => d.AdaptedFromId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // ── WorkoutLog FK constraints ────────────────────────────────────────
        modelBuilder.Entity<WorkoutLog>()
            .HasOne<TrainingDay>()
            .WithMany()
            .HasForeignKey(w => w.TrainingDayId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkoutLog>()
            .HasOne<TrainingPlan>()
            .WithMany()
            .HasForeignKey(w => w.PlanId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // ── InternalUserId FK constraints (→ Users.Id) ──────────────────────
        modelBuilder.Entity<TrainingPlan>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.InternalUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkoutLog>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(w => w.InternalUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NotTodayDecision>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.InternalUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PendingConfirmation>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.InternalUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdaptationEvent>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.InternalUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlanPreview>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.InternalUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlanEvent>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.InternalUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlanEvent>()
            .HasOne<TrainingPlan>()
            .WithMany()
            .HasForeignKey(p => p.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NotificationPreference>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.InternalUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Shadow FK constraints (no navigation properties) ─────────────────
        modelBuilder.Entity<NotTodayDecision>()
            .HasOne<TrainingPlan>()
            .WithMany()
            .HasForeignKey(n => n.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NotTodayDecision>()
            .HasOne<TrainingDay>()
            .WithMany()
            .HasForeignKey(n => n.TrainingDayId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PendingConfirmation>()
            .HasOne<TrainingPlan>()
            .WithMany()
            .HasForeignKey(p => p.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PendingConfirmation>()
            .HasOne<TrainingDay>()
            .WithMany()
            .HasForeignKey(p => p.TrainingDayId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdaptationEvent>()
            .HasOne<TrainingPlan>()
            .WithMany()
            .HasForeignKey(a => a.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdaptationEvent>()
            .HasOne<TrainingDay>()
            .WithMany()
            .HasForeignKey(a => a.TriggeredByTrainingDayId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // ── JSONB column types ───────────────────────────────────────────────
        modelBuilder.Entity<PlanTemplate>()
            .Property(t => t.DataJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<PlanPreview>()
            .Property(p => p.RequestPayloadJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<PlanPreview>()
            .Property(p => p.PreviewPayloadJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<AdaptationEvent>()
            .Property(a => a.AffectedDaysJson)
            .HasColumnType("jsonb");

        // ── Indexes ───────────────────────────────────────────────────────────
        modelBuilder.Entity<PlanTemplate>()
            .HasIndex(t => t.TemplateId)
            .IsUnique();

        // Enforce "one active plan per user" at the database level.
        // NULL InternalUserId values are excluded from the unique constraint
        // (the filter already narrows to active rows; NULLs are ignored by PostgreSQL unique indexes).
        modelBuilder.Entity<TrainingPlan>()
            .HasIndex(p => p.InternalUserId)
            .IsUnique()
            .HasDatabaseName("IX_TrainingPlans_InternalUserId_ActiveOnly")
            .HasFilter("\"Status\" = 'active'");

        modelBuilder.Entity<TrainingPlan>()
            .HasIndex(p => new { p.InternalUserId, p.Status })
            .HasDatabaseName("IX_TrainingPlans_InternalUserId_Status");

        modelBuilder.Entity<TrainingDay>()
            .HasIndex(d => new { d.PlanId, d.Date });

        modelBuilder.Entity<TrainingDay>()
            .HasIndex(d => new { d.WeekId, d.Date })
            .HasDatabaseName("IX_TrainingDays_WeekId_Date");

        modelBuilder.Entity<TrainingDay>()
            .HasIndex(d => d.Status)
            .HasDatabaseName("IX_TrainingDays_Status");

        modelBuilder.Entity<TrainingWeek>()
            .HasIndex(w => new { w.PlanId, w.WeekNumber });

        modelBuilder.Entity<PlanPreview>()
            .HasIndex(p => p.InternalUserId)
            .HasDatabaseName("IX_PlanPreviews_InternalUserId");

        modelBuilder.Entity<WorkoutLog>()
            .HasIndex(w => w.TrainingDayId);

        modelBuilder.Entity<WorkoutLog>()
            .HasIndex(w => new { w.InternalUserId, w.CreatedAt })
            .HasDatabaseName("IX_WorkoutLogs_InternalUserId_CreatedAt");

        modelBuilder.Entity<PendingConfirmation>()
            .HasIndex(p => p.TrainingDayId);

        modelBuilder.Entity<AdaptationEvent>()
            .HasIndex(a => new { a.PlanId, a.CreatedAt })
            .HasDatabaseName("IX_AdaptationEvents_PlanId_CreatedAt");

        modelBuilder.Entity<PlanEvent>()
            .HasIndex(p => p.PlanId)
            .HasDatabaseName("IX_PlanEvents_PlanId");

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
