namespace LmMobileApi.Personnels.Domain;

public class Personnel
{
    public int PersonnelID { get; private set; }
    public string PersonnelName { get; private set; } = string.Empty;

    private Personnel()
    {
    }
}