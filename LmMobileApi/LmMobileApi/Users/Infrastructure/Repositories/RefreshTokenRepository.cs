using LmMobileApi.Shared.Data;
using LmMobileApi.Shared.Results;
using LmMobileApi.Users.Domain;

namespace LmMobileApi.Users.Infrastructure.Repositories;

public interface IRefreshTokenRepository
{
    Task<Result<RefreshToken?>> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<Result<RefreshToken?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<RefreshToken>>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<int>> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<Result<int>> UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<Result<int>> RevokeUserTokensAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<int>> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
}

public class RefreshTokenRepository(IUnitOfWork unitOfWork) : DapperRepository(unitOfWork), IRefreshTokenRepository
{
    public async Task<Result<RefreshToken?>> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, UserId, TokenHash, ExpiresAt, CreatedAt, IsRevoked, RevokedAt, ReplacedByToken 
            FROM RefreshTokens 
            WHERE TokenHash = @TokenHash";
        
        return await QueryFirstOrDefaultAsync<RefreshToken>(sql, new { TokenHash = tokenHash }, cancellationToken);
    }

    public async Task<Result<RefreshToken?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, UserId, TokenHash, ExpiresAt, CreatedAt, IsRevoked, RevokedAt, ReplacedByToken 
            FROM RefreshTokens 
            WHERE Id = @Id";
        
        return await QueryFirstOrDefaultAsync<RefreshToken>(sql, new { Id = id }, cancellationToken);
    }

    public async Task<Result<IEnumerable<RefreshToken>>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, UserId, TokenHash, ExpiresAt, CreatedAt, IsRevoked, RevokedAt, ReplacedByToken 
            FROM RefreshTokens 
            WHERE UserId = @UserId 
            ORDER BY CreatedAt DESC";
        
        return await QueryAsync<RefreshToken>(sql, new { UserId = userId }, cancellationToken);
    }

    public async Task<Result<int>> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO RefreshTokens (Id, UserId, TokenHash, ExpiresAt, CreatedAt, IsRevoked, RevokedAt, ReplacedByToken)
            VALUES (@Id, @UserId, @TokenHash, @ExpiresAt, @CreatedAt, @IsRevoked, @RevokedAt, @ReplacedByToken)";
        
        return await ExecuteAsync(sql, refreshToken, cancellationToken);
    }

    public async Task<Result<int>> UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE RefreshTokens 
            SET IsRevoked = @IsRevoked, RevokedAt = @RevokedAt, ReplacedByToken = @ReplacedByToken
            WHERE Id = @Id";
        
        return await ExecuteAsync(sql, refreshToken, cancellationToken);
    }

    public async Task<Result<int>> RevokeUserTokensAsync(string userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE RefreshTokens 
            SET IsRevoked = 1, RevokedAt = GETUTCDATE()
            WHERE UserId = @UserId AND IsRevoked = 0";
        
        return await ExecuteAsync(sql, new { UserId = userId }, cancellationToken);
    }

    public async Task<Result<int>> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM RefreshTokens 
            WHERE ExpiresAt < GETUTCDATE() OR (IsRevoked = 1 AND RevokedAt < DATEADD(day, -7, GETUTCDATE()))";
        
        return await ExecuteAsync(sql, cancellationToken: cancellationToken);
    }
} 