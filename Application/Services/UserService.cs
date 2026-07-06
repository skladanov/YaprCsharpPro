using Microsoft.Extensions.Logging;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<EventService> _logger;

    public UserService(IUserRepository repository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator, ILogger<EventService> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<string?> LoginAsync(LoginRequest request, CancellationToken token)
    {
        // 1. Ищем пользователя по логину (без загрузки лишних данных — репозиторий делает FirstOrDefault)
        var user = await _repository.GetByLoginAsync(request.Login, token);

        if (user is null)
            throw new UnauthorizedAccessException();

        // 2. Проверяем пароль через безопасный метод сравнения
        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException();

        // 3. Генерируем JWT-токен
        return _jwtTokenGenerator.GenerateToken(user.Id, user.Login, user.Role);
    }

    public async Task RegisterAsync(RegisterRequest request, CancellationToken token)
    {
        var user = await _repository.GetByLoginAsync(request.Login, token);
        if (user is not null)
            throw new DuplicateLoginException(request.Login);

        var hash = _passwordHasher.Hash(request.Password);

        var newUser = User.Create(
            id: Guid.NewGuid(),
            login: request.Login,
            passwordHash: hash,
            role: request.Role ?? UserRole.User
        );

        await _repository.AddUserAsync(newUser, token);
    }
}
