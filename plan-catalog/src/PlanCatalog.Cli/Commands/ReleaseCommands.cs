using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Publishing;

namespace PlanCatalog.Cli.Commands;

internal static class ReleaseCommands
{
    public static int BuildBundle(string combinationKey, int version, bool json)
    {
        var snapshot = InfrastructureFactory.CreateSourceRepository().LoadSnapshot();

        var schemaValidator = InfrastructureFactory.CreateSchemaValidator();
        var serializer = InfrastructureFactory.Serializer;
        var retirementLedger = InfrastructureFactory.CreateRetirementLedger();

        var domainResult = CatalogGraphValidator.Validate(snapshot);
        if (!domainResult.IsValid)
        {
            return CliOutput.Report("build-bundle", false, domainResult.Issues, data: null, json);
        }

        try
        {
            var stamped = CatalogStamper.StampAsPublished(serializer, InfrastructureFactory.Hasher, snapshot);

            var combination = stamped.Combinations.FirstOrDefault(c => c.Metadata.Key == combinationKey && c.Metadata.Version == version);
            if (combination is not null)
            {
                var graphResult = CandidatePublishGraphValidator.Validate(stamped, combination, retirementLedger);
                if (!graphResult.IsValid)
                {
                    return CliOutput.Report("build-bundle", false, graphResult.Issues, data: null, json);
                }
            }

            var bundle = InfrastructureFactory.CreateBundleAssembler().Assemble(stamped, combinationKey, version, retirementLedger);
            return CliOutput.Report("build-bundle", true, [], data: bundle, json);
        }
        catch (InvalidOperationException ex)
        {
            return CliOutput.Fail("build-bundle", "CLI_BUNDLE_ASSEMBLY_FAILED", ex.Message, json);
        }
    }

    public static int BuildRelease(string releaseVersion, ReleaseChannel channel, bool allowUnconfirmedContent, bool json)
    {
        try
        {
            var manifest = InfrastructureFactory.CreatePublisher().BuildPreview(releaseVersion, channel, allowUnconfirmedContent);
            return CliOutput.Report("build-release", true, [], data: manifest, json);
        }
        catch (CatalogValidationException ex)
        {
            return CliOutput.Report("build-release", false, ex.Result.Issues, data: ex.ContentDecisionDetail, json);
        }
    }

    public static int Publish(string releaseVersion, ReleaseChannel channel, bool allowUnconfirmedContent, bool dryRun, bool json)
    {
        if (dryRun)
        {
            return BuildRelease(releaseVersion, channel, allowUnconfirmedContent, json);
        }

        try
        {
            var manifest = InfrastructureFactory.CreatePublisher().Publish(releaseVersion, channel, allowUnconfirmedContent);
            return CliOutput.Report("publish", true, [], data: manifest, json);
        }
        catch (CatalogValidationException ex)
        {
            return CliOutput.Report("publish", false, ex.Result.Issues, data: ex.ContentDecisionDetail, json);
        }
        catch (InvalidOperationException ex)
        {
            return CliOutput.Fail("publish", "CLI_PUBLISH_FAILED", ex.Message, json);
        }
    }

    public static int VerifyRelease(string releaseVersion, bool json)
    {
        var repository = InfrastructureFactory.CreatePublishedRepository();

        if (!repository.ReleaseExists(releaseVersion))
        {
            return CliOutput.Fail("verify-release", "CLI_RELEASE_NOT_FOUND", $"Release '{releaseVersion}' does not exist.", json);
        }

        var releaseDirectory = Path.Combine(CliPaths.ArtifactsDirectory, "appsel-plan-catalog", releaseVersion);
        var checksumsPath = Path.Combine(releaseDirectory, "checksums.sha256");

        if (!File.Exists(checksumsPath))
        {
            return CliOutput.Fail("verify-release", "CLI_CHECKSUMS_MISSING", "checksums.sha256 not found in release directory.", json);
        }

        var issues = new List<PlanCatalog.Core.Validation.ValidationIssue>();
        var hasher = InfrastructureFactory.Hasher;

        foreach (var line in File.ReadAllLines(checksumsPath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var separatorIndex = line.IndexOf("  ", StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                issues.Add(new PlanCatalog.Core.Validation.ValidationIssue("CLI_CHECKSUM_LINE_MALFORMED", PlanCatalog.Core.Validation.ValidationSeverity.Error, $"Malformed checksum line: '{line}'."));
                continue;
            }

            var expectedHash = line[..separatorIndex];
            var relativePath = line[(separatorIndex + 2)..];
            var filePath = Path.Combine(releaseDirectory, relativePath);

            if (!File.Exists(filePath))
            {
                issues.Add(new PlanCatalog.Core.Validation.ValidationIssue("CLI_RELEASE_FILE_MISSING", PlanCatalog.Core.Validation.ValidationSeverity.Error, $"File '{relativePath}' listed in checksums.sha256 is missing."));
                continue;
            }

            var actualHash = hasher.ComputeHash(File.ReadAllText(filePath));
            if (!string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
            {
                issues.Add(new PlanCatalog.Core.Validation.ValidationIssue("CLI_RELEASE_HASH_MISMATCH", PlanCatalog.Core.Validation.ValidationSeverity.Error, $"File '{relativePath}' hash mismatch: expected {expectedHash}, got {actualHash}."));
            }
        }

        var manifest = repository.ReadManifest(releaseVersion);

        var supersededStatus = InfrastructureFactory.CreateReleaseStatusLedger().GetSupersededStatus(releaseVersion);
        if (supersededStatus is not null)
        {
            // Non-blocking: historical release verification must continue to succeed even when superseded.
            issues.Add(new PlanCatalog.Core.Validation.ValidationIssue("VERIFY_RELEASE_SUPERSEDED", PlanCatalog.Core.Validation.ValidationSeverity.Warning,
                $"Release '{releaseVersion}' is SUPERSEDED ({supersededStatus.Reason}); it is not a production-selectable release." +
                (supersededStatus.SupersededByVersion is null ? "" : $" Superseded by '{supersededStatus.SupersededByVersion}'.")));
        }

        return CliOutput.Report("verify-release", issues.All(i => i.Severity != PlanCatalog.Core.Validation.ValidationSeverity.Error), issues, data: manifest, json);
    }
}
