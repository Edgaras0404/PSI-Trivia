using Microsoft.EntityFrameworkCore;
using TriviaBackend.Models.Entities;

namespace TriviaBackend.Data
{
    public interface ITriviaDbContext
    {
        DbSet<TriviaQuestion> Questions { get; }
        DbSet<BaseUser> Users { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

}
