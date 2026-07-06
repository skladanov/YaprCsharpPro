using Domain.Models;

public record LoginRequest(string Login, string Password, CancellationToken token);
