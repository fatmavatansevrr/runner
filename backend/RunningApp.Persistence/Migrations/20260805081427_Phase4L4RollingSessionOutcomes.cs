using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4L4RollingSessionOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CompletionStatus",
                table: "LongHorizonRollingSessionStates",
                type: "text",
                nullable: false,
                defaultValue: "Planned",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<double>(
                name: "ActualDistanceKm",
                table: "LongHorizonRollingSessionStates",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActualDurationMinutes",
                table: "LongHorizonRollingSessionStates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "LongHorizonRollingSessionStates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotTodayReason",
                table: "LongHorizonRollingSessionStates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NotTodayRecordedAtUtc",
                table: "LongHorizonRollingSessionStates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OutcomeVersion",
                table: "LongHorizonRollingSessionStates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LongHorizonRollingSessionStates_AssignedDate",
                table: "LongHorizonRollingSessionStates",
                column: "AssignedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LongHorizonRollingSessionStates_AssignedDate",
                table: "LongHorizonRollingSessionStates");

            migrationBuilder.DropColumn(
                name: "ActualDistanceKm",
                table: "LongHorizonRollingSessionStates");

            migrationBuilder.DropColumn(
                name: "ActualDurationMinutes",
                table: "LongHorizonRollingSessionStates");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "LongHorizonRollingSessionStates");

            migrationBuilder.DropColumn(
                name: "NotTodayReason",
                table: "LongHorizonRollingSessionStates");

            migrationBuilder.DropColumn(
                name: "NotTodayRecordedAtUtc",
                table: "LongHorizonRollingSessionStates");

            migrationBuilder.DropColumn(
                name: "OutcomeVersion",
                table: "LongHorizonRollingSessionStates");

            migrationBuilder.AlterColumn<string>(
                name: "CompletionStatus",
                table: "LongHorizonRollingSessionStates",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Planned");
        }
    }
}
