using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Identity;

public interface ILocalActivationStateStore
{
    Task<OperationResult<LocalActivationState>> LoadAsync();
    Task<OperationResult> SaveAsync(LocalActivationState state);
    Task<OperationResult> ClearAsync();
}
