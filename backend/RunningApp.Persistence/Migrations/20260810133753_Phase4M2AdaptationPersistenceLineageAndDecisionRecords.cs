using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4M2AdaptationPersistenceLineageAndDecisionRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AdaptedFromSessionId",
                table: "LongHorizonRollingSessionStates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanningStatus",
                table: "LongHorizonRollingSessionStates",
                type: "text",
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "LongHorizonAdaptationDecisionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceWindowStartWeek = table.Column<int>(type: "integer", nullable: false),
                    SourceWindowEndWeek = table.Column<int>(type: "integer", nullable: false),
                    TriggerSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionType = table.Column<string>(type: "text", nullable: false),
                    SafetyReviewRequired = table.Column<bool>(type: "boolean", nullable: false),
                    ReasonCode = table.Column<string>(type: "text", nullable: false),
                    ReplacementSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupersededSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContractVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongHorizonAdaptationDecisionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LongHorizonAdaptationDecisionRecords_LongHorizonRollingPlan~",
                        column: x => x.PlanStateId,
                        principalTable: "LongHorizonRollingPlanStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LongHorizonAdaptationDecisionRecords_LongHorizonRollingSess~",
                        column: x => x.ReplacementSessionId,
                        principalTable: "LongHorizonRollingSessionStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LongHorizonAdaptationDecisionRecords_LongHorizonRollingSes~1",
                        column: x => x.SupersededSessionId,
                        principalTable: "LongHorizonRollingSessionStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LongHorizonAdaptationDecisionRecords_LongHorizonRollingSes~2",
                        column: x => x.TriggerSessionId,
                        principalTable: "LongHorizonRollingSessionStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LongHorizonRollingSessionStates_AdaptedFromSessionId",
                table: "LongHorizonRollingSessionStates",
                column: "AdaptedFromSessionId",
                unique: true,
                filter: "\"AdaptedFromSessionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LongHorizonAdaptationDecisionRecords_PlanStateId",
                table: "LongHorizonAdaptationDecisionRecords",
                column: "PlanStateId");

            migrationBuilder.CreateIndex(
                name: "IX_LongHorizonAdaptationDecisionRecords_ReplacementSessionId",
                table: "LongHorizonAdaptationDecisionRecords",
                column: "ReplacementSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LongHorizonAdaptationDecisionRecords_SupersededSessionId",
                table: "LongHorizonAdaptationDecisionRecords",
                column: "SupersededSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LongHorizonAdaptationDecisionRecords_TriggerSessionId",
                table: "LongHorizonAdaptationDecisionRecords",
                column: "TriggerSessionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LongHorizonRollingSessionStates_LongHorizonRollingSessionSt~",
                table: "LongHorizonRollingSessionStates",
                column: "AdaptedFromSessionId",
                principalTable: "LongHorizonRollingSessionStates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LongHorizonRollingSessionStates_LongHorizonRollingSessionSt~",
                table: "LongHorizonRollingSessionStates");

            migrationBuilder.DropTable(
                name: "LongHorizonAdaptationDecisionRecords");

            migrationBuilder.DropIndex(
                name: "IX_LongHorizonRollingSessionStates_AdaptedFromSessionId",
                table: "LongHorizonRollingSessionStates");

            migrationBuilder.DropColumn(
                name: "AdaptedFromSessionId",
                table: "LongHorizonRollingSessionStates");

            migrationBuilder.DropColumn(
                name: "PlanningStatus",
                table: "LongHorizonRollingSessionStates");
        }
    }
}
