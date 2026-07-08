namespace PlanCatalog.Core.Ports;

public sealed record ReleaseStatusEntry(string ReleaseVersion, string Reason, string? SupersededByVersion);

/// <summary>
/// Tracks which published releases have been superseded/marked non-production, without ever mutating
/// the immutable release directory itself. Consulted so default/production resolution never silently
/// selects a superseded release.
/// </summary>
public interface IReleaseStatusLedger
{
    ReleaseStatusEntry? GetSupersededStatus(string releaseVersion);
}
