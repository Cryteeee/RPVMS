using BlazorApp1.Server.Data;
using Microsoft.EntityFrameworkCore;
using BlazorApp1.Shared.Models;

namespace BlazorApp1.Server.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            return !await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> IsUsernameUniqueAsync(string username)
        {
            return !await _context.Users.AnyAsync(u => u.UserName.ToLower() == username.ToLower());
        }
    }
}
