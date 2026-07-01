using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingWeeks_PlanId",
                table: "TrainingWeeks");

            migrationBuilder.DropIndex(
                name: "IX_TrainingDays_PlanId",
                table: "TrainingDays");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_TrainingDayId",
                table: "WorkoutLogs",
                column: "TrainingDayId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingWeeks_PlanId_WeekNumber",
                table: "TrainingWeeks",
                columns: new[] { "PlanId", "WeekNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlans_UserId_Status",
                table: "TrainingPlans",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDays_PlanId_Date",
                table: "TrainingDays",
                columns: new[] { "PlanId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanPreviews_UserId",
                table: "PlanPreviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingConfirmations_TrainingDayId",
                table: "PendingConfirmations",
                column: "TrainingDayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutLogs_TrainingDayId",
                table: "WorkoutLogs");

            migrationBuilder.DropIndex(
                name: "IX_TrainingWeeks_PlanId_WeekNumber",
                table: "TrainingWeeks");

            migrationBuilder.DropIndex(
                name: "IX_TrainingPlans_UserId_Status",
                table: "TrainingPlans");

            migrationBuilder.DropIndex(
                name: "IX_TrainingDays_PlanId_Date",
                table: "TrainingDays");

            migrationBuilder.DropIndex(
                name: "IX_PlanPreviews_UserId",
                table: "PlanPreviews");

            migrationBuilder.DropIndex(
                name: "IX_PendingConfirmations_TrainingDayId",
                table: "PendingConfirmations");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingWeeks_PlanId",
                table: "TrainingWeeks",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDays_PlanId",
                table: "TrainingDays",
                column: "PlanId");
        }
    }
}
