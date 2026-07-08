namespace PlanCatalog.Cli.Commands;

internal static class CliPaths
{
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string CatalogDirectory => Path.Combine(RepoRoot, "catalog");
    public static string SchemasDirectory => Path.Combine(RepoRoot, "schemas");

    /// <summary>Defaults to &lt;repoRoot&gt;/artifacts; overridable via PLAN_CATALOG_ARTIFACTS_DIR (used by tests to avoid mutating the real release tree).</summary>
    public static string ArtifactsDirectory =>
        Environment.GetEnvironmentVariable("PLAN_CATALOG_ARTIFACTS_DIR") is { Length: > 0 } overridePath
            ? overridePath
            : Path.Combine(RepoRoot, "artifacts");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is not null)
        {
            return dir.FullName;
        }

        dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate PlanCatalog.sln from the executable or working directory.");
    }
}
