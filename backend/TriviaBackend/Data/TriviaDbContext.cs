using Microsoft.EntityFrameworkCore;
using TriviaBackend.Models.Enums;
using TriviaBackend.Models.Entities;

namespace TriviaBackend.Data
{
    /// <summary>
    /// Class for managing the database schema
    /// </summary>
    public class TriviaDbContext : DbContext, ITriviaDbContext
    {
        public TriviaDbContext(DbContextOptions<TriviaDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
        public required DbSet<TriviaQuestion> Questions { get; set; }
        public required DbSet<BaseUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BaseUser>()
                .HasDiscriminator<string>("user_type")
                .HasValue<Player>("Player")
                .HasValue<Admin>("Admin");

            modelBuilder.Entity<TriviaQuestion>().HasData(
                new TriviaQuestion
                {
                    Id = 100,
                    QuestionText = "What is the most populated city?",
                    AnswerOptions = ["Paris", "Tokyo", "Shanghai", "Gelgaudiškis"],
                    CorrectAnswerIndex = 1,
                    Category = QuestionCategory.Geography,
                    Difficulty = DifficultyLevel.Easy,
                    TimeLimit = 20
                },
                new TriviaQuestion
                {
                    Id = 200,
                    QuestionText = "which is least",
                    AnswerOptions = ["pi", "e", "golden ratio", "square root of 2"],
                    CorrectAnswerIndex = 3,
                    Category = QuestionCategory.Geography,
                    Difficulty = DifficultyLevel.Medium,
                    TimeLimit = 30
                }
            );
        }
    }
}
