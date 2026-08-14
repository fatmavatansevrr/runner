using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LongHorizonRunwayCalendarAndTargetLockSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CalendarProjectionPayloadJson",
                table: "LongHorizonRunwayStates",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetLockPayloadJson",
                table: "LongHorizonRunwayStates",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalendarProjectionPayloadJson",
                table: "LongHorizonRunwayStates");

            migrationBuilder.DropColumn(
                name: "TargetLockPayloadJson",
                table: "LongHorizonRunwayStates");
        }
    }
}
