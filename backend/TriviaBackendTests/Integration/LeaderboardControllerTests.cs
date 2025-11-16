using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TriviaBackend.Controllers;
using TriviaBackend.Exceptions;
using TriviaBackend.Models.Entities;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackendTests.Unit
{
    [TestFixture]
    public class LeaderboardControllerTests
    {
        private Mock<IPlayerService> _playerServiceMock = null!;
        private Mock<ILogger<ExceptionHandler>> _loggerMock = null!;
        private LeaderboardController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _playerServiceMock = new Mock<IPlayerService>();
            _loggerMock = new Mock<ILogger<ExceptionHandler>>();
            _controller = new LeaderboardController(_playerServiceMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task GetGlobalLeaderboard_ReturnsTopPlayers()
        {
            var players = new List<Player>
            {
                new() { Username = "Alice", Elo = 1500, TotalPoints = 500, GamesPlayed = 10 },
                new() { Username = "Bob", Elo = 1200, TotalPoints = 300, GamesPlayed = 5 }
            };

            _playerServiceMock.Setup(s => s.GetAllPlayersAsync()).ReturnsAsync(players);

            var result = await _controller.GetGlobalLeaderboard(2);

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var leaderboard = okResult!.Value as IEnumerable<object>;
            Assert.That(leaderboard!.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task GetPlayerRank_ReturnsNotFound_WhenUserDoesNotExist()
        {
            _playerServiceMock.Setup(s => s.GetPlayerByUsernameAsync("Unknown")).ReturnsAsync((Player?)null);

            var result = await _controller.GetPlayerRank("Unknown");

            var notFoundResult = result.Result as NotFoundObjectResult;
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(notFoundResult!.Value, Is.EqualTo("Player not found"));
        }


        [Test]
        public async Task UpdatePlayerStats_ReturnsNotFound_WhenPlayerDoesNotExist()
        {
            _playerServiceMock.Setup(s => s.GetPlayerByUsernameAsync("Unknown")).ReturnsAsync((Player?)null);

            var statsUpdate = new PlayerStatsUpdate { Username = "Unknown", EloChange = 50 };

            var result = await _controller.UpdatePlayerStats(statsUpdate);

            var notFoundResult = result as NotFoundObjectResult;
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(notFoundResult!.Value, Is.EqualTo("Player not found"));
        }
    }
}
