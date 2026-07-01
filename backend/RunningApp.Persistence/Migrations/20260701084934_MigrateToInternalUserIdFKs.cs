using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToInternalUserIdFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutLogs_UserId_CreatedAt",
                table: "WorkoutLogs");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_TrainingPlans_UserId_ActiveOnly",
                table: "TrainingPlans");

            migrationBuilder.DropIndex(
                name: "IX_TrainingPlans_UserId_Status",
                table: "TrainingPlans");

            migrationBuilder.DropIndex(
                name: "IX_PlanPreviews_UserId",
                table: "PlanPreviews");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "WorkoutLogs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PlanPreviews");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PlanEvents");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PendingConfirmations");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "NotTodayDecisions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AdaptationEvents");

            migrationBuilder.AddColumn<Guid>(
                name: "InternalUserId",
                table: "WorkoutLogs",
                type: "uuid",
                nullable: true);

            // Remove any UserProfile rows where InternalUserId was never populated.
            // (dev-only concern — prod would need a proper backfill before this point)
            migrationBuilder.Sql(@"DELETE FROM ""UserProfiles"" WHERE ""InternalUserId"" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "InternalUserId",
                table: "UserProfiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InternalUserId",
                table: "TrainingPlans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InternalUserId",
                table: "PlanPreviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InternalUserId",
                table: "PlanEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InternalUserId",
                table: "PendingConfirmations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InternalUserId",
                table: "NotTodayDecisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InternalUserId",
                table: "NotificationPreferences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InternalUserId",
                table: "AdaptationEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_InternalUserId_CreatedAt",
                table: "WorkoutLogs",
                columns: new[] { "InternalUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlans_InternalUserId_ActiveOnly",
                table: "TrainingPlans",
                column: "InternalUserId",
                unique: true,
                filter: "\"Status\" = 'active'");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlans_InternalUserId_Status",
                table: "TrainingPlans",
                columns: new[] { "InternalUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanPreviews_InternalUserId",
                table: "PlanPreviews",
                column: "InternalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanEvents_InternalUserId",
                table: "PlanEvents",
                column: "InternalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingConfirmations_InternalUserId",
                table: "PendingConfirmations",
                column: "InternalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotTodayDecisions_InternalUserId",
                table: "NotTodayDecisions",
                column: "InternalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_InternalUserId",
                table: "NotificationPreferences",
                column: "InternalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptationEvents_InternalUserId",
                table: "AdaptationEvents",
                column: "InternalUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdaptationEvents_Users_InternalUserId",
                table: "AdaptationEvents",
                column: "InternalUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationPreferences_Users_InternalUserId",
                table: "NotificationPreferences",
                column: "InternalUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotTodayDecisions_Users_InternalUserId",
                table: "NotTodayDecisions",
                column: "InternalUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingConfirmations_Users_InternalUserId",
                table: "PendingConfirmations",
                column: "InternalUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanEvents_Users_InternalUserId",
                table: "PlanEvents",
                column: "InternalUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanPreviews_Users_InternalUserId",
                table: "PlanPreviews",
                column: "InternalUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingPlans_Users_InternalUserId",
                table: "TrainingPlans",
                column: "InternalUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutLogs_Users_InternalUserId",
                table: "WorkoutLogs",
                column: "InternalUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdaptationEvents_Users_InternalUserId",
                table: "AdaptationEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationPreferences_Users_InternalUserId",
                table: "NotificationPreferences");

            migrationBuilder.DropForeignKey(
                name: "FK_NotTodayDecisions_Users_InternalUserId",
                table: "NotTodayDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_PendingConfirmations_Users_InternalUserId",
                table: "PendingConfirmations");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanEvents_Users_InternalUserId",
                table: "PlanEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanPreviews_Users_InternalUserId",
                table: "PlanPreviews");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingPlans_Users_InternalUserId",
                table: "TrainingPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutLogs_Users_InternalUserId",
                table: "WorkoutLogs");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutLogs_InternalUserId_CreatedAt",
                table: "WorkoutLogs");

            migrationBuilder.DropIndex(
                name: "IX_TrainingPlans_InternalUserId_ActiveOnly",
                table: "TrainingPlans");

            migrationBuilder.DropIndex(
                name: "IX_TrainingPlans_InternalUserId_Status",
                table: "TrainingPlans");

            migrationBuilder.DropIndex(
                name: "IX_PlanPreviews_InternalUserId",
                table: "PlanPreviews");

            migrationBuilder.DropIndex(
                name: "IX_PlanEvents_InternalUserId",
                table: "PlanEvents");

            migrationBuilder.DropIndex(
                name: "IX_PendingConfirmations_InternalUserId",
                table: "PendingConfirmations");

            migrationBuilder.DropIndex(
                name: "IX_NotTodayDecisions_InternalUserId",
                table: "NotTodayDecisions");

            migrationBuilder.DropIndex(
                name: "IX_NotificationPreferences_InternalUserId",
                table: "NotificationPreferences");

            migrationBuilder.DropIndex(
                name: "IX_AdaptationEvents_InternalUserId",
                table: "AdaptationEvents");

            migrationBuilder.DropColumn(
                name: "InternalUserId",
                table: "WorkoutLogs");

            migrationBuilder.DropColumn(
                name: "InternalUserId",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "InternalUserId",
                table: "PlanPreviews");

            migrationBuilder.DropColumn(
                name: "InternalUserId",
                table: "PlanEvents");

            migrationBuilder.DropColumn(
                name: "InternalUserId",
                table: "PendingConfirmations");

            migrationBuilder.DropColumn(
                name: "InternalUserId",
                table: "NotTodayDecisions");

            migrationBuilder.DropColumn(
                name: "InternalUserId",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "InternalUserId",
                table: "AdaptationEvents");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "WorkoutLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "InternalUserId",
                table: "UserProfiles",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "UserProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "TrainingPlans",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "PlanPreviews",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "PlanEvents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "PendingConfirmations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "NotTodayDecisions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "NotificationPreferences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "AdaptationEvents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_UserId_CreatedAt",
                table: "WorkoutLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlans_UserId_ActiveOnly",
                table: "TrainingPlans",
                column: "UserId",
                unique: true,
                filter: "\"Status\" = 'active'");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlans_UserId_Status",
                table: "TrainingPlans",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanPreviews_UserId",
                table: "PlanPreviews",
                column: "UserId");
        }
    }
}
