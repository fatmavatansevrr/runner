using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4L3LongHorizonPublicConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LongHorizonRollingPlanStateId",
                table: "TrainingPlans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleStrategy",
                table: "TrainingPlans",
                type: "text",
                nullable: false,
                defaultValue: "StaticComplete");

            migrationBuilder.AddColumn<int>(
                name: "ConfirmationContractVersion",
                table: "PlanPreviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutableWindowFingerprint",
                table: "PlanPreviews",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LongHorizonInitializationSnapshotJson",
                table: "PlanPreviews",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LongHorizonPlanStateId",
                table: "PlanPreviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedInputFingerprint",
                table: "PlanPreviews",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublicContractVersion",
                table: "PlanPreviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RollingPersistenceContractVersion",
                table: "PlanPreviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleStrategy",
                table: "PlanPreviews",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StructuralRoadmapFingerprint",
                table: "PlanPreviews",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlans_LongHorizonRollingPlanStateId",
                table: "TrainingPlans",
                column: "LongHorizonRollingPlanStateId",
                unique: true,
                filter: "\"LongHorizonRollingPlanStateId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingPlans_LongHorizonRollingPlanStates_LongHorizonRolli~",
                table: "TrainingPlans",
                column: "LongHorizonRollingPlanStateId",
                principalTable: "LongHorizonRollingPlanStates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingPlans_LongHorizonRollingPlanStates_LongHorizonRolli~",
                table: "TrainingPlans");

            migrationBuilder.DropIndex(
                name: "IX_TrainingPlans_LongHorizonRollingPlanStateId",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "LongHorizonRollingPlanStateId",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "ScheduleStrategy",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "ConfirmationContractVersion",
                table: "PlanPreviews");

            migrationBuilder.DropColumn(
                name: "ExecutableWindowFingerprint",
                table: "PlanPreviews");

            migrationBuilder.DropColumn(
                name: "LongHorizonInitializationSnapshotJson",
                table: "PlanPreviews");

            migrationBuilder.DropColumn(
                name: "LongHorizonPlanStateId",
                table: "PlanPreviews");

            migrationBuilder.DropColumn(
                name: "NormalizedInputFingerprint",
                table: "PlanPreviews");

            migrationBuilder.DropColumn(
                name: "PublicContractVersion",
                table: "PlanPreviews");

            migrationBuilder.DropColumn(
                name: "RollingPersistenceContractVersion",
                table: "PlanPreviews");

            migrationBuilder.DropColumn(
                name: "ScheduleStrategy",
                table: "PlanPreviews");

            migrationBuilder.DropColumn(
                name: "StructuralRoadmapFingerprint",
                table: "PlanPreviews");
        }
    }
}
