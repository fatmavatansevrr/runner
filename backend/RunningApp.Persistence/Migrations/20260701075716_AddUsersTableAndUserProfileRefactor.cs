using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersTableAndUserProfileRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingDays_WeekId",
                table: "TrainingDays");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlanId",
                table: "WorkoutLogs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "InternalUserId",
                table: "UserProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InjuryNotes",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredDays",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PreferredPace",
                table: "TrainingPlans",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TrainingPlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeeklyAvailability",
                table: "TrainingPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ActualPaceMinKm",
                table: "TrainingDays",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AdaptedFromId",
                table: "TrainingDays",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlanVersion",
                table: "TrainingDays",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "TrainingDays",
                type: "text",
                nullable: true);

            // TEXT → JSONB: PostgreSQL requires USING clause for this cast.
            // EF Core's AlterColumn does not emit USING automatically, so we
            // use explicit SQL for all four JSON columns.
            migrationBuilder.Sql(
                @"ALTER TABLE ""PlanTemplates"" ALTER COLUMN ""DataJson"" TYPE jsonb USING ""DataJson""::jsonb;");

            migrationBuilder.Sql(
                @"ALTER TABLE ""PlanPreviews"" ALTER COLUMN ""RequestPayloadJson"" TYPE jsonb USING ""RequestPayloadJson""::jsonb;");

            migrationBuilder.Sql(
                @"ALTER TABLE ""PlanPreviews"" ALTER COLUMN ""PreviewPayloadJson"" TYPE jsonb USING ""PreviewPayloadJson""::jsonb;");

            migrationBuilder.Sql(
                @"ALTER TABLE ""AdaptationEvents"" ALTER COLUMN ""AffectedDaysJson"" TYPE jsonb USING ""AffectedDaysJson""::jsonb;");

            migrationBuilder.AlterColumn<string>(
                name: "TemplateId",
                table: "PlanPreviews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalAuthProvider = table.Column<string>(type: "text", nullable: false),
                    ExternalUserId = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    EmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_PlanId",
                table: "WorkoutLogs",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_UserId_CreatedAt",
                table: "WorkoutLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_InternalUserId",
                table: "UserProfiles",
                column: "InternalUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDays_AdaptedFromId",
                table: "TrainingDays",
                column: "AdaptedFromId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDays_Status",
                table: "TrainingDays",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDays_WeekId_Date",
                table: "TrainingDays",
                columns: new[] { "WeekId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_PendingConfirmations_PlanId",
                table: "PendingConfirmations",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_NotTodayDecisions_PlanId",
                table: "NotTodayDecisions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_NotTodayDecisions_TrainingDayId",
                table: "NotTodayDecisions",
                column: "TrainingDayId");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptationEvents_PlanId_CreatedAt",
                table: "AdaptationEvents",
                columns: new[] { "PlanId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Provider_ExternalId",
                table: "Users",
                columns: new[] { "ExternalAuthProvider", "ExternalUserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AdaptationEvents_TrainingPlans_PlanId",
                table: "AdaptationEvents",
                column: "PlanId",
                principalTable: "TrainingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotTodayDecisions_TrainingDays_TrainingDayId",
                table: "NotTodayDecisions",
                column: "TrainingDayId",
                principalTable: "TrainingDays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotTodayDecisions_TrainingPlans_PlanId",
                table: "NotTodayDecisions",
                column: "PlanId",
                principalTable: "TrainingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingConfirmations_TrainingDays_TrainingDayId",
                table: "PendingConfirmations",
                column: "TrainingDayId",
                principalTable: "TrainingDays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingConfirmations_TrainingPlans_PlanId",
                table: "PendingConfirmations",
                column: "PlanId",
                principalTable: "TrainingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingDays_TrainingDays_AdaptedFromId",
                table: "TrainingDays",
                column: "AdaptedFromId",
                principalTable: "TrainingDays",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_Users_InternalUserId",
                table: "UserProfiles",
                column: "InternalUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutLogs_TrainingDays_TrainingDayId",
                table: "WorkoutLogs",
                column: "TrainingDayId",
                principalTable: "TrainingDays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutLogs_TrainingPlans_PlanId",
                table: "WorkoutLogs",
                column: "PlanId",
                principalTable: "TrainingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdaptationEvents_TrainingPlans_PlanId",
                table: "AdaptationEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_NotTodayDecisions_TrainingDays_TrainingDayId",
                table: "NotTodayDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_NotTodayDecisions_TrainingPlans_PlanId",
                table: "NotTodayDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_PendingConfirmations_TrainingDays_TrainingDayId",
                table: "PendingConfirmations");

            migrationBuilder.DropForeignKey(
                name: "FK_PendingConfirmations_TrainingPlans_PlanId",
                table: "PendingConfirmations");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingDays_TrainingDays_AdaptedFromId",
                table: "TrainingDays");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_Users_InternalUserId",
                table: "UserProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutLogs_TrainingDays_TrainingDayId",
                table: "WorkoutLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutLogs_TrainingPlans_PlanId",
                table: "WorkoutLogs");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutLogs_PlanId",
                table: "WorkoutLogs");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutLogs_UserId_CreatedAt",
                table: "WorkoutLogs");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_InternalUserId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_TrainingDays_AdaptedFromId",
                table: "TrainingDays");

            migrationBuilder.DropIndex(
                name: "IX_TrainingDays_Status",
                table: "TrainingDays");

            migrationBuilder.DropIndex(
                name: "IX_TrainingDays_WeekId_Date",
                table: "TrainingDays");

            migrationBuilder.DropIndex(
                name: "IX_PendingConfirmations_PlanId",
                table: "PendingConfirmations");

            migrationBuilder.DropIndex(
                name: "IX_NotTodayDecisions_PlanId",
                table: "NotTodayDecisions");

            migrationBuilder.DropIndex(
                name: "IX_NotTodayDecisions_TrainingDayId",
                table: "NotTodayDecisions");

            migrationBuilder.DropIndex(
                name: "IX_AdaptationEvents_PlanId_CreatedAt",
                table: "AdaptationEvents");

            migrationBuilder.DropColumn(
                name: "InternalUserId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "InjuryNotes",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "PreferredDays",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "PreferredPace",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "WeeklyAvailability",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "ActualPaceMinKm",
                table: "TrainingDays");

            migrationBuilder.DropColumn(
                name: "AdaptedFromId",
                table: "TrainingDays");

            migrationBuilder.DropColumn(
                name: "PlanVersion",
                table: "TrainingDays");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "TrainingDays");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlanId",
                table: "WorkoutLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.Sql(
                @"ALTER TABLE ""PlanTemplates"" ALTER COLUMN ""DataJson"" TYPE text USING ""DataJson""::text;");

            migrationBuilder.Sql(
                @"ALTER TABLE ""PlanPreviews"" ALTER COLUMN ""RequestPayloadJson"" TYPE text USING ""RequestPayloadJson""::text;");

            migrationBuilder.Sql(
                @"ALTER TABLE ""PlanPreviews"" ALTER COLUMN ""PreviewPayloadJson"" TYPE text USING ""PreviewPayloadJson""::text;");

            migrationBuilder.Sql(
                @"ALTER TABLE ""AdaptationEvents"" ALTER COLUMN ""AffectedDaysJson"" TYPE text USING ""AffectedDaysJson""::text;");

            migrationBuilder.AlterColumn<string>(
                name: "TemplateId",
                table: "PlanPreviews",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDays_WeekId",
                table: "TrainingDays",
                column: "WeekId");
        }
    }
}
