public interface IUserService
{
    Task RegisterAsync(RegisterRequest request, CancellationToken token);
    Task<string> LoginAsync(LoginRequest request, CancellationToken token);
}
