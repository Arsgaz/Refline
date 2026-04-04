using Refline.Data.Infrastructure;

namespace Refline.Data.Infrastructure;

public interface IAutoStartRegistryService
{
    OperationResult ApplyAutoStart(string appName, bool enable);
}
