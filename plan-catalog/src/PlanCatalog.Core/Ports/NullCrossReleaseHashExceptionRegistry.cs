namespace PlanCatalog.Core.Ports;

/// <summary>No-op default: nothing is a known exception, so every cross-release mismatch is rejected.</summary>
public sealed class NullCrossReleaseHashExceptionRegistry : ICrossReleaseHashExceptionRegistry
{
    public static readonly NullCrossReleaseHashExceptionRegistry Instance = new();

    private NullCrossReleaseHashExceptionRegistry()
    {
    }

    public bool IsKnownException(string documentType, string key, int version, string releaseVersion, string observedContentHash) => false;
}
