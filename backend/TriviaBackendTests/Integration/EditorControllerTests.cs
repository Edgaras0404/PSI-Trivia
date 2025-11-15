using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using TriviaBackend;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;

namespace TriviaBackendTests.Integration
{
    public class EditorControllerTests : WebApplicationFactory<Program>
    {
        [Test]
        public async Task GetQuestion_WhenExists_ReturnsOk()
        {
            var client = CreateClient();

            var create = await client.PostAsJsonAsync("/api/questions/create", new TriviaQuestion
            {
                Id = 10,
                Category = QuestionCategory.History
            });

            var response = await client.GetAsync("/api/editor/getquestion/10");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var q = await response.Content.ReadFromJsonAsync<TriviaQuestion>();
            Assert.That(q!.Id, Is.EqualTo(10));
        }

        [Test]
        public async Task GetQuestion_WrongIdReturnsNotFound()
        {
            var client = CreateClient();

            var response = await client.GetAsync("/api/editor/getquestion/0");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task AddQuestion_ReturnsOk()
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync("/api/editor/addquestion", new TriviaQuestion
            {
                Category = QuestionCategory.Sports
            });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task AddQuestion_WrongIndex_RetrnsBadRequest()
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync("/api/editor/addquestion", new TriviaQuestionDTO
            {
                QuestionText = "Test question",
                Answer1 = "A",
                Answer2 = "B",
                Answer3 = "C",
                Answer4 = "D",
                CorrectAnswerIndex = 5,
                Category = QuestionCategory.Science,
                Difficulty = DifficultyLevel.Easy,
                TimeLimit = 30
            }
            );

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
        [Test]
        public async Task AddQuestion_WrongTimelimit_RetrnsBadRequest()
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync("/api/editor/addquestion", new TriviaQuestionDTO
            {
                QuestionText = "Test question",
                Answer1 = "A",
                Answer2 = "B",
                Answer3 = "C",
                Answer4 = "D",
                CorrectAnswerIndex = 0,
                Category = QuestionCategory.Science,
                Difficulty = DifficultyLevel.Easy,
                TimeLimit = 2
            }
            );

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task DeleteQuestion_RemovesQuestion()
        {
            var client = CreateClient();

            await client.PostAsJsonAsync("/api/questions/create", new TriviaQuestion
            {
                Id = 20,
                Category = QuestionCategory.Science
            });

            await client.DeleteAsync("/api/editor/deletequestion/20");

            var response = await client.GetAsync("/api/editor/getquestion/20");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
    }
}
