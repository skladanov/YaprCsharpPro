using Domain.Models;

namespace Application.Services
{
    public interface IUserService
    {
        Task RegisterAsync(String login, UserRole role, String password, CancellationToken token);
        Task<string> LoginAsync(String login, String password, CancellationToken token);
    }
}
