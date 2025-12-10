using Microsoft.EntityFrameworkCore;
using TriviaBackend.Models.Entities;

namespace TriviaBackend.Services.Interfaces.DB
{
    public interface IClanService
    {
        Task<Clan?> GetClanByIdAsync(int clanId);
        Task AddMemberToClanAsync(Clan clan, BaseUser user);
        Task RemoveMemberFromClanAsync(Clan clan, BaseUser user);
    }
}
