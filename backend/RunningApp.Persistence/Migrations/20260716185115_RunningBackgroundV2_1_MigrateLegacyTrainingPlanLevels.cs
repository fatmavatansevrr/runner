using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <summary>
    /// Running Background V2.1 — data-hygiene fix for pre-V2 legacy
    /// <c>TrainingPlans.Level</c> values. Confirmed via direct query against
    /// the real local dev database before writing this migration: 320 rows
    /// held the legacy text "running_regularly" (144 already held the
    /// canonical "intermediate"; no "new_to_running"/"used_to_run" rows were
    /// found in this table). This migration corrects those 320 rows to the
    /// canonical value so relational reads no longer need the legacy-alias
    /// compatibility path for this table. It does not touch
    /// PlanTemplates.Level (already migrated in
    /// 20260716175426_RunningBackgroundV2FourLevelModel) or any
    /// PlanPreviews.PreviewPayloadJson snapshot JSON (immutable/hash-verified
    /// — intentionally NOT touched by this or any migration; historical
    /// snapshot reads remain served by RunningBackgroundJsonConverter, not by
    /// a data migration).
    ///
    /// Down() cannot exactly restore which specific rows were
    /// "running_regularly" before this ran (that information isn't preserved
    /// anywhere), so it is intentionally a documented no-op rather than a
    /// blind revert to "intermediate" for a value that predates this
    /// migration. Re-running Up() is always safe (idempotent — it only
    /// touches rows still holding the legacy text).
    /// </summary>
    public partial class RunningBackgroundV2_1_MigrateLegacyTrainingPlanLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"TrainingPlans\" SET \"Level\" = 'intermediate' WHERE \"Level\" = 'running_regularly';");
            migrationBuilder.Sql(
                "UPDATE \"TrainingPlans\" SET \"Level\" = 'beginner' WHERE \"Level\" IN ('new_to_running', 'used_to_run');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentional no-op — see class-level remarks: which specific
            // rows were legacy-valued before Up() ran is not recoverable.
        }
    }
}
