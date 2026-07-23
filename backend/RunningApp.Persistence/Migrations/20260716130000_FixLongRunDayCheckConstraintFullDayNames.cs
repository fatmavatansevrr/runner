using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Phase 4F.9.2 relational-validation correction — pre-existing legacy
    /// defect, unrelated to Phase 4F catalog generation/routing/prescription/
    /// persistence. Discovered because this constraint was never exercised
    /// against real PostgreSQL until Phase 4F.9.2 bootstrapped a local
    /// database: <c>Application.Common.RunningDay.Normalize</c> writes
    /// <c>TrainingPlans.LongRunDay</c> as a full capitalized weekday name
    /// ("Saturday"), but the <c>CK_TrainingPlans_LongRunDay</c> check
    /// constraint added by <c>AddOnboardingSnapshotFields</c> only permitted
    /// 3-letter abbreviations ('Mon'..'Sun'), so every legacy confirm with a
    /// long-run day set failed with a 500 (23514) once the constraint was
    /// actually enforced by a live database. This migration widens the
    /// constraint to accept the full weekday names the application actually
    /// writes, while retaining the original 3-letter values for backward
    /// compatibility with any historical rows. Does not touch
    /// <c>RunningDay.Normalize</c> or any Phase 4F code/behavior.
    /// </remarks>
    public partial class FixLongRunDayCheckConstraintFullDayNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""TrainingPlans""
                DROP CONSTRAINT IF EXISTS ""CK_TrainingPlans_LongRunDay"";");

            migrationBuilder.Sql(@"ALTER TABLE ""TrainingPlans""
                ADD CONSTRAINT ""CK_TrainingPlans_LongRunDay""
                CHECK (""LongRunDay"" IS NULL OR ""LongRunDay"" IN (
                    'Monday','Tuesday','Wednesday','Thursday','Friday','Saturday','Sunday',
                    'Mon','Tue','Wed','Thu','Fri','Sat','Sun'
                ));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""TrainingPlans""
                DROP CONSTRAINT IF EXISTS ""CK_TrainingPlans_LongRunDay"";");

            migrationBuilder.Sql(@"ALTER TABLE ""TrainingPlans""
                ADD CONSTRAINT ""CK_TrainingPlans_LongRunDay""
                CHECK (""LongRunDay"" IS NULL OR ""LongRunDay"" IN (
                    'Mon','Tue','Wed','Thu','Fri','Sat','Sun'
                ));");
        }
    }
}
