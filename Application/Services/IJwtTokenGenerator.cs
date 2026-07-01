using Domain.Models;

namespace Application.Services
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(Guid userId, string login, UserRole role);
    }
}
