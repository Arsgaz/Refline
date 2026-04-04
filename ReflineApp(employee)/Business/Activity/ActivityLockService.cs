namespace Refline.Business.Activity;

public class ActivityLockService
{
    private readonly object _syncRoot = new();

    public T ExecuteLocked<T>(Func<T> action)
    {
        lock (_syncRoot)
        {
            return action();
        }
    }
}
