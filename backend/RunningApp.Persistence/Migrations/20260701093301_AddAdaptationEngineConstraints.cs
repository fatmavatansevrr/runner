using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdaptationEngineConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE ""AdaptationEvents"" SET ""ExplanationKey"" = '' WHERE ""ExplanationKey"" IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "ExplanationKey",
                table: "AdaptationEvents",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.Sql(@"ALTER TABLE ""AdaptationEvents"" ADD CONSTRAINT ""CK_AdaptationEvents_ExplanationKey""
                CHECK (""ExplanationKey"" <> '');");

            migrationBuilder.Sql(@"ALTER TABLE ""TrainingDays"" ADD CONSTRAINT ""CK_TrainingDays_Source""
                CHECK (""Source"" IS NULL OR ""Source"" IN ('template','user_override','engine_adapted','engine_recovered'));");

            migrationBuilder.Sql(@"ALTER TABLE ""TrainingDays""
                ADD CONSTRAINT ""CK_TrainingDays_PlannedDistance""  CHECK (""PlannedDistanceKm"" >= 0),
                ADD CONSTRAINT ""CK_TrainingDays_ActualDistance""   CHECK (""ActualDistanceKm"" IS NULL OR ""ActualDistanceKm"" >= 0),
                ADD CONSTRAINT ""CK_TrainingDays_PlannedDuration""  CHECK (""PlannedDurationMin"" >= 0),
                ADD CONSTRAINT ""CK_TrainingDays_ActualDuration""   CHECK (""ActualDurationMin"" IS NULL OR ""ActualDurationMin"" >= 0);");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptationEvents_TriggeredByTrainingDayId",
                table: "AdaptationEvents",
                column: "TriggeredByTrainingDayId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdaptationEvents_TrainingDays_TriggeredByTrainingDayId",
                table: "AdaptationEvents",
                column: "TriggeredByTrainingDayId",
                principalTable: "TrainingDays",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdaptationEvents_TrainingDays_TriggeredByTrainingDayId",
                table: "AdaptationEvents");

            migrationBuilder.DropIndex(
                name: "IX_AdaptationEvents_TriggeredByTrainingDayId",
                table: "AdaptationEvents");

            migrationBuilder.Sql(@"ALTER TABLE ""TrainingDays""
                DROP CONSTRAINT IF EXISTS ""CK_TrainingDays_PlannedDistance"",
                DROP CONSTRAINT IF EXISTS ""CK_TrainingDays_ActualDistance"",
                DROP CONSTRAINT IF EXISTS ""CK_TrainingDays_PlannedDuration"",
                DROP CONSTRAINT IF EXISTS ""CK_TrainingDays_ActualDuration"";");

            migrationBuilder.Sql(@"ALTER TABLE ""TrainingDays"" DROP CONSTRAINT IF EXISTS ""CK_TrainingDays_Source"";");

            migrationBuilder.Sql(@"ALTER TABLE ""AdaptationEvents"" DROP CONSTRAINT IF EXISTS ""CK_AdaptationEvents_ExplanationKey"";");

            migrationBuilder.AlterColumn<string>(
                name: "ExplanationKey",
                table: "AdaptationEvents",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
