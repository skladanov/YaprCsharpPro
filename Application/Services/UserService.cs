using Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly ILogger<EventService> _logger;

        public UserService(IUserRepository repository, ILogger<EventService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        Task<User> IUserService.GetUserAsync(Guid userId, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        Task IUserService.UserLoginAsync(Guid userId, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        Task IUserService.UserRegisterAsync(string login, UserRole role, string password, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
