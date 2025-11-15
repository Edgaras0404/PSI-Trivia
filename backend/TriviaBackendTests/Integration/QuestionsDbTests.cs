using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace TriviaBackendTests.Integration
{
    public class QuestionsDbTests
    {
        [Test]
        public async Task AddQuestion_ThenRetrieve_ShouldReturnSame()
        {
            var services = new ServiceCollection();

            services.AddDbContext<TriviaDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

            services.AddScoped<ITriviaDbContext, TriviaDbContext>();
            services.AddScoped<QuestionService>();

            var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();

            context.Questions.Add(
                new TriviaQuestion { Id = 1, Category = QuestionCategory.History }
            );

            await context.SaveChangesAsync();

            var service = scope.ServiceProvider.GetRequiredService<QuestionService>();
            var result = service.GetQuestionCountByCategory();

            Assert.That(result[QuestionCategory.History], Is.EqualTo(1));
        }

    }
}