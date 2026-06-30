using Domain.Models;

namespace Application.Services
{
    interface IUserRepository
    {
        Task UserRegisterAsync(String login, UserRole role, String password, CancellationToken token);
        Task UserLoginAsync(Guid userId, CancellationToken token);
        Task<User> GetUserAsync(Guid userId, CancellationToken token);
    }
}
