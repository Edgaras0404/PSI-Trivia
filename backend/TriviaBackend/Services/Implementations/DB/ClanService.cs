using Microsoft.EntityFrameworkCore;
using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackend.Services.Implementations.DB
{
    public class ClanService(ITriviaDbContext context) : IClanService
    {
        private readonly ITriviaDbContext _context = context;
        public async Task<Clan?> GetClanByIdAsync(int clanId) =>
            await _context.Clans.FindAsync(clanId);

        public async Task AddMemberToClanAsync(Clan clan, BaseUser user)
        {
            user.ClanId = clan.Id;
            clan.MemberIds.Add(user.Id);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveMemberFromClanAsync(Clan clan, BaseUser user)
        {
            user.ClanId = null;
            clan.MemberIds.Remove(user.Id);
            await _context.SaveChangesAsync();
        }


    }
}