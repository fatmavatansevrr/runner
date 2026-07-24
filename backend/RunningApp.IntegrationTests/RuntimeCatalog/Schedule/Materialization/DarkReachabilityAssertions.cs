using System.Text;
using System.Text.RegularExpressions;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Test-only two-level reachability guard: verifiers may be referenced only
/// by the dark orchestrator; the orchestrator may not be activated by
/// production.
///
/// HONEST, BOUNDED CONTRACT (per ARCHITECTURAL_CLAIM_VERIFICATION_GOVERNANCE.md
/// -- claims about absence of behavior require explicit, bounded scope, not
/// an implied "catches everything"):
///
/// DOES detect, via a member-access regex over comment/string-stripped
/// source (<c>\b{Type}\s*\.\s*Verify\b</c>, no invoking-parenthesis
/// requirement):
/// - direct invocation syntax: <c>Type.Verify(...)</c>, including split
///   across multiple lines and with unusual internal whitespace;
/// - a bare method-group reference used as a value, e.g. assignment
///   (<c>var f = Type.Verify;</c>), an argument
///   (<c>items.Select(Type.Verify)</c>), or an explicit delegate-typed
///   assignment (<c>Func&lt;...&gt; f = Type.Verify;</c>) -- these do not
///   require an invoking "(" immediately after "Verify" and were NOT
///   detected by this helper's original, narrower invocation-only pattern.
///
/// Preprocessor-disabled code (<c>#if NEVER_DEFINED ... #endif</c>) is
/// deliberately still scanned and will still be flagged -- over-inclusive
/// in that direction is acceptable for a "no call site" guard; the helper
/// makes no attempt to evaluate compilation symbols.
///
/// Does NOT claim to detect, and must not be relied on for:
/// - reflection-based/late-bound invocation via <c>Type.GetType("...")</c>
///   plus <c>MethodInfo.Invoke</c> (string-constructed type/method names are
///   opaque to a source-text regex scan);
/// - expression trees that reference the method symbolically without the
///   literal source text <c>Type.Verify</c> appearing (e.g. built via
///   further indirection or code generation);
/// - source generators or other build-time code synthesis that could
///   produce a call the regex would never see as authored source text.
///
/// This is a regex-based heuristic over stripped source text, not a
/// syntax-tree/semantic analysis (no Roslyn/Microsoft.CodeAnalysis
/// dependency exists anywhere in this solution as of Phase 4G.3B.4b.1's
/// method-group fix; adding one solely to close this narrow, well-bounded
/// pattern-matching gap was judged disproportionate -- see
/// PHASE4G_3B_4B_SAFETY_VERIFICATION_ORCHESTRATOR.md for the investigation
/// and reasoning).
/// </summary>
internal static class DarkReachabilityAssertions
{
    internal const string OrchestratorFileName = "SafetyVerificationOrchestrator.cs";

    public static void AssertVerifierIsReachableOnlyFromDarkOrchestrator(string verifierTypeName)
    {
        var files = ProductionFiles().ToList();
        var orchestrator = files.Single(f => Path.GetFileName(f).Equals(OrchestratorFileName, StringComparison.OrdinalIgnoreCase));
        var reference = MemberAccessPattern(verifierTypeName);
        var orchestratorCalls = MatchesWithLines(orchestrator, reference);
        Assert.True(orchestratorCalls.Count == 1,
            $"Expected exactly one real {verifierTypeName}.Verify reference in {orchestrator}; found {orchestratorCalls.Count}: {string.Join(" | ", orchestratorCalls)}");

        var unexpected = files
            .Where(f => !Path.GetFileName(f).Equals($"{verifierTypeName}.cs", StringComparison.OrdinalIgnoreCase)
                && !Path.GetFileName(f).Equals(OrchestratorFileName, StringComparison.OrdinalIgnoreCase))
            .SelectMany(f => MatchesWithLines(f, reference)).ToList();
        Assert.True(unexpected.Count == 0,
            $"Unexpected live production caller(s) of {verifierTypeName}.Verify: {string.Join(" | ", unexpected)}");

        AssertNoProductionActivation(verifierTypeName, allowOrchestratorReference: true);
        AssertOrchestratorHasNoLiveActivation();
    }

    public static void AssertOrchestratorHasNoLiveActivation() =>
        AssertNoProductionActivation("SafetyVerificationOrchestrator", allowOrchestratorReference: false);

