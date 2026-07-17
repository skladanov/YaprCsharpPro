public record RegisterRequest(string Login, string Password, string? Role, CancellationToken token);
