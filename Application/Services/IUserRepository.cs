using Domain.Models;

namespace Application.Services
{
    public interface IUserRepository
    {
        Task<User?> GetByLoginAsync(string login, CancellationToken token);
        Task AddUserAsync(User user, CancellationToken token);
    }
}
