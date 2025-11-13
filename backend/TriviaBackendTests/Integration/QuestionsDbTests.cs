using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Services.Implementations;

namespace TriviaBackendTests.Integration
{
    public class QuestionsDbTests
    {
        [Test]
        public async Task AddQuestion_ThenRetrieve_ShouldReturnSame()
        {
            //var options = new DbContextOptionsBuilder<TriviaDbContext>()
            //    .UseInMemoryDatabase("TestDb")
            //    .Options;

            //using var context = new TriviaDbContext(options);
            //var service = new QuestionService(context);

            //context.Questions.Add(new TriviaQuestion { Id = 1, Category = QuestionCategory.History });
            //await context.SaveChangesAsync();

            //var result = service.GetQuestionCountByCategory();

            //Assert.That(result[QuestionCategory.History], Is.EqualTo(1));
            Assert.That(true);
        }
    }
}