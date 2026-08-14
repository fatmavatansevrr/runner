using RunningApp.Persistence;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Internal, production-inert seam used only by PostgreSQL integration tests
/// to add one deliberately invalid tracked mutation to the repository's
/// existing SaveChanges unit of work. It is not registered in DI and cannot
/// be selected from runtime input.
/// </summary>
internal interface ILongHorizonPersistenceConstraintMutation
{
    void Stage(AppDbContext db, LongHorizonPersistenceOperation operation, Guid planStateId);
}

internal sealed class NoOpLongHorizonPersistenceConstraintMutation : ILongHorizonPersistenceConstraintMutation
{
    public static readonly NoOpLongHorizonPersistenceConstraintMutation Instance = new();

    public void Stage(AppDbContext db, LongHorizonPersistenceOperation operation, Guid planStateId) { }
}
