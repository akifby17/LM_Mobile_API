namespace LmMobileApi.Users.Domain.Contracts;

public record LoginResponse(
    string AccessToken, 
    string RefreshToken, 
    int PersonnelId,
    string PersonnelName,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt
);

