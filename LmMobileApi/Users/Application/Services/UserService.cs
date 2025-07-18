using global::LmMobileApi.Shared.Results;
using global::LmMobileApi.Users.Domain;
using global::LmMobileApi.Users.Domain.Contracts;
using global::LmMobileApi.Users.Infrastructure.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


public interface IUserService
{
    Task<Result<LoginResponse>> LoginAsync(User user, CancellationToken cancellationToken = default);
}

public class UserService(IUserRepository userRepository) : IUserService
{
    public async Task<Result<LoginResponse>> LoginAsync(User user, CancellationToken cancellationToken = default)
    {
        var userResult = await userRepository.GetUserAsync(user, cancellationToken);
        if (userResult.IsFailure)
            return userResult.Error;

        if (userResult.Data is null)
            return new Error("InvalidCredentials", "User not found");

        return new LoginResponse(CreateJwtToken(userResult.Data!), userResult.Data!.PersonnelId);
    }

    private string CreateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("Touchtech-Bilgi-Teknolojileri-Yazılım-Danismanlik");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = "Touchtech",
            Issuer = "Touchtech",
            Subject = new ClaimsIdentity(
            [
                new(ClaimTypes.Name, user.UserName),
            ]),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

