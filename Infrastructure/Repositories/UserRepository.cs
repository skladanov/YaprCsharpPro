using Application.Services;
using Domain.Models;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task AddUserAsync(User user, CancellationToken token)
        {
            _context.users.Add(user);
            await _context.SaveChangesAsync(token);
        }

        public async Task<User?> GetByLoginAsync(string login, CancellationToken token)
        {
            return await _context.users.FirstOrDefaultAsync(u => u.Login == login, token);
        }
    }
}
