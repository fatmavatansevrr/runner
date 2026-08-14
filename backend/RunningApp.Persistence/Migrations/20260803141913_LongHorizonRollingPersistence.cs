using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LongHorizonRollingPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LongHorizonRollingPlanStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalWeeks = table.Column<int>(type: "integer", nullable: false),
                    ReadinessProfile = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RaceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GoalType = table.Column<string>(type: "text", nullable: false),
                    GoalDistance = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<string>(type: "text", nullable: false),
                    DaysPerWeek = table.Column<int>(type: "integer", nullable: false),
                    PreferredDaysCsv = table.Column<string>(type: "text", nullable: false),
                    LongRunDay = table.Column<string>(type: "text", nullable: false),
                    CandidateKey = table.Column<string>(type: "text", nullable: false),
                    CandidateVersion = table.Column<int>(type: "integer", nullable: false),
                    CatalogRootPath = table.Column<string>(type: "text", nullable: false),
                    CurrentLifecycleStatus = table.Column<string>(type: "text", nullable: false),
                    CurrentWindowStartWeek = table.Column<int>(type: "integer", nullable: false),
                    CurrentWindowEndWeek = table.Column<int>(type: "integer", nullable: false),
                    LastActivatedGlobalWeek = table.Column<int>(type: "integer", nullable: true),
                    LatestCheckpointDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ActiveContextVersionSequence = table.Column<int>(type: "integer", nullable: false),
                    ActiveContextVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentBlockedPublicReasonCategory = table.Column<string>(type: "text", nullable: true),
                    CurrentBlockedInternalReasonCode = table.Column<string>(type: "text", nullable: true),
                    BlockedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    RetryEligible = table.Column<bool>(type: "boolean", nullable: false),
                    PersistenceContractVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongHorizonRollingPlanStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LongHorizonActivationWindowRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartGlobalWeek = table.Column<int>(type: "integer", nullable: false),
                    EndGlobalWeek = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    ContextVersionSequence = table.Column<int>(type: "integer", nullable: false),
                    ContextVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckpointDecisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CoreContextId = table.Column<Guid>(type: "uuid", nullable: true),
                    RunwayPrescriptionId = table.Column<string>(type: "text", nullable: true),
                    TargetLockId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: false),
                    FailureReasonCode = table.Column<string>(type: "text", nullable: true),
                    ContractVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongHorizonActivationWindowRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LongHorizonActivationWindowRecords_LongHorizonRollingPlanSt~",
                        column: x => x.PlanStateId,
                        principalTable: "LongHorizonRollingPlanStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LongHorizonBlockRetryRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    BlockedGlobalWeekStart = table.Column<int>(type: "integer", nullable: false),
                    BlockedGlobalWeekEnd = table.Column<int>(type: "integer", nullable: false),
                    PublicReasonCategory = table.Column<string>(type: "text", nullable: false),
                    InternalReasonCode = table.Column<string>(type: "text", nullable: false),
                    EvidenceFingerprint = table.Column<string>(type: "text", nullable: false),
                    CheckpointDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RetryEligible = table.Column<bool>(type: "boolean", nullable: false),
                    RelatedDecisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongHorizonBlockRetryRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LongHorizonBlockRetryRecords_LongHorizonRollingPlanStates_P~",
                        column: x => x.PlanStateId,
                        principalTable: "LongHorizonRollingPlanStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LongHorizonCheckpointRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AsOfDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceWindowStartWeek = table.Column<int>(type: "integer", nullable: false),
                    SourceWindowEndWeek = table.Column<int>(type: "integer", nullable: false),
                    EvidenceFingerprint = table.Column<string>(type: "text", nullable: false),
                    ValidatedWeeklyVolumeKm = table.Column<double>(type: "double precision", nullable: true),
                    ValidatedLongRunKm = table.Column<double>(type: "double precision", nullable: true),
                    CompletedFrequency = table.Column<int>(type: "integer", nullable: true),
                    AuthorityClassification = table.Column<string>(type: "text", nullable: false),
                    Decision = table.Column<string>(type: "text", nullable: false),
                    AuthoritativeReasonCode = table.Column<string>(type: "text", nullable: true),
                    ContextVersionSequence = table.Column<int>(type: "integer", nullable: false),
                    PersistenceVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongHorizonCheckpointRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LongHorizonCheckpointRecords_LongHorizonRollingPlanStates_P~",
                        column: x => x.PlanStateId,
                        principalTable: "LongHorizonRollingPlanStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LongHorizonCoreContextRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContextVersionSequence = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFromGlobalWeek = table.Column<int>(type: "integer", nullable: false),
                    EffectiveToGlobalWeek = table.Column<int>(type: "integer", nullable: false),
                    AsOfDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ConditionResultSummaryJson = table.Column<string>(type: "jsonb", nullable: false),
                    ValidatedLoadAuthoritySummary = table.Column<string>(type: "text", nullable: false),
                    GeneratedCoreResultIdentity = table.Column<string>(type: "text", nullable: false),
                    SelectedCoreWeeksPayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    SupersededByContextId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedDecisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongHorizonCoreContextRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LongHorizonCoreContextRecords_LongHorizonRollingPlanStates_~",
                        column: x => x.PlanStateId,
                        principalTable: "LongHorizonRollingPlanStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LongHorizonRollingWeekStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    GlobalWeek = table.Column<int>(type: "integer", nullable: false),
                    SegmentType = table.Column<string>(type: "text", nullable: false),
                    Stage = table.Column<string>(type: "text", nullable: false),
                    StructuralStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StructuralEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LifecycleState = table.Column<string>(type: "text", nullable: false),
                    WeeklyVolumeKm = table.Column<double>(type: "double precision", nullable: true),
                    LongRunKm = table.Column<double>(type: "double precision", nullable: true),
                    ActivationContextVersionSequence = table.Column<int>(type: "integer", nullable: true),
                    ActivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BlockedReasonCode = table.Column<string>(type: "text", nullable: true),
                    BlockedDecisionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongHorizonRollingWeekStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LongHorizonRollingWeekStates_LongHorizonRollingPlanStates_P~",
                        column: x => x.PlanStateId,
                        principalTable: "LongHorizonRollingPlanStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LongHorizonRunwayStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetLockId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetContextVersionSequence = table.Column<int>(type: "integer", nullable: false),
                    LockedRunwayStartGlobalWeek = table.Column<int>(type: "integer", nullable: false),
                    LockedRunwayEndGlobalWeek = table.Column<int>(type: "integer", nullable: false),
                    CoreWeekOneWeeklyTargetKm = table.Column<double>(type: "double precision", nullable: false),
                    CoreWeekOneLongRunTargetKm = table.Column<double>(type: "double precision", nullable: false),
                    FullPrescriptionId = table.Column<string>(type: "text", nullable: false),
                    FullPrescriptionVersion = table.Column<int>(type: "integer", nullable: false),
                    PrescriptionPayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CalendarCompositionIdentity = table.Column<string>(type: "text", nullable: false),
                    CreatedDecisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongHorizonRunwayStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LongHorizonRunwayStates_LongHorizonRollingPlanStates_PlanSt~",
                        column: x => x.PlanStateId,
                        principalTable: "LongHorizonRollingPlanStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LongHorizonRollingSessionStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionOrdinal = table.Column<int>(type: "integer", nullable: false),
                    SessionRole = table.Column<string>(type: "text", nullable: false),
                    WorkoutKey = table.Column<string>(type: "text", nullable: true),
                    WorkoutVersion = table.Column<int>(type: "integer", nullable: true),
                    DistanceKm = table.Column<double>(type: "double precision", nullable: false),
                    AssignedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActivationContextVersionSequence = table.Column<int>(type: "integer", nullable: false),
                    Provenance = table.Column<string>(type: "text", nullable: false),
                    CompletionStatus = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongHorizonRollingSessionStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LongHorizonRollingSessionStates_LongHorizonRollingWeekState~",
                        column: x => x.WeekStateId,
                        principalTable: "LongHorizonRollingWeekStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LongHorizonActivationWindowRecords_IdempotencyKey",
                table: "LongHorizonActivationWindowRecords",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LongHorizonActivationWindowRecords_PlanStateId_Range",
                table: "LongHorizonActivationWindowRecords",
                columns: new[] { "PlanStateId", "StartGlobalWeek", "EndGlobalWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_LongHorizonBlockRetryRecords_PlanStateId_CreatedAtUtc",
                table: "LongHorizonBlockRetryRecords",
                columns: new[] { "PlanStateId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LongHorizonCheckpointRecords_PlanStateId_AsOfDate_Window",
                table: "LongHorizonCheckpointRecords",
                columns: new[] { "PlanStateId", "AsOfDate", "SourceWindowStartWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LongHorizonCoreContextRecords_PlanStateId_ContextVersionSequence",
                table: "LongHorizonCoreContextRecords",
                columns: new[] { "PlanStateId", "ContextVersionSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LongHorizonRollingSessionStates_WeekStateId_SessionOrdinal",
                table: "LongHorizonRollingSessionStates",
                columns: new[] { "WeekStateId", "SessionOrdinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LongHorizonRollingWeekStates_PlanStateId_GlobalWeek",
                table: "LongHorizonRollingWeekStates",
                columns: new[] { "PlanStateId", "GlobalWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LongHorizonRunwayStates_PlanStateId",
                table: "LongHorizonRunwayStates",
                column: "PlanStateId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LongHorizonActivationWindowRecords");

            migrationBuilder.DropTable(
                name: "LongHorizonBlockRetryRecords");

            migrationBuilder.DropTable(
                name: "LongHorizonCheckpointRecords");

            migrationBuilder.DropTable(
                name: "LongHorizonCoreContextRecords");

            migrationBuilder.DropTable(
                name: "LongHorizonRollingSessionStates");

            migrationBuilder.DropTable(
                name: "LongHorizonRunwayStates");

            migrationBuilder.DropTable(
                name: "LongHorizonRollingWeekStates");

            migrationBuilder.DropTable(
                name: "LongHorizonRollingPlanStates");
        }
    }
}
