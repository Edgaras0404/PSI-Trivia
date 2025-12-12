using System.Net;
using System.Net.Http.Json;
using TriviaBackend;
using TriviaBackend.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TriviaBackendTests.Integration
{
    [TestFixture]
    public class ClanControllerTests : WebApplicationFactory<Program>
    {
        private TestWebApplicationFactory _factory = null!;
        private HttpClient _client = null!;

        [SetUp]
        public void Setup()
        {
            _factory = new TestWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        [Test]
        public async Task GetClan_ById_ReturnsOk()
        {

        }
    }
}
