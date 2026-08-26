using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase10KFreq6D13LongHorizonRollingSessionLineage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogPrescriptionProfileKey",
                table: "LongHorizonRollingSessionStates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogPrescriptionProfileVersion",
                table: "LongHorizonRollingSessionStates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LaneOrdinal",
                table: "LongHorizonRollingSessionStates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgressionStageKey",
                table: "LongHorizonRollingSessionStates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlotOrdinal",
                table: "LongHorizonRollingSessionStates",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CatalogPrescriptionProfileKey",
                table: "LongHorizonRollingSessionStates");

            migrationBuilder.DropColumn(
                name: "CatalogPrescriptionProfileVersion",
                table: "LongHorizonRollingSessionStates");

            migrationBuilder.DropColumn(
                name: "LaneOrdinal",
                table: "LongHorizonRollingSessionStates");

            migrationBuilder.DropColumn(
                name: "ProgressionStageKey",
                table: "LongHorizonRollingSessionStates");

            migrationBuilder.DropColumn(
                name: "SlotOrdinal",
                table: "LongHorizonRollingSessionStates");
        }
    }
}
