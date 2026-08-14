using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunningApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4L6B2RollbackCompatibilityGuard : Migration
    {
        /// <inheritdoc />
        // Phase 4L.6B.2: database-level rollback compatibility guard. Deliberately
        // NOT modeled as an EF entity/DbSet -- the whole point is that this
        // protection must survive an application-binary rollback to committed
        // HEAD, which has no knowledge of this table, this trigger, or
        // RollingLongHorizon at all. Enforcement lives entirely in PostgreSQL,
        // independent of which application binary (old or current) is connected.
        // Toggling "Enabled" is intentionally NOT exposed through any HTTP route
        // in either application -- see the Phase 4L.6B.2 rollback procedure,
        // which toggles it with a direct SQL statement as an explicit,
        // non-client-reachable operational step.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE "RollbackCompatibilityMode" (
                    "Id" smallint NOT NULL,
                    "Enabled" boolean NOT NULL DEFAULT false,
                    "EnabledAtUtc" timestamp with time zone NULL,
                    "DisabledAtUtc" timestamp with time zone NULL,
                    CONSTRAINT "PK_RollbackCompatibilityMode" PRIMARY KEY ("Id"),
                    CONSTRAINT "CK_RollbackCompatibilityMode_SingletonId" CHECK ("Id" = 1)
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "RollbackCompatibilityMode" ("Id", "Enabled") VALUES (1, false);
                """);

            // Fails closed: if the singleton control row is ever missing or
            // unreadable, guard_enabled resolves to true (blocking), never
            // false (permissive). Blocks BEFORE UPDATE and BEFORE DELETE on
            // any TrainingPlans row whose ScheduleStrategy is
            // 'RollingLongHorizon' while the guard is enabled -- this is
            // stricter than "block only Status changes" because no legitimate
            // write of any kind is expected to reach this table during the
            // incident window the guard is designed for (the current
            // application, the only writer that understands rolling plans, is
            // stopped for the duration compatibility mode is enabled).
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_guard_rolling_plan_mutation() RETURNS trigger AS $$
                DECLARE
                    guard_enabled boolean;
                BEGIN
                    SELECT "Enabled" INTO guard_enabled FROM "RollbackCompatibilityMode" WHERE "Id" = 1;
                    IF guard_enabled IS NULL THEN
                        guard_enabled := true;
                    END IF;

                    IF guard_enabled AND OLD."ScheduleStrategy" = 'RollingLongHorizon' THEN
                        RAISE EXCEPTION 'ROLLBACK_COMPATIBILITY_MUTATION_BLOCKED: RollingLongHorizon plan mutation blocked while rollback compatibility mode is enabled.'
                            USING ERRCODE = 'LH001';
                    END IF;

                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    ELSE
                        RETURN NEW;
                    END IF;
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER trg_guard_rolling_plan_update
                    BEFORE UPDATE ON "TrainingPlans"
                    FOR EACH ROW
                    EXECUTE FUNCTION fn_guard_rolling_plan_mutation();
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER trg_guard_rolling_plan_delete
                    BEFORE DELETE ON "TrainingPlans"
                    FOR EACH ROW
                    EXECUTE FUNCTION fn_guard_rolling_plan_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TRIGGER IF EXISTS trg_guard_rolling_plan_delete ON "TrainingPlans";""");
            migrationBuilder.Sql("""DROP TRIGGER IF EXISTS trg_guard_rolling_plan_update ON "TrainingPlans";""");
            migrationBuilder.Sql("""DROP FUNCTION IF EXISTS fn_guard_rolling_plan_mutation();""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "RollbackCompatibilityMode";""");
        }
    }
}
