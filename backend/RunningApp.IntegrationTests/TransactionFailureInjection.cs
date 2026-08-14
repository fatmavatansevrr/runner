using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Backend Integration Phase 4G.6C.2 — test-only mid-transaction
/// failure-injection seam (Part 1). Registered EXCLUSIVELY by
/// <see cref="CustomWebApplicationFactory"/>'s <c>ConfigureWebHost</c>
/// (test infrastructure), never by <c>RunningApp.Api/Program.cs</c>
/// (production DI). Disabled by default (<see cref="FailWhenSqlContains"/>
/// is null) — a no-op interceptor with zero effect on normal confirmation
/// behavior unless a test explicitly arms it.
///
/// Design: rather than a coarse "checkpoint name" API, this consults the
/// REAL SQL command text EF Core/Npgsql is about to execute inside the
/// real transaction, and throws before it runs once a configured table name
/// has appeared <see cref="FailAfterOccurrence"/> times. This is deliberately
/// at the lowest safe seam (a <see cref="DbCommandInterceptor"/>/
/// <see cref="DbTransactionInterceptor"/> pair) so the real EF/PostgreSQL
/// transaction executes exactly as production would up to the injected
/// failure point — never a mocked repository, never a pre-transaction throw.
/// </summary>
public sealed class TransactionFailureInjectionState
{
    private int _occurrenceCount;

    /// <summary>Substring to match against each executing command's SQL text (e.g. "\"TrainingWeeks\""). Null = disabled.</summary>
    public string? FailWhenSqlContains { get; set; }

    /// <summary>1-based: fail on the Nth command whose text contains <see cref="FailWhenSqlContains"/>.</summary>
    public int FailAfterOccurrence { get; set; } = 1;

    /// <summary>When true, the NEXT transaction commit attempt fails instead of any command.</summary>
    public bool FailOnCommit { get; set; }

    public bool CommandAttempted { get; private set; }

    public void Reset()
    {
        FailWhenSqlContains = null;
        FailAfterOccurrence = 1;
        FailOnCommit = false;
        _occurrenceCount = 0;
        CommandAttempted = false;
    }

    internal bool ShouldFailCommand(string commandText)
    {
        if (FailWhenSqlContains is null) return false;
        if (!commandText.Contains(FailWhenSqlContains, StringComparison.OrdinalIgnoreCase)) return false;
        CommandAttempted = true;
        var occurrence = Interlocked.Increment(ref _occurrenceCount);
        return occurrence >= FailAfterOccurrence;
    }
}

/// <summary>Thrown by the injection seam — never a real infrastructure failure. Distinct type so tests can assert the injected path was actually reached.</summary>
public sealed class TransactionFailureInjectedException : Exception
{
    public TransactionFailureInjectedException(string message) : base(message) { }
}

public sealed class TransactionFailureInjectionCommandInterceptor : DbCommandInterceptor
{
    private readonly TransactionFailureInjectionState _state;
    public TransactionFailureInjectionCommandInterceptor(TransactionFailureInjectionState state) => _state = state;

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        if (_state.ShouldFailCommand(command.CommandText))
        {
            throw new TransactionFailureInjectedException(
                $"[TEST-ONLY INJECTED FAILURE] before executing a command matching '{_state.FailWhenSqlContains}'.");
        }
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (_state.ShouldFailCommand(command.CommandText))
        {
            throw new TransactionFailureInjectedException(
                $"[TEST-ONLY INJECTED FAILURE] before executing a command matching '{_state.FailWhenSqlContains}'.");
        }
        return base.NonQueryExecutingAsync(command, eventData, result, ct);
    }

    // EF Core's Npgsql modification-command-batch machinery issues INSERT
    // statements via ExecuteReader (not ExecuteNonQuery) so it can read back
    // affected-row counts/RETURNING clauses per statement in a batch -- this
    // is the seam that actually fires for TrainingWeek/TrainingDay/PlanEvent
    // inserts in practice (confirmed empirically: NonQueryExecuting alone
    // never observed these commands).
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        if (_state.ShouldFailCommand(command.CommandText))
        {
            throw new TransactionFailureInjectedException(
                $"[TEST-ONLY INJECTED FAILURE] before executing a command matching '{_state.FailWhenSqlContains}'.");
        }
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken ct = default)
    {
        if (_state.ShouldFailCommand(command.CommandText))
        {
            throw new TransactionFailureInjectedException(
                $"[TEST-ONLY INJECTED FAILURE] before executing a command matching '{_state.FailWhenSqlContains}'.");
        }
        return base.ReaderExecutingAsync(command, eventData, result, ct);
    }
}

public sealed class TransactionFailureInjectionTransactionInterceptor : DbTransactionInterceptor
{
    private readonly TransactionFailureInjectionState _state;
    public TransactionFailureInjectionTransactionInterceptor(TransactionFailureInjectionState state) => _state = state;

    public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result)
    {
        if (_state.FailOnCommit)
        {
            throw new TransactionFailureInjectedException("[TEST-ONLY INJECTED FAILURE] at transaction commit.");
        }
        return base.TransactionCommitting(transaction, eventData, result);
    }

    public override ValueTask<InterceptionResult> TransactionCommittingAsync(
        DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken ct = default)
    {
        if (_state.FailOnCommit)
        {
            throw new TransactionFailureInjectedException("[TEST-ONLY INJECTED FAILURE] at transaction commit.");
        }
        return base.TransactionCommittingAsync(transaction, eventData, result, ct);
    }
}
