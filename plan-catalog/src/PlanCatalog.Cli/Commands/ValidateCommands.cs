using PlanCatalog.Contracts;
using PlanCatalog.Core.Validation;

namespace PlanCatalog.Cli.Commands;

internal static class ValidateCommands
{
    public static int Validate(string? key, int? version, bool json)
    {
        var snapshot = InfrastructureFactory.CreateSourceRepository().LoadSnapshot();
        var schemaValidator = InfrastructureFactory.CreateSchemaValidator();
        var serializer = InfrastructureFactory.Serializer;

        var issues = new List<PlanCatalog.Core.Validation.ValidationIssue>();

        void ValidateEach<T>(IEnumerable<T> documents, string documentType, Func<T, (string Key, int Version)> id)
        {
            foreach (var document in documents)
            {
                var (docKey, docVersion) = id(document);
                if (key is not null && !string.Equals(docKey, key, StringComparison.Ordinal))
                {
                    continue;
                }

                if (version is not null && docVersion != version)
                {
                    continue;
                }

                issues.AddRange(schemaValidator.Validate(documentType, serializer.Serialize(document)).Issues);
            }
        }

        ValidateEach(snapshot.PlanTemplates, DocumentTypes.PlanTemplate, d => (d.Metadata.Key, d.Metadata.Version));
        ValidateEach(snapshot.RunLayouts, DocumentTypes.RunLayout, d => (d.Metadata.Key, d.Metadata.Version));
        ValidateEach(snapshot.LevelModifiers, DocumentTypes.LevelModifier, d => (d.Metadata.Key, d.Metadata.Version));
        ValidateEach(snapshot.WorkoutProgressions, DocumentTypes.WorkoutProgression, d => (d.Metadata.Key, d.Metadata.Version));
        ValidateEach(snapshot.ProgressionModifiers, DocumentTypes.ProgressionModifier, d => (d.Metadata.Key, d.Metadata.Version));
        ValidateEach(snapshot.Workouts, DocumentTypes.WorkoutDefinition, d => (d.Metadata.Key, d.Metadata.Version));
        ValidateEach(snapshot.RuntimeConditionValueRegistries, DocumentTypes.RuntimeConditionValueRegistry, d => (d.Metadata.Key, d.Metadata.Version));
        ValidateEach(snapshot.PeakVolumeBandPolicies, DocumentTypes.PeakVolumeBandPolicy, d => (d.Metadata.Key, d.Metadata.Version));
        ValidateEach(snapshot.RulePacks, DocumentTypes.RulePack, d => (d.Metadata.Key, d.Metadata.Version));
        ValidateEach(snapshot.Combinations, DocumentTypes.TemplateCombination, d => (d.Metadata.Key, d.Metadata.Version));

        var graphResult = CatalogGraphValidator.Validate(snapshot);
        var graphIssues = key is null
            ? graphResult.Issues
            : graphResult.Issues.Where(i => i.Message.Contains(key, StringComparison.Ordinal)).ToList();

        issues.AddRange(graphIssues);

        var success = issues.All(i => i.Severity != PlanCatalog.Core.Validation.ValidationSeverity.Error);
        return CliOutput.Report("validate", success, issues, data: null, json);
    }

    public static int ValidateCombination(string combinationKey, int version, bool json)
    {
        var snapshot = InfrastructureFactory.CreateSourceRepository().LoadSnapshot();
        var combination = snapshot.Combinations.FirstOrDefault(c => c.Metadata.Key == combinationKey && c.Metadata.Version == version);

        if (combination is null)
        {
            var issue = new PlanCatalog.Core.Validation.ValidationIssue(
                "CLI_COMBINATION_NOT_FOUND", PlanCatalog.Core.Validation.ValidationSeverity.Error,
                $"Combination '{combinationKey}' v{version} was not found in the catalog source.");
            return CliOutput.Report("validate-combination", false, [issue], data: null, json);
        }

        var structuralResult = TemplateCombinationValidator.Validate(combination, snapshot);
        var graphResult = CandidatePublishGraphValidator.Validate(snapshot, combination, InfrastructureFactory.CreateRetirementLedger());
        var allIssues = structuralResult.Issues.Concat(graphResult.Issues).ToList();
        var success = allIssues.All(i => i.Severity != PlanCatalog.Core.Validation.ValidationSeverity.Error);
        return CliOutput.Report("validate-combination", success, allIssues, data: null, json);
    }
}
