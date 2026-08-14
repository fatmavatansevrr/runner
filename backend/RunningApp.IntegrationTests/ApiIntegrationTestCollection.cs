using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Any integration-test class that directly resets, shares, inserts into, or
/// counts rows in the common PostgreSQL database must use this collection.
/// The real host uses one hardcoded mock user (mock-user-001), and xUnit runs
/// classes in parallel by default; an uncollected relational test can race a
/// reset/generate/confirm/count sequence and create false HTTP 500 or row-count
/// failures. Collection membership serializes all such classes while leaving
/// unrelated EF InMemory tests parallel. The shared factory also removes
/// machine-dependent Windows Event Log providers and supplies test-only EF
/// connection-open instrumentation.
/// </summary>
[CollectionDefinition(Name)]
public class ApiIntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "ApiIntegrationTests (shared mock user, sequential)";
}
