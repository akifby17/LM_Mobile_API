using System.Data;
using LmMobileApi.Shared.Results;
using Microsoft.Data.SqlClient;

namespace LmMobileApi.Shared.Data;

public interface IDatabaseContext : IDisposable
{
    IDbConnection Connection { get; }
    Task<Result> OpenConnectionAsync(CancellationToken cancellationToken = default);
}

public class DapperDatabaseContext(IDbConnection connection) : IDatabaseContext
{
    private bool _disposedValue = false;

    public IDbConnection Connection => connection;

    public async Task<Result> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ((SqlConnection)connection).OpenAsync(cancellationToken);
            return Result.Success;
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue) return;
        if (disposing)
            connection.Dispose();

        _disposedValue = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    ~DapperDatabaseContext()
    {
        Dispose(disposing: false);
    }
}