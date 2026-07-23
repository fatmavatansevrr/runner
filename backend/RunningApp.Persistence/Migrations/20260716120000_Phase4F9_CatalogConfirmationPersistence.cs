using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4F9_CatalogConfirmationPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CatalogConfirmedAtUtc",
                table: "TrainingPlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogDependencyVersionsJson",
                table: "TrainingPlans",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogMaterializerVersion",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogPreviewContentHash",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenerationSource",
                table: "TrainingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourcePreviewId",
                table: "TrainingPlans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogPhaseKey",
                table: "TrainingDays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogPrescriptionJson",
                table: "TrainingDays",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogPrescriptionSchemaVersion",
                table: "TrainingDays",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogProgressionStageKey",
                table: "TrainingDays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogStructuralRole",
                table: "TrainingDays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogWorkoutDefinitionKey",
                table: "TrainingDays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogWorkoutDefinitionVersion",
                table: "TrainingDays",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenerationSource",
                table: "TrainingDays",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlans_SourcePreviewId",
                table: "TrainingPlans",
                column: "SourcePreviewId",
                unique: true,
                filter: "\"SourcePreviewId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingPlans_SourcePreviewId",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(name: "CatalogConfirmedAtUtc", table: "TrainingPlans");
            migrationBuilder.DropColumn(name: "CatalogDependencyVersionsJson", table: "TrainingPlans");
            migrationBuilder.DropColumn(name: "CatalogMaterializerVersion", table: "TrainingPlans");
            migrationBuilder.DropColumn(name: "CatalogPreviewContentHash", table: "TrainingPlans");
            migrationBuilder.DropColumn(name: "GenerationSource", table: "TrainingPlans");
            migrationBuilder.DropColumn(name: "SourcePreviewId", table: "TrainingPlans");
            migrationBuilder.DropColumn(name: "CatalogPhaseKey", table: "TrainingDays");
            migrationBuilder.DropColumn(name: "CatalogPrescriptionJson", table: "TrainingDays");
            migrationBuilder.DropColumn(name: "CatalogPrescriptionSchemaVersion", table: "TrainingDays");
            migrationBuilder.DropColumn(name: "CatalogProgressionStageKey", table: "TrainingDays");
            migrationBuilder.DropColumn(name: "CatalogStructuralRole", table: "TrainingDays");
            migrationBuilder.DropColumn(name: "CatalogWorkoutDefinitionKey", table: "TrainingDays");
            migrationBuilder.DropColumn(name: "CatalogWorkoutDefinitionVersion", table: "TrainingDays");
            migrationBuilder.DropColumn(name: "GenerationSource", table: "TrainingDays");
        }
    }
}
