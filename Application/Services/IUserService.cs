using Domain.Models;

namespace Application.Services
{
    public interface IUserService
    {
        Task UserRegisterAsync(String login, UserRole role, String password, CancellationToken token);
        Task UserLoginAsync(Guid userId, CancellationToken token);
        Task<User> GetUserAsync(Guid userId, CancellationToken token);
    }
}
