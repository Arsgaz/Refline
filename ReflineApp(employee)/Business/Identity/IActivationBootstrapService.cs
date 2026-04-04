using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Identity;

public interface IActivationBootstrapService
{
    Task<OperationResult<LocalActivationState>> BootstrapAsync();
}
