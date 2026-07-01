using Domain.Models;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<EventService> _logger;

        public UserService(IUserRepository repository, IPasswordHasher passwordHasher,  ILogger<EventService> logger)
        {
            _repository = repository;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public Task LoginAsync(Guid userId, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public async Task RegisterAsync(string login, UserRole role, string password, CancellationToken token)
        {
            if (await _repository.LoginExistsAsync(login, token))
            {
                //throw new InvalidOperationException("Логин уже занят"); //не понятно что делать
                return;
            }

            var hash = _passwordHasher.Hash(password);

            var user = User.Create(
                id: Guid.NewGuid(),
                login: login,
                passwordHash: hash,
                role: role
            );

            await _repository.CreatUserAsync(user, token);
        }
    }
}
