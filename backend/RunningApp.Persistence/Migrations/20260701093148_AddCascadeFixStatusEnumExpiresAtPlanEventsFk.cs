using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCascadeFixStatusEnumExpiresAtPlanEventsFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingDays_TrainingPlans_PlanId",
                table: "TrainingDays");

            // Rename legacy 'pending' value to 'pending_confirmation' before adding CHECK constraint.
            migrationBuilder.Sql(@"UPDATE ""TrainingDays"" SET ""Status"" = 'pending_confirmation' WHERE ""Status"" = 'pending';");

            migrationBuilder.Sql(@"ALTER TABLE ""TrainingDays"" ADD CONSTRAINT ""CK_TrainingDays_Status""
                CHECK (""Status"" IN ('planned','completed','missed','skipped','pending_confirmation','rescheduled','soft_missed'));");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "PendingConfirmations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanEvents_PlanId",
                table: "PlanEvents",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanEvents_TrainingPlans_PlanId",
                table: "PlanEvents",
                column: "PlanId",
                principalTable: "TrainingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingDays_TrainingPlans_PlanId",
                table: "TrainingDays",
                column: "PlanId",
                principalTable: "TrainingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanEvents_TrainingPlans_PlanId",
                table: "PlanEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingDays_TrainingPlans_PlanId",
                table: "TrainingDays");

            migrationBuilder.DropIndex(
                name: "IX_PlanEvents_PlanId",
                table: "PlanEvents");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "PendingConfirmations");

            migrationBuilder.Sql(@"ALTER TABLE ""TrainingDays"" DROP CONSTRAINT IF EXISTS ""CK_TrainingDays_Status"";");
            migrationBuilder.Sql(@"UPDATE ""TrainingDays"" SET ""Status"" = 'pending' WHERE ""Status"" = 'pending_confirmation';");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingDays_TrainingPlans_PlanId",
                table: "TrainingDays",
                column: "PlanId",
                principalTable: "TrainingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
