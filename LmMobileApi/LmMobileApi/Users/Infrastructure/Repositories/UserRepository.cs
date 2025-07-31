using System.Security.Cryptography;
using System.Text;
using LmMobileApi.Shared.Data;
using LmMobileApi.Shared.Results;
using LmMobileApi.Users.Domain;

namespace LmMobileApi.Users.Infrastructure.Repositories;

public interface IUserRepository
{
    Task<Result<User?>> GetUserAsync(User user, CancellationToken cancellationToken = default);
    Task<Result<User?>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
}

public class UserRepository(IUnitOfWork unitOfWork) : DapperRepository(unitOfWork), IUserRepository
{
    public async Task<Result<User?>> GetUserAsync(User user, CancellationToken cancellationToken = default)
    {
        // Şifreyi hash'le
        user.SetPassword(GetHashedPassword(user.Password.ToUpper()));
        
        // Kullanıcıyı sorgula
        const string usersQuery = @"SELECT * FROM Users WHERE UserName = UPPER(@UserName) AND Password = @Password";
        var requestedUser = await QueryFirstOrDefaultAsync<User>(usersQuery, user, cancellationToken: cancellationToken);
        
        if (requestedUser.Data is null)
            return Error.NotFound;
        
        // PersonnelID ve PersonnelName'i al
        const string personnelQuery = @"SELECT PersonnelID, PersonnelName FROM dbo.Personnel WHERE UPPER(PersonnelName) = UPPER(@UserName)";
        var personnelInfo = await QueryFirstOrDefaultAsync<PersonnelInfo>(personnelQuery, user, cancellationToken: cancellationToken);
        
        if (personnelInfo.Data is null)
            return new Error("PersonnelNotFound", "Personnel not found");
        
        requestedUser.Data.SetPersonnelId(personnelInfo.Data.PersonnelID);
        requestedUser.Data.SetPersonnelName(personnelInfo.Data.PersonnelName);
        return requestedUser;
    }

    public async Task<Result<User?>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        // Sadece kullanıcı adına göre kullanıcıyı bul
        const string usersQuery = @"SELECT * FROM Users WHERE UserName = UPPER(@UserName)";
        var requestedUser = await QueryFirstOrDefaultAsync<User>(usersQuery, new { UserName = username }, cancellationToken: cancellationToken);
        
        if (requestedUser.Data is null)
            return Error.NotFound;
        
        // PersonnelID ve PersonnelName'i al
        const string personnelQuery = @"SELECT PersonnelID, PersonnelName FROM dbo.Personnel WHERE UPPER(PersonnelName) = UPPER(@UserName)";
        var personnelInfo = await QueryFirstOrDefaultAsync<PersonnelInfo>(personnelQuery, new { UserName = username }, cancellationToken: cancellationToken);
        
        if (personnelInfo.Data is null)
            return new Error("PersonnelNotFound", "Personnel not found");
        
        requestedUser.Data.SetPersonnelId(personnelInfo.Data.PersonnelID);
        requestedUser.Data.SetPersonnelName(personnelInfo.Data.PersonnelName);
        return requestedUser;
    }

    private static string GetHashedPassword(string password)
    {
        var inputBytes = Encoding.ASCII.GetBytes(password);
        var hashBytes = MD5.HashData(inputBytes);
        return Convert.ToHexString(hashBytes);
    }
}

public record PersonnelInfo(int PersonnelID, string PersonnelName);