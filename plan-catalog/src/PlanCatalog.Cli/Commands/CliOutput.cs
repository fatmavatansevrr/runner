using System.Text.Json;
using PlanCatalog.Core.Validation;

namespace PlanCatalog.Cli.Commands;

internal static class CliOutput
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static int Report(string command, bool success, IReadOnlyList<ValidationIssue> issues, object? data, bool json)
    {
        var report = new CliReport
        {
            Success = success,
            Command = command,
            Issues = issues.Select(i => new CliReportIssue(i.Code, i.Severity.ToString(), i.Message, i.JsonPath)).ToList(),
            Data = data
        };

        Print(report, json);
        return success ? 0 : 1;
    }

    public static int Fail(string command, string code, string message, bool json)
    {
        var issue = new CliReportIssue(code, "Error", message, null);
        var report = new CliReport { Success = false, Command = command, Issues = [issue] };
        Print(report, json);
        return 1;
    }

    private static void Print(CliReport report, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
            return;
        }

        Console.WriteLine($"[{report.Command}] {(report.Success ? "PASSED" : "FAILED")}");
        foreach (var issue in report.Issues)
        {
            Console.WriteLine($"  {issue.Severity}: {issue.Code} — {issue.Message}{(issue.JsonPath is null ? "" : $" ({issue.JsonPath})")}");
        }

        if (report.Data is not null)
        {
            Console.WriteLine(JsonSerializer.Serialize(report.Data, JsonOptions));
        }
    }
}
