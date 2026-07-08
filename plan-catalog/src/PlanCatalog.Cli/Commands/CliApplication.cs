using PlanCatalog.Contracts.Enums;
using PlanCatalog.Infrastructure.Audit;

namespace PlanCatalog.Cli.Commands;

public static class CliApplication
{
    public static Task<int> RunAsync(string[] args) => Task.FromResult(Run(args));

    private static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: plan-catalog <command> [options]");
            return 1;
        }

        var command = args[0];
        var rest = args.Skip(1).ToArray();
        var json = rest.Contains("--json");
        var dryRun = rest.Contains("--dry-run");
        var allowUnconfirmedContent = rest.Contains("--allow-unconfirmed-content");

        try
        {
            return command switch
            {
                "validate" => ValidateCommands.Validate(OptionValue(rest, "--key"), OptionIntValue(rest, "--version"), json),
                "validate-combination" => ValidateCommands.ValidateCombination(PositionalArgument(rest), OptionIntValue(rest, "--version") ?? 1, json),
                "build-bundle" => ReleaseCommands.BuildBundle(PositionalArgument(rest), OptionIntValue(rest, "--version") ?? 1, json),
                "build-release" => ReleaseCommands.BuildRelease(RequireOption(rest, "--version"), ParseChannel(rest), allowUnconfirmedContent, json),
                "publish" => ReleaseCommands.Publish(RequireOption(rest, "--version"), ParseChannel(rest), allowUnconfirmedContent, dryRun, json),
                "verify-release" => ReleaseCommands.VerifyRelease(RequireOption(rest, "--version"), json),
                "retire" => RetireCommand.Retire(RequireOption(rest, "--type"), RequireOption(rest, "--key"), int.Parse(RequireOption(rest, "--version")), json),
                "supersede-release" => SupersedeReleaseCommand.Supersede(RequireOption(rest, "--version"), RequireOption(rest, "--reason"), OptionValue(rest, "--superseded-by"), json),
                "audit" => RunAudit(json),
                _ => Unknown(command)
            };
        }
        catch (InvalidOperationException ex)
        {
            return CliOutput.Fail(command, "CLI_ERROR", ex.Message, json);
        }
    }

    private static int RunAudit(bool json)
    {
        var (decisionJsonPath, decisionMdPath) = DomainContentAuditReportWriter.Write(CliPaths.RepoRoot);
        var (fixtureJsonPath, fixtureMdPath) = GoldenFixtureIntegrityReportWriter.Write(CliPaths.RepoRoot);
        return CliOutput.Report("audit", true, [], data: new { decisionJsonPath, decisionMdPath, fixtureJsonPath, fixtureMdPath }, json);
    }

    /// <summary>Defaults to PILOT — a release channel must be an explicit, deliberate choice for anything stronger.</summary>
    private static ReleaseChannel ParseChannel(string[] args)
    {
        var value = OptionValue(args, "--channel");
        if (value is null)
        {
            return ReleaseChannel.Pilot;
        }

        return Enum.Parse<ReleaseChannel>(value, ignoreCase: true);
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command '{command}'. Known commands: validate, validate-combination, build-bundle, build-release, publish, verify-release, retire, audit.");
        return 1;
    }

    private static string PositionalArgument(string[] args) =>
        args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal))
        ?? throw new InvalidOperationException("A positional argument (key) is required.");

    private static string? OptionValue(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static int? OptionIntValue(string[] args, string name)
    {
        var value = OptionValue(args, name);
        return value is null ? null : int.Parse(value);
    }

    private static string RequireOption(string[] args, string name) =>
        OptionValue(args, name) ?? throw new InvalidOperationException($"Missing required option '{name}'.");
}
