using Microsoft.EntityFrameworkCore;
using System;
using TriviaBackend.Models.Enums;
using TriviaBackend.Models.Objects;

namespace TriviaBackend.Data
{
    public class TriviaDbContext : DbContext
    {

        public TriviaDbContext(DbContextOptions<TriviaDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
        public required DbSet<TriviaQuestion> Questions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TriviaQuestion>()
                .Property(q => q.Options)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                );

            modelBuilder.Entity<TriviaQuestion>().HasData(
                new TriviaQuestion
                {
                    Id = 100,
                    QuestionText = "What is the most populated city?",
                    Options = new List<string> { "Paris", "Tokyo", "Shanghai", "Gelgaudiškis" },
                    CorrectAnswerIndex = 1,
                    Category = QuestionCategory.Geography,
                    Difficulty = DifficultyLevel.Easy,
                    TimeLimit = 20
                },
                new TriviaQuestion
                {
                    Id = 200,
                    QuestionText = "which is least",
                    Options = new List<string> { "pi", "e", "golden ratio", "square root of 2" },
                    CorrectAnswerIndex = 3,
                    Category = QuestionCategory.Geography,
                    Difficulty = DifficultyLevel.Medium,
                    TimeLimit = 30
                }
            );
        }
    }
}
