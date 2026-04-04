namespace Refline.Business.Identity;

public interface ICurrentUserContext
{
    Guid? GetCurrentUserId();
    void SetCurrentUser(Guid userId);
    void Clear();
}
