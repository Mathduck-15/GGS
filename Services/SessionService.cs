using GoodGovernanceApp.Models;

namespace GoodGovernanceApp.Services;

public class SessionService
{
    public User? CurrentUser { get; set; }

    public void ClearSession()
    {
        CurrentUser = null;
    }
}
