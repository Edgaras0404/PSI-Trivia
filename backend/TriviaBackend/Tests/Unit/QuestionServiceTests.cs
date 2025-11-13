using Moq;
using NUnit.Framework;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Services.Implementations;
using TriviaBackend.Data;


namespace TriviaBackend.Tests.Unit
{
    [TestFixture]
    public class QuestionServiceTests
    {
        private Mock<ITriviaDbContext> _dbContextMock = null!;
        private QuestionService _service = null!;
        private List<TriviaQuestion> _questions = null!;

        [SetUp]
        public void Setup()
        {
            _questions = [
                new() { Id = 1, Category = QuestionCategory.Geography, Difficulty = DifficultyLevel.Easy },
                new() { Id = 2, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Hard },
                new() { Id = 3, Category = QuestionCategory.Geography, Difficulty = DifficultyLevel.Medium }
            ];

            var mockDbSet = MockDbSet.CreateMockDbSet(_questions);
            _dbContextMock = new Mock<ITriviaDbContext>();
            _dbContextMock.Setup(db => db.Questions).Returns(mockDbSet.Object);

            _service = new QuestionService(_dbContextMock.Object);
        }

        [Test]
        public void GetQuestions_FiltersByCategory()
        {
            var result = _service.GetQuestions([QuestionCategory.Geography]);

            Assert.That(result.All(q => q.Category == QuestionCategory.Geography));
        }

        [Test]
        public void GetQuestionCountByCategory_ReturnsCounts()
        {
            var result = _service.GetQuestionCountByCategory();

            Assert.That(result[QuestionCategory.Geography], Is.EqualTo(2));
            Assert.That(result[QuestionCategory.Science], Is.EqualTo(1));
        }
    }
}