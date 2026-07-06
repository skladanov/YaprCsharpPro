public record RegisterRequest(string Login, string Password, UserRole? Role, CancellationToken token);
