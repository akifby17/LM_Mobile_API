using LmMobileApi.Users.Infrastructure.Repositories;

namespace LmMobileApi.Users.Application.Services;

public class RefreshTokenCleanupService(IServiceProvider serviceProvider, ILogger<RefreshTokenCleanupService> logger) : BackgroundService
{
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Her 24 saatte bir çalış

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
                
                var result = await refreshTokenRepository.DeleteExpiredTokensAsync(stoppingToken);
                
                if (result.IsSuccess)
                {
                    logger.LogInformation("Expired refresh tokens cleaned up successfully. Deleted {Count} tokens.", result.Data);
                }
                else
                {
                    logger.LogWarning("Failed to clean up expired refresh tokens: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while cleaning up expired refresh tokens");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }
} 