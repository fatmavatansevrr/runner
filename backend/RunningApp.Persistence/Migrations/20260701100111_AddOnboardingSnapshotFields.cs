using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingSnapshotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomDurationWeeks",
                table: "TrainingPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomGoalType",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomTargetTimeSeconds",
                table: "TrainingPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HabitPlanType",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LongRunDay",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(@"ALTER TABLE ""TrainingPlans""
                ADD CONSTRAINT ""CK_TrainingPlans_HabitPlanType""
                CHECK (""HabitPlanType"" IS NULL OR ""HabitPlanType"" IN (
                    'five_k_comfort','ten_k_nonstop','five_k_under_30','custom'
                ));");

            migrationBuilder.Sql(@"ALTER TABLE ""TrainingPlans""
                ADD CONSTRAINT ""CK_TrainingPlans_CustomGoalType""
                CHECK (""CustomGoalType"" IS NULL OR ""CustomGoalType"" IN (
                    'comfort','steady_pace','finish_under_time'
                ));");

            migrationBuilder.Sql(@"ALTER TABLE ""TrainingPlans""
                ADD CONSTRAINT ""CK_TrainingPlans_LongRunDay""
                CHECK (""LongRunDay"" IS NULL OR ""LongRunDay"" IN (
                    'Mon','Tue','Wed','Thu','Fri','Sat','Sun'
                ));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""TrainingPlans""
                DROP CONSTRAINT IF EXISTS ""CK_TrainingPlans_HabitPlanType"",
                DROP CONSTRAINT IF EXISTS ""CK_TrainingPlans_CustomGoalType"",
                DROP CONSTRAINT IF EXISTS ""CK_TrainingPlans_LongRunDay"";");

            migrationBuilder.DropColumn(
                name: "CustomDurationWeeks",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CustomGoalType",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CustomTargetTimeSeconds",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "HabitPlanType",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "LongRunDay",
                table: "TrainingPlans");
        }
    }
}
