using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// All HTTP-based tests that call POST /api/v1/testing/reset operate on a
/// single hardcoded mock user (mock-user-001) against a real, shared Postgres
/// database. xUnit runs different test classes in parallel by default, and
/// two classes racing a reset + generate-preview + confirm sequence against
/// the same user's rows produces intermittent 500s that are a test-isolation
/// artifact, not a product defect. Grouping every such class into this one
/// collection forces them to run sequentially relative to each other (xUnit's
/// default behavior for classes sharing a collection), while still running in
/// parallel with any unrelated (e.g. EF InMemory) test classes outside it.
/// </summary>
[CollectionDefinition(Name)]
public class ApiIntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "ApiIntegrationTests (shared mock user, sequential)";
}
