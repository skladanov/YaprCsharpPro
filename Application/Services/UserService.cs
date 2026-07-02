using Domain.Exceptions;
using Domain.Models;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
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

        public async Task<string?> LoginAsync(string login, string password, CancellationToken token)
        {
            // 1. Ищем пользователя по логину (без загрузки лишних данных — репозиторий делает FirstOrDefault)
            var user = await _repository.GetByLoginAsync(login, token);

            if (user is null)
            {
                // Важно: не сообщай злоумышленнику, что именно не так (логин или пароль).
                // Для API достаточно вернуть null или общий ответ «неверные учётные данные».
                return null;
            }

            // 2. Проверяем пароль через безопасный метод сравнения
            if (! _passwordHasher.Verify(password, user.PasswordHash))
            {
                return null;
            }

            // 3. Генерируем JWT-токен
            return _jwtTokenGenerator.GenerateToken(user.Id, user.Login, user.Role);
        }

        public async Task RegisterAsync(string login, UserRole role, string password, CancellationToken token)
        {
            var exists = await _repository.GetByLoginAsync(login, token);
            if (exists != null)
                throw new DuplicateLoginException(login);

            var hash = _passwordHasher.Hash(password);

            var user = User.Create(
                id: Guid.NewGuid(),
                login: login,
                passwordHash: hash,
                role: role
            );

            await _repository.AddUserAsync(user, token);
        }
    }
}
