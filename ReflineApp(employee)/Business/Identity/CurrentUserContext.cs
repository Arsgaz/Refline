namespace Refline.Business.Identity;

public class CurrentUserContext : ICurrentUserContext
{
    private Guid? _currentUserId;

    public Guid? GetCurrentUserId()
    {
        return _currentUserId;
    }

    public void SetCurrentUser(Guid userId)
    {
        _currentUserId = userId == Guid.Empty ? null : userId;
    }

    public void Clear()
    {
        _currentUserId = null;
    }
}
