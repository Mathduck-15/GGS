namespace GoodGovernanceApp.Models;

public class ApplicationProfileModel
{
    public int Id { get; set; }
    public string GoveName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string LogoAddress { get; set; } = string.Empty;
}
