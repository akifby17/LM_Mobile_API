
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LmMobileApi.Shared.Data;
using LmMobileApi.Shared.Results;
using LmMobileApi.Users.Domain;
using LmMobileApi.Users.Domain.Contracts;
using LmMobileApi.Users.Infrastructure.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace LmMobileApi.Users.Application.Services;

public interface IUserService
{
    Task<Result<LoginResponse>> LoginAsync(User user, CancellationToken cancellationToken = default);
    Task<Result<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public class UserService(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository, IUnitOfWork unitOfWork) : IUserService
{
    private const int AccessTokenExpiryDays = 7; 
    private const int RefreshTokenExpiryDays = 7; // 7 gün

    public async Task<Result<LoginResponse>> LoginAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            // Bağlantıyı aç
            var connectionResult = await unitOfWork.OpenConnectionAsync(cancellationToken);
            if (connectionResult.IsFailure)
                return connectionResult.Error;

            // Transaction başlat
            unitOfWork.BeginTransaction();

            var userResult = await userRepository.GetUserAsync(user, cancellationToken);
            if (userResult.IsFailure)
            {
                unitOfWork.Rollback();
                return userResult.Error;
            }

            if (userResult.Data is null)
            {
                unitOfWork.Rollback();
                return new Error("InvalidCredentials", "User not found");
            }

            // Mevcut aktif token'ları revoke et
            var revokeResult = await refreshTokenRepository.RevokeUserTokensAsync(userResult.Data!.UserName, cancellationToken);
            if (revokeResult.IsFailure)
            {
                unitOfWork.Rollback();
                return revokeResult.Error;
            }

            // Yeni token'lar oluştur
            var accessTokenResult = CreateJwtToken(userResult.Data!);
            var refreshTokenResult = GenerateRefreshToken();
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

            // Refresh token'ı hash'leyip veritabanına kaydet
            var refreshTokenHash = HashToken(refreshTokenResult);
            var refreshToken = new RefreshToken(userResult.Data!.UserName, refreshTokenHash, refreshTokenExpiresAt);
            
            var saveResult = await refreshTokenRepository.CreateAsync(refreshToken, cancellationToken);
            if (saveResult.IsFailure)
            {
                unitOfWork.Rollback();
                return saveResult.Error;
            }

            // Transaction'ı commit et
            unitOfWork.Commit();

            return new LoginResponse(
                accessTokenResult.Token, 
                refreshTokenResult, 
                userResult.Data!.PersonnelId,
                userResult.Data!.PersonnelName,
                accessTokenResult.ExpiresAt,
                refreshTokenExpiresAt
            );
        }
        catch (Exception ex)
        {
            unitOfWork.Rollback();
            return new Error("LoginError", ex.Message);
        }
    }

    public async Task<Result<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Bağlantıyı aç
            var connectionResult = await unitOfWork.OpenConnectionAsync(cancellationToken);
            if (connectionResult.IsFailure)
                return connectionResult.Error;

            // Transaction başlat
            unitOfWork.BeginTransaction();

            // Token'ı hash'leyip veritabanından ara
            var tokenHash = HashToken(request.RefreshToken);
            
            var tokenResult = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
            

            if (tokenResult.IsFailure)
            {
                unitOfWork.Rollback();
                return tokenResult.Error;
            }

            if (tokenResult.Data is null)
            {
                unitOfWork.Rollback();
                return new Error("InvalidRefreshToken", "Refresh token not found");
            }

            var refreshToken = tokenResult.Data;

            // Token'ın geçerli olup olmadığını kontrol et
            if (!refreshToken.IsActive)
            {
                unitOfWork.Rollback();
                return new Error("InvalidRefreshToken", "Refresh token is expired or revoked");
            }

            // Kullanıcıyı bul (sadece username ile)
            var userResult = await userRepository.GetUserByUsernameAsync(refreshToken.UserId, cancellationToken);
            if (userResult.IsFailure || userResult.Data is null)
            {
                unitOfWork.Rollback();
                return new Error("UserNotFound", "User not found");
            }

            // Yeni token'lar oluştur
            var newAccessToken = CreateJwtToken(userResult.Data);
            var newRefreshTokenValue = GenerateRefreshToken();
            var newRefreshTokenHash = HashToken(newRefreshTokenValue);
            var newRefreshTokenExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

            // Eski token'ı revoke et
            refreshToken.Revoke(newRefreshTokenHash);
            var updateResult = await refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);
            if (updateResult.IsFailure)
            {
                unitOfWork.Rollback();
                return updateResult.Error;
            }

            // Yeni refresh token'ı kaydet
            var newRefreshToken = new RefreshToken(refreshToken.UserId, newRefreshTokenHash, newRefreshTokenExpiresAt);
            var saveResult = await refreshTokenRepository.CreateAsync(newRefreshToken, cancellationToken);
            if (saveResult.IsFailure)
            {
                unitOfWork.Rollback();
                return saveResult.Error;
            }

            // Transaction'ı commit et
            unitOfWork.Commit();

            return new RefreshTokenResponse(
                newAccessToken.Token,
                newRefreshTokenValue,
                newAccessToken.ExpiresAt,
                newRefreshTokenExpiresAt
            );
        }
        catch (Exception ex)
        {
            unitOfWork.Rollback();
            return new Error("RefreshTokenError", ex.Message);
        }
    }

    public async Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            // Bağlantıyı aç
            var connectionResult = await unitOfWork.OpenConnectionAsync(cancellationToken);
            if (connectionResult.IsFailure)
                return connectionResult.Error;

            // Transaction başlat
            unitOfWork.BeginTransaction();

            var tokenHash = HashToken(refreshToken);
            var tokenResult = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
            
            if (tokenResult.IsFailure)
            {
                unitOfWork.Rollback();
                return tokenResult.Error;
            }

            if (tokenResult.Data is null)
            {
                unitOfWork.Rollback();
                return new Error("InvalidRefreshToken", "Refresh token not found");
            }

            var token = tokenResult.Data;
            token.Revoke();
            
            var updateResult = await refreshTokenRepository.UpdateAsync(token, cancellationToken);
            if (updateResult.IsFailure)
            {
                unitOfWork.Rollback();
                return updateResult.Error;
            }

            // Transaction'ı commit et
            unitOfWork.Commit();
            return Result.Success;
        }
        catch (Exception ex)
        {
            unitOfWork.Rollback();
            return new Error("RevokeTokenError", ex.Message);
        }
    }

    private (string Token, DateTime ExpiresAt) CreateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("teksdata-yazilim-ve-otomasyon-sistemleri-limited-sirketi-loom-monitoring");
        var expiresAt = DateTime.UtcNow.AddDays(AccessTokenExpiryDays); 

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = "Teksdata",
            Issuer = "Teksdata",
            Subject = new ClaimsIdentity(
            [
                new(ClaimTypes.Name, user.UserName),
            ]),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiresAt);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashedBytes);
    }
}
