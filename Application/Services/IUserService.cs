using Domain.Models;

namespace Application.Services
{
    public interface IUserService
    {
        Task RegisterAsync(String login, UserRole role, String password, CancellationToken token);
        Task LoginAsync(Guid userId, CancellationToken token);
        Task<User> GetUserByIdAsync(Guid userId, CancellationToken token);
    }
}
