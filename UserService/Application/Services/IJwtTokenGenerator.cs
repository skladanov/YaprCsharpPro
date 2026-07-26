public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string login, UserRole role);
}