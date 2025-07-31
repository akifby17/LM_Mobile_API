using System.Data;
using Dapper;
using LmMobileApi.Shared.Results;
using Microsoft.Data.SqlClient;

namespace LmMobileApi.Shared.Data;

public interface IUnitOfWork : IDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
    Task<Result> OpenConnectionAsync(CancellationToken cancellationToken = default);
    void BeginTransaction();
    void Commit();
    void Rollback();
    Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken = default);

}

// Implementation of DapperUnitOfWork with IDatabaseContext
public class DapperUnitOfWork(IDatabaseContext databaseContext) : IUnitOfWork
{
    private IDbTransaction? _transaction;
    private bool _disposed;

    public IDbConnection Connection => databaseContext.Connection;
    public IDbTransaction? Transaction => _transaction;

    public Task<Result> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        return databaseContext.OpenConnectionAsync(cancellationToken);
    }

    public void BeginTransaction()
    {
        _transaction = databaseContext.Connection.BeginTransaction();
    }

    public void Commit()
    {
        _transaction?.Commit();
    }

    public void Rollback()
    {
        _transaction?.Rollback();
    }

    public async Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ((SqlConnection)databaseContext.Connection).ExecuteAsync(new CommandDefinition("SELECT 1", transaction: _transaction, cancellationToken: cancellationToken));
            return Result<int>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure("SqlException", ex.Message);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _transaction?.Dispose();
            databaseContext?.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

