using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanCatalogProvenanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogPhaseKey",
                table: "TrainingWeeks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanonicalDistanceFamily",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogCandidateKey",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogCandidateStatusAtGenerationTime",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogCandidateVersion",
                table: "TrainingPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogLayoutKey",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogLayoutVersion",
                table: "TrainingPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogLevelModifierKey",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogLevelModifierVersion",
                table: "TrainingPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogPeakVolumeBandPolicyKey",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogPeakVolumeBandPolicyVersion",
                table: "TrainingPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogProgressionModifierKey",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogProgressionModifierVersion",
                table: "TrainingPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogRulePackKey",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogRulePackVersion",
                table: "TrainingPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogRuntimeConditionRegistryKey",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogRuntimeConditionRegistryVersion",
                table: "TrainingPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogTemplateKey",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogTemplateVersion",
                table: "TrainingPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogWorkoutProgressionKey",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogWorkoutProgressionVersion",
                table: "TrainingPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RequestedTargetDistanceKm",
                table: "TrainingPlans",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogIntensityKey",
                table: "TrainingDays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogSlotRole",
                table: "TrainingDays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogStageKey",
                table: "TrainingDays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogWorkoutFamily",
                table: "TrainingDays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogWorkoutKey",
                table: "TrainingDays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogWorkoutVersion",
                table: "TrainingDays",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CatalogPhaseKey",
                table: "TrainingWeeks");

            migrationBuilder.DropColumn(
                name: "CanonicalDistanceFamily",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogCandidateKey",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogCandidateStatusAtGenerationTime",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogCandidateVersion",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogLayoutKey",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogLayoutVersion",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogLevelModifierKey",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogLevelModifierVersion",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogPeakVolumeBandPolicyKey",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogPeakVolumeBandPolicyVersion",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogProgressionModifierKey",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogProgressionModifierVersion",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogRulePackKey",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogRulePackVersion",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogRuntimeConditionRegistryKey",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogRuntimeConditionRegistryVersion",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogTemplateKey",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogTemplateVersion",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogWorkoutProgressionKey",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogWorkoutProgressionVersion",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "RequestedTargetDistanceKm",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "CatalogIntensityKey",
                table: "TrainingDays");

            migrationBuilder.DropColumn(
                name: "CatalogSlotRole",
                table: "TrainingDays");

            migrationBuilder.DropColumn(
                name: "CatalogStageKey",
                table: "TrainingDays");

            migrationBuilder.DropColumn(
                name: "CatalogWorkoutFamily",
                table: "TrainingDays");

            migrationBuilder.DropColumn(
                name: "CatalogWorkoutKey",
                table: "TrainingDays");

            migrationBuilder.DropColumn(
                name: "CatalogWorkoutVersion",
                table: "TrainingDays");
        }
    }
}