    /// <summary>
    /// Matches a reference to <c>{verifierTypeName}.Verify</c> as a member
    /// access, WITHOUT requiring an immediately-following invoking "(" --
    /// this is what additionally catches method-group assignments/arguments/
    /// delegate-typed assignments beyond plain invocation syntax. A trailing
    /// word boundary still excludes a differently-named member that merely
    /// starts with "Verify" (e.g. "VerifyOrDefault").
    /// </summary>
    private static Regex MemberAccessPattern(string verifierTypeName) =>
        new($@"\b{Regex.Escape(verifierTypeName)}\s*\.\s*Verify\b", RegexOptions.Compiled);

    internal static IReadOnlyList<string> FindInvocationsInSources(string verifierTypeName, IReadOnlyDictionary<string, string> sources)
    {
        var reference = MemberAccessPattern(verifierTypeName);
        return sources.SelectMany(pair => MatchesWithLines(pair.Key, pair.Value, reference)).ToList();
    }

    internal static bool ContainsActivationInSource(string typeName, string source)
    {
        var code = StripCommentsAndStrings(source);
        var codeWithStrings = StripCommentsAndStrings(source, preserveStrings: true);
        return Regex.IsMatch(code, $@"\b(AddSingleton|AddScoped|AddTransient)\s*<[^>]*\b{Regex.Escape(typeName)}\b")
            || Regex.IsMatch(code, $@"\btypeof\s*\(\s*{Regex.Escape(typeName)}\s*\)")
            || Regex.IsMatch(code, $@"\bActivator\.CreateInstance\s*<\s*{Regex.Escape(typeName)}\s*>")
            || Regex.IsMatch(codeWithStrings, $@"\bType\.GetType\s*\(\s*""[^""]*{Regex.Escape(typeName)}[^""]*""");
    }

    private static void AssertNoProductionActivation(string typeName, bool allowOrchestratorReference)
    {
        var unexpected = new List<string>();
        foreach (var file in ProductionFiles())
        {
            if (Path.GetFileName(file).Equals($"{typeName}.cs", StringComparison.OrdinalIgnoreCase)) continue;
            var content = File.ReadAllText(file);
            if (allowOrchestratorReference && Path.GetFileName(file).Equals(OrchestratorFileName, StringComparison.OrdinalIgnoreCase))
            {
                if (ContainsActivationInSource(typeName, content)) unexpected.Add(file + ": activation syntax inside orchestrator");
                continue;
            }
            var code = StripCommentsAndStrings(content);
            if (Regex.IsMatch(code, $@"\b{Regex.Escape(typeName)}\b") || ContainsActivationInSource(typeName, content)) unexpected.Add(file);
        }
        Assert.True(unexpected.Count == 0, $"Unexpected production activation/reference for {typeName}: {string.Join(" | ", unexpected)}");
    }

    private static IEnumerable<string> ProductionFiles()
    {
        var repo = TestPlanServicesFactory.RepoRoot();
        foreach (var project in new[] { "RunningApp.Application", "RunningApp.Api", "RunningApp.Infrastructure", "RunningApp.Persistence" })
        {
            var root = Path.Combine(repo, "backend", project);
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))) yield return file;
        }
    }

    private static List<string> MatchesWithLines(string file, Regex regex) => MatchesWithLines(file, File.ReadAllText(file), regex);
    private static List<string> MatchesWithLines(string file, string source, Regex regex)
    {
        var code = StripCommentsAndStrings(source); var results = new List<string>();
        foreach (Match match in regex.Matches(code))
        {
            var line = code.AsSpan(0, match.Index).Count('\n') + 1;
            results.Add($"{file}:{line}");
        }
        return results;
    }

    private static string StripCommentsAndStrings(string source, bool preserveStrings = false)
    {
        var output = new StringBuilder(source.Length); var i = 0;
        while (i < source.Length)
        {
            if (i + 1 < source.Length && source[i] == '/' && source[i + 1] == '/') { while (i < source.Length && source[i] != '\n') { output.Append(' '); i++; } continue; }
            if (i + 1 < source.Length && source[i] == '/' && source[i + 1] == '*') { output.Append("  "); i += 2; while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '/')) { output.Append(source[i] == '\n' ? '\n' : ' '); i++; } if (i + 1 < source.Length) { output.Append("  "); i += 2; } continue; }
            if (source[i] is '"' or '\'') { var quote = source[i]; output.Append(preserveStrings ? quote : ' '); i++; while (i < source.Length) { if (source[i] == '\\' && i + 1 < source.Length) { if (preserveStrings) output.Append(source, i, 2); else output.Append("  "); i += 2; continue; } var c = source[i++]; output.Append(preserveStrings || c == '\n' ? c : ' '); if (c == quote) break; } continue; }
            output.Append(source[i++]);
        }
        return output.ToString();
    }
}
