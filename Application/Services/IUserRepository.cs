using Domain.Models;

namespace Application.Services
{
    public interface IUserRepository
    {
        Task<User?> GetByLoginAsync(string login, CancellationToken token);
        Task CreatUserAsync(User user, CancellationToken token);
        Task<bool> LoginExistsAsync(string login, CancellationToken token);
    }
}
