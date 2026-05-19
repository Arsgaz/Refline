using Refline.Admin.Models;

namespace Refline.Admin.Business.Identity;

public static class RoleAccessPolicy
{
    public static bool CanAccessAdminApp(UserRole role)
    {
        return role is UserRole.Admin or UserRole.Manager;
    }
}
