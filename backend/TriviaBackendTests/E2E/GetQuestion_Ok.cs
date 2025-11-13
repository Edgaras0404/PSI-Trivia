using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;

namespace TriviaBackendTests.E2E
{
    //[TestFixture]
    //public class EditorControllerE2ETests
    //{
    //    private WebApplicationFactory<Program> _factory = null!;
    //    private HttpClient _client = null!;

    //    [SetUp]
    //    public void Setup()
    //    {
    //        _factory = new WebApplicationFactory<Program>();
    //        _client = _factory.CreateClient();
    //    }

    //    [TearDown]
    //    public void TearDown()
    //    {
    //        _client.Dispose();
    //        _factory.Dispose();
    //    }

    //    [Test]
    //    public async Task GetQuestion_ReturnsQuestion_WhenExists()
    //    {
    //        var question = new TriviaQuestion
    //        {
    //            Id = 1,
    //            QuestionText = "Test?",
    //            AnswerOptions = ["A", "B", "C", "D"],
    //            CorrectAnswerIndex = 0,
    //            Category = QuestionCategory.Geography,
    //            Difficulty = DifficultyLevel.Easy,
    //            TimeLimit = 20
    //        };

    //        // For an in-memory or test DB setup, insert question here
    //        // (Example with TestServer / InMemory DB not shown)

    //        // Act
    //        var response = await _client.GetAsync("/api/editor/getquestion/1");

    //        // Assert
    //        Assert.That(response.IsSuccessStatusCode, Is.True);

    //        var result = await response.Content.ReadFromJsonAsync<TriviaQuestion>();
    //        Assert.That(result, Is.Not.Null);
    //        Assert.That(result!.Id, Is.EqualTo(1));
    //    }

    //    [Test]
    //    public async Task GetQuestion_ReturnsNotFound_WhenMissing()
    //    {
    //        var response = await _client.GetAsync("/api/editor/getquestion/9999");
    //        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    //    }

    //    [Test]
    //    public async Task AddQuestion_ReturnsOk()
    //    {
    //        var newQuestion = new TriviaQuestionDTO
    //        {
    //            QuestionText = "New question?",
    //            Answer1 = "A",
    //            Answer2 = "B",
    //            Answer3 = "C",
    //            Answer4 = "D",
    //            CorrectAnswerIndex = 1,
    //            Category = QuestionCategory.History,
    //            Difficulty = DifficultyLevel.Medium,
    //            TimeLimit = 30
    //        };

    //        var response = await _client.PostAsJsonAsync("/api/editor/addquestion", newQuestion);
    //        Assert.That(response.IsSuccessStatusCode, Is.True);
    //    }

    //    [Test]
    //    public async Task DeleteQuestion_ReturnsOk()
    //    {
    //        var response = await _client.DeleteAsync("/api/editor/deletequestion/1");
    //        Assert.That(response.IsSuccessStatusCode, Is.True);
    //    }
    //}
}