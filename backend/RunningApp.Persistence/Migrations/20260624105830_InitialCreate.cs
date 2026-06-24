using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdaptationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    TriggerSource = table.Column<string>(type: "text", nullable: false),
                    TriggeredByTrainingDayId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    AffectedDaysJson = table.Column<string>(type: "text", nullable: true),
                    ExplanationKey = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DismissedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptationEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyTipSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TipKey = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    WorkoutType = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<string>(type: "text", nullable: true),
                    GoalType = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyTipSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ReminderStyle = table.Column<string>(type: "text", nullable: false),
                    WorkoutRemindersEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EveningReminderEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotTodayDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TriggerSource = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    ResultingStatus = table.Column<string>(type: "text", nullable: false),
                    DecisionPayloadJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotTodayDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PendingConfirmations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingConfirmations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingDayId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanPreviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TemplateId = table.Column<string>(type: "text", nullable: false),
                    RequestPayloadJson = table.Column<string>(type: "text", nullable: false),
                    PreviewPayloadJson = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanPreviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    GoalType = table.Column<string>(type: "text", nullable: false),
                    GoalDistance = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<string>(type: "text", nullable: false),
                    DaysPerWeek = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    DataJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeprecatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TemplateId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    GoalType = table.Column<string>(type: "text", nullable: false),
                    GoalDistance = table.Column<string>(type: "text", nullable: false),
                    GoalDistanceKm = table.Column<double>(type: "double precision", nullable: true),
                    Level = table.Column<string>(type: "text", nullable: false),
                    DaysPerWeek = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    RaceName = table.Column<string>(type: "text", nullable: true),
                    RaceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TargetFinishTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EstimatedEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    RunningBackground = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    Result = table.Column<string>(type: "text", nullable: false),
                    ActualDistanceKm = table.Column<double>(type: "double precision", nullable: true),
                    ActualDurationMin = table.Column<int>(type: "integer", nullable: true),
                    UserNote = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingWeeks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false),
                    WeekType = table.Column<string>(type: "text", nullable: false),
                    PlannedVolumeKm = table.Column<double>(type: "double precision", nullable: false),
                    ActualVolumeKm = table.Column<double>(type: "double precision", nullable: false),
                    IsRecoveryWeek = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingWeeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingWeeks_TrainingPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "TrainingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DayType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PlannedDistanceKm = table.Column<double>(type: "double precision", nullable: false),
                    PlannedDurationMin = table.Column<int>(type: "integer", nullable: false),
                    PlannedPaceMinKm = table.Column<double>(type: "double precision", nullable: true),
                    Intensity = table.Column<string>(type: "text", nullable: true),
                    ActualDistanceKm = table.Column<double>(type: "double precision", nullable: true),
                    ActualDurationMin = table.Column<int>(type: "integer", nullable: true),
                    IsLongRun = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OriginalType = table.Column<string>(type: "text", nullable: true),
                    CanMarkComplete = table.Column<bool>(type: "boolean", nullable: false),
                    CanMarkNotToday = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingDays_TrainingPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "TrainingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingDays_TrainingWeeks_WeekId",
                        column: x => x.WeekId,
                        principalTable: "TrainingWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DailyTipSets",
                columns: new[] { "Id", "CreatedAt", "GoalType", "Language", "Level", "Message", "TipKey", "Title", "WorkoutType" },
                values: new object[,]
                {
                    { new Guid("a1111111-1111-1111-1111-111111111111"), new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Utc), null, "en", null, "Today is about showing up, not pushing hard.", "easy_run_tip_01", "Keep it comfortable", "easy" },
                    { new Guid("b2222222-2222-2222-2222-222222222222"), new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Utc), null, "en", null, "A long run should feel conversational. Keep it steady.", "long_run_tip_01", "Find your rhythm", "long_run" },
                    { new Guid("c3333333-3333-3333-3333-333333333333"), new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Utc), null, "en", null, "Recovery is when the magic happens. Let your body heal.", "rest_tip_01", "Rest with intent", "rest" },
                    { new Guid("d4444444-4444-4444-4444-444444444444"), new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Utc), null, "en", null, "Taking a day off is part of a sustainable plan.", "missed_tip_01", "No worries", null },
                    { new Guid("e5555555-5555-5555-5555-555555555555"), new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Utc), null, "en", null, "Awesome job today. Remember to hydrate and stretch.", "completed_tip_01", "Well run!", null }
                });

            migrationBuilder.InsertData(
                table: "PlanTemplates",
                columns: new[] { "Id", "CreatedAt", "DataJson", "DaysPerWeek", "DeprecatedAt", "GoalDistance", "GoalType", "Level", "TemplateId", "Unit", "Version" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Utc), "{\"templateId\":\"habit_5k_beginner_3day_km_v1\",\"version\":1,\"goalType\":\"habit\",\"goalDistance\":\"five_k\",\"level\":\"beginner\",\"daysPerWeek\":3,\"unit\":\"km\",\"weeks\":[{\"weekNumber\":1,\"weekType\":\"build\",\"days\":[{\"slotIndex\":1,\"dayType\":\"easy\",\"distanceKm\":2.0,\"durationMin\":20,\"intensity\":\"z2\"},{\"slotIndex\":2,\"dayType\":\"easy\",\"distanceKm\":2.5,\"durationMin\":25,\"intensity\":\"z2\"},{\"slotIndex\":3,\"dayType\":\"long_run\",\"distanceKm\":3.0,\"durationMin\":30,\"intensity\":\"z2\"}]}]}", 3, null, "five_k", "habit", "new_to_running", "habit_5k_beginner_3day_km_v1", "km", 1 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Utc), "{\"templateId\":\"habit_5k_beginner_4day_km_v1\",\"version\":1,\"goalType\":\"habit\",\"goalDistance\":\"five_k\",\"level\":\"beginner\",\"daysPerWeek\":4,\"unit\":\"km\",\"weeks\":[{\"weekNumber\":1,\"weekType\":\"build\",\"days\":[{\"slotIndex\":1,\"dayType\":\"easy\",\"distanceKm\":2.0,\"durationMin\":20,\"intensity\":\"z2\"},{\"slotIndex\":2,\"dayType\":\"easy\",\"distanceKm\":2.0,\"durationMin\":20,\"intensity\":\"z2\"},{\"slotIndex\":3,\"dayType\":\"easy\",\"distanceKm\":2.5,\"durationMin\":25,\"intensity\":\"z2\"},{\"slotIndex\":4,\"dayType\":\"long_run\",\"distanceKm\":3.0,\"durationMin\":30,\"intensity\":\"z2\"}]}]}", 4, null, "five_k", "habit", "new_to_running", "habit_5k_beginner_4day_km_v1", "km", 1 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Utc), "{\"templateId\":\"race_5k_beginner_3day_km_v1\",\"version\":1,\"goalType\":\"race\",\"goalDistance\":\"five_k\",\"level\":\"beginner\",\"daysPerWeek\":3,\"unit\":\"km\",\"weeks\":[{\"weekNumber\":1,\"weekType\":\"build\",\"days\":[{\"slotIndex\":1,\"dayType\":\"easy\",\"distanceKm\":2.5,\"durationMin\":25,\"intensity\":\"z2\"},{\"slotIndex\":2,\"dayType\":\"interval\",\"distanceKm\":3.0,\"durationMin\":30,\"intensity\":\"z4\"},{\"slotIndex\":3,\"dayType\":\"long_run\",\"distanceKm\":4.0,\"durationMin\":40,\"intensity\":\"z2\"}]}]}", 3, null, "five_k", "race", "new_to_running", "race_5k_beginner_3day_km_v1", "km", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanTemplates_TemplateId",
                table: "PlanTemplates",
                column: "TemplateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDays_PlanId",
                table: "TrainingDays",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDays_WeekId",
                table: "TrainingDays",
                column: "WeekId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingWeeks_PlanId",
                table: "TrainingWeeks",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdaptationEvents");

            migrationBuilder.DropTable(
                name: "DailyTipSets");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "NotTodayDecisions");

            migrationBuilder.DropTable(
                name: "PendingConfirmations");

            migrationBuilder.DropTable(
                name: "PlanEvents");

            migrationBuilder.DropTable(
                name: "PlanPreviews");

            migrationBuilder.DropTable(
                name: "PlanTemplates");

            migrationBuilder.DropTable(
                name: "TrainingDays");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "WorkoutLogs");

            migrationBuilder.DropTable(
                name: "TrainingWeeks");

            migrationBuilder.DropTable(
                name: "TrainingPlans");
        }
    }
}
