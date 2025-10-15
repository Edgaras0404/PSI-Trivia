using Microsoft.EntityFrameworkCore;
using System;
using TriviaBackend.Data;
using TriviaBackend.Models.Entities;

namespace TriviaBackend.Services
{
    public class DBService(TriviaDbContext context)
    {
        private readonly TriviaDbContext _context = context;

        public async Task<BaseUser?> GetUserByIdAsync(string id) =>
            await _context.Users.FindAsync(id);
        public async Task<BaseUser?> GetUserByUsernameAsync(string id) =>
            await _context.Users.FindAsync(id);

        public async Task AddUserAsync(BaseUser user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveUserAsync(BaseUser user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
