using PlanCatalog.Cli.Commands;
using Xunit;

namespace PlanCatalog.Tests.Publishing;

/// <summary>Exercises the CLI dispatcher end-to-end against the real pilot catalog, redirecting artifacts/ to a temp directory.</summary>
public sealed class CliCommandsTests : IDisposable
{
    private readonly string _tempArtifactsDir = Path.Combine(Path.GetTempPath(), "plan-catalog-cli-tests", Guid.NewGuid().ToString("N"));

    public CliCommandsTests()
    {
        Environment.SetEnvironmentVariable("PLAN_CATALOG_ARTIFACTS_DIR", _tempArtifactsDir);
    }

    [Fact]
    public async Task Validate_PilotCatalog_ExitsZero()
    {
        var exitCode = await CliApplication.RunAsync(["validate"]);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ValidateCombination_KnownCombination_ExitsZero()
    {
        var exitCode = await CliApplication.RunAsync(["validate-combination", "TEN_K__4D__INTERMEDIATE", "--version", "1"]);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ValidateCombination_UnknownCombination_ExitsNonZero()
    {
        var exitCode = await CliApplication.RunAsync(["validate-combination", "DOES_NOT_EXIST", "--version", "1"]);
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task BuildBundle_KnownCombination_ExitsZero()
    {
        var exitCode = await CliApplication.RunAsync(["build-bundle", "TEN_K__4D__INTERMEDIATE", "--version", "1"]);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task PublishThenVerify_ProducesAVerifiableRelease()
    {
        var publishExit = await CliApplication.RunAsync(["publish", "--version", "cli-e2e-1", "--channel", "Pilot", "--allow-unconfirmed-content"]);
        Assert.Equal(0, publishExit);

        var releaseDir = Path.Combine(_tempArtifactsDir, "appsel-plan-catalog", "cli-e2e-1");
        Assert.True(Directory.Exists(releaseDir));
        Assert.True(File.Exists(Path.Combine(releaseDir, "release-manifest.json")));
        Assert.True(File.Exists(Path.Combine(releaseDir, "checksums.sha256")));

        var verifyExit = await CliApplication.RunAsync(["verify-release", "--version", "cli-e2e-1"]);
        Assert.Equal(0, verifyExit);
    }

    [Fact]
    public async Task Publish_SameVersionTwice_SecondAttemptExitsNonZero()
    {
        var first = await CliApplication.RunAsync(["publish", "--version", "cli-e2e-2", "--channel", "Pilot", "--allow-unconfirmed-content"]);
        var second = await CliApplication.RunAsync(["publish", "--version", "cli-e2e-2", "--channel", "Pilot", "--allow-unconfirmed-content"]);

        Assert.Equal(0, first);
        Assert.NotEqual(0, second);
    }

    [Fact]
    public async Task VerifyRelease_TamperedFile_ExitsNonZeroWithHashMismatch()
    {
        await CliApplication.RunAsync(["publish", "--version", "cli-e2e-3", "--channel", "Pilot", "--allow-unconfirmed-content"]);

        var releaseDir = Path.Combine(_tempArtifactsDir, "appsel-plan-catalog", "cli-e2e-3");
        var workoutFile = Path.Combine(releaseDir, "workouts", "EASY_STANDARD.v1.json");
        await File.WriteAllTextAsync(workoutFile, File.ReadAllText(workoutFile).Replace("EASY_STANDARD", "TAMPERED"));

        var verifyExit = await CliApplication.RunAsync(["verify-release", "--version", "cli-e2e-3"]);
        Assert.NotEqual(0, verifyExit);
    }

    [Fact]
    public async Task Retire_RecordsLedgerEntry()
    {
        var exitCode = await CliApplication.RunAsync(["retire", "--type", "WORKOUT_DEFINITION", "--key", "FARTLEK", "--version", "1"]);
        Assert.Equal(0, exitCode);

        var ledgerPath = Path.Combine(_tempArtifactsDir, "appsel-plan-catalog", "retirements.json");
        Assert.True(File.Exists(ledgerPath));
        Assert.Contains("FARTLEK", File.ReadAllText(ledgerPath));
    }

    [Fact]
    public async Task RetiringAnArtifact_DoesNotBreakVerificationOfAnAlreadyPublishedHistoricalRelease()
    {
        var publishExit = await CliApplication.RunAsync(["publish", "--version", "cli-e2e-4", "--channel", "Pilot", "--allow-unconfirmed-content"]);
        Assert.Equal(0, publishExit);

        var retireExit = await CliApplication.RunAsync(["retire", "--type", "PROGRESSION_MODIFIER", "--key", "INTERMEDIATE_PROGRESSION_MODIFIER_V1", "--version", "1"]);
        Assert.Equal(0, retireExit);

        var verifyExit = await CliApplication.RunAsync(["verify-release", "--version", "cli-e2e-4"]);
        Assert.Equal(0, verifyExit);
    }

    [Fact]
    public async Task RetiringADependency_BlocksAssemblyOfANewBundleForTheSameCombination()
    {
        var retireExit = await CliApplication.RunAsync(["retire", "--type", "PROGRESSION_MODIFIER", "--key", "INTERMEDIATE_PROGRESSION_MODIFIER_V1", "--version", "1"]);
        Assert.Equal(0, retireExit);

        var buildBundleExit = await CliApplication.RunAsync(["build-bundle", "TEN_K__4D__INTERMEDIATE", "--version", "1"]);
        Assert.NotEqual(0, buildBundleExit);
    }

    [Fact]
    public async Task SupersedeRelease_DoesNotBreakVerification_ButIsFlaggedNonProduction()
    {
        await CliApplication.RunAsync(["publish", "--version", "cli-e2e-5", "--channel", "Pilot", "--allow-unconfirmed-content"]);

        var supersedeExit = await CliApplication.RunAsync(["supersede-release", "--version", "cli-e2e-5", "--reason", "test-supersede", "--superseded-by", "cli-e2e-6"]);
        Assert.Equal(0, supersedeExit);

        var ledgerPath = Path.Combine(_tempArtifactsDir, "appsel-plan-catalog", "release-status.json");
        Assert.True(File.Exists(ledgerPath));
        Assert.Contains("SUPERSEDED", File.ReadAllText(ledgerPath));

        // Historical verification must still succeed — supersession is advisory, not destructive.
        var verifyExit = await CliApplication.RunAsync(["verify-release", "--version", "cli-e2e-5"]);
        Assert.Equal(0, verifyExit);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("PLAN_CATALOG_ARTIFACTS_DIR", null);

        if (Directory.Exists(_tempArtifactsDir))
        {
            try { Directory.Delete(_tempArtifactsDir, recursive: true); } catch { /* best-effort cleanup */ }
        }

        GC.SuppressFinalize(this);
    }
}
