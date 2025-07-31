using Dapper;
using LmMobileApi.Shared.Results;
using Microsoft.Data.SqlClient;

namespace LmMobileApi.Shared.Data;

public interface IRepository
{
    Task<Result<IEnumerable<T>>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
    Task<Result<T?>> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
    Task<Result<int>> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default);
}

public class DapperRepository(IUnitOfWork unitOfWork) : IRepository
{
    protected IUnitOfWork UnitOfWork => unitOfWork;
    public async Task<Result<IEnumerable<T>>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        // Transaction yoksa bağlantıyı kontrol et ve aç
        if (unitOfWork.Transaction == null)
        {
            var connectionResult = await unitOfWork.OpenConnectionAsync(cancellationToken);
            if (connectionResult.IsFailure)
                return connectionResult.Error;
        }

        try
        {
            var result = await ((SqlConnection)unitOfWork.Connection).QueryAsync<T>(new CommandDefinition(sql, param, unitOfWork.Transaction, cancellationToken: cancellationToken));
            return Result<IEnumerable<T>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<T>>.Failure("SqlException", ex.Message);
        }
    }

    public async Task<Result<T?>> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        // Transaction yoksa bağlantıyı kontrol et ve aç
        if (unitOfWork.Transaction == null && unitOfWork.Connection.State != System.Data.ConnectionState.Open)
        {
            var connectionResult = await unitOfWork.OpenConnectionAsync(cancellationToken);
            if (connectionResult.IsFailure)
                return connectionResult.Error;
        }

        try
        {
            var result = await ((SqlConnection)unitOfWork.Connection).QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, param, unitOfWork.Transaction, cancellationToken: cancellationToken));
            return Result<T?>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T?>.Failure("SqlException", ex.Message);
        }
    }

    public async Task<Result<int>> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        // Transaction yoksa bağlantıyı kontrol et ve aç
        if (unitOfWork.Transaction == null)
        {
            var connectionResult = await unitOfWork.OpenConnectionAsync(cancellationToken);
            if (connectionResult.IsFailure)
                return connectionResult.Error;
        }

        try
        {
            var result = await ((SqlConnection)unitOfWork.Connection).ExecuteAsync(new CommandDefinition(sql, param, unitOfWork.Transaction, cancellationToken: cancellationToken));
            return Result<int>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure("SqlException", ex.Message);
        }
    }
}
