using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Identity;

public interface IUserStore
{
    Task<OperationResult<User?>> GetByLoginAsync(string login);
    Task<OperationResult<User?>> GetByIdAsync(Guid userId);
    Task<OperationResult<IReadOnlyList<User>>> GetAllAsync();
    Task<OperationResult> SaveAllAsync(IEnumerable<User> users);
}
