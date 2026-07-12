using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4E2_CatalogConfirmationState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmedPlanId",
                table: "PlanPreviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInvalidated",
                table: "PlanPreviews",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanPreviews_ConfirmedPlanId",
                table: "PlanPreviews",
                column: "ConfirmedPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanPreviews_TrainingPlans_ConfirmedPlanId",
                table: "PlanPreviews",
                column: "ConfirmedPlanId",
                principalTable: "TrainingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanPreviews_TrainingPlans_ConfirmedPlanId",
                table: "PlanPreviews");

            migrationBuilder.DropIndex(
                name: "IX_PlanPreviews_ConfirmedPlanId",
                table: "PlanPreviews");

            migrationBuilder.DropColumn(
                name: "ConfirmedPlanId",
                table: "PlanPreviews");

            migrationBuilder.DropColumn(
                name: "IsInvalidated",
                table: "PlanPreviews");
        }
    }
}
