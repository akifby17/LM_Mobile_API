﻿namespace LmMobileApi.Users.Domain.Contracts;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int PersonnelId,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt
);

