using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase10KFreq6D4D4CatalogPrescriptionProfileLineage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogPrescriptionProfileKey",
                table: "TrainingDays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogPrescriptionProfileVersion",
                table: "TrainingDays",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CatalogPrescriptionProfileKey",
                table: "TrainingDays");

            migrationBuilder.DropColumn(
                name: "CatalogPrescriptionProfileVersion",
                table: "TrainingDays");
        }
    }
}
