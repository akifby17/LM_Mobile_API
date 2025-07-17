namespace LmMobileApi.Users.Domain;

public class User
{
    public string UserName { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;
    public int PersonnelId { get; private set; }

    public User(string userName, string password)
    {
        UserName = userName;
        Password = password;
    }

    public void SetPassword(string password)
    {
        Password = password;
    }

    public void SetPersonnelId(int personnelId)
    {
        PersonnelId = personnelId;
    }

    /// <summary>
    /// Constructor for Dapper
    /// </summary>
    private User() { } // For Dapper
}