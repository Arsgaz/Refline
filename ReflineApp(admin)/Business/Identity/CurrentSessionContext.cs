using Refline.Admin.Models;

namespace Refline.Admin.Business.Identity;

public sealed class CurrentSessionContext
{
    public AdminUser? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public long CompanyId => CurrentUser?.CompanyId ?? 0;

    public UserRole? Role => CurrentUser?.Role;

    public void SetCurrentUser(AdminUser user)
    {
        CurrentUser = user;
    }

    public void Clear()
    {
        CurrentUser = null;
    }
}
