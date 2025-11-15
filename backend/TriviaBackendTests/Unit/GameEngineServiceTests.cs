using Moq;
using NUnit.Framework;
using System;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Records;
using TriviaBackend.Models.Enums;
using TriviaBackend.Exceptions;
using TriviaBackend.Services.Implementations;
using Microsoft.Extensions.Logging;

namespace TriviaBackendTests.Unit
{
    [TestFixture]
    public class GameEngineServiceTests
    {
        private Mock<IServiceProvider> _serviceProviderMock = null!;
        private Mock<ILogger<ExceptionHandler>> _loggerMock = null!;
        private GameEngineService _engine = null!;

        [SetUp]
        public void Setup()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _loggerMock = new Mock<ILogger<ExceptionHandler>>();

            _engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, new GameSettings
            {
                MaxPlayers = 2,
                AllowLateJoining = true
            });
        }

        [Test]
        public void AddPlayer_ShouldAddPlayer_WhenUnderMaxPlayers()
        {
            var result = _engine.AddPlayer("Alice");

            Assert.That(result, Is.True);
            Assert.That(_engine.GetSettings().MaxPlayers, Is.EqualTo(2));
            Assert.That(_engine.CurrentQuestionNumber, Is.EqualTo(0));
        }

        [Test]
        public void AddPlayer_ShouldReturnFalse_WhenMaxPlayersReached()
        {
            _engine.AddPlayer("Alice");
            _engine.AddPlayer("Bob");

            var result = _engine.AddPlayer("Charlie");

            Assert.That(result, Is.False);
        }

        [Test]
        public void AddPlayer_ShouldInitializePlayerCorrectly()
        {
            _engine.AddPlayer("Alice", 42);

            var field = typeof(GameEngineService)
                .GetField("_players", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var players = (System.Collections.Generic.List<GamePlayer>)field!.GetValue(_engine)!;

            Assert.That(players.Count, Is.EqualTo(1));
            Assert.That(players[0].Id, Is.EqualTo(42));
            Assert.That(players[0].Name, Is.EqualTo("Alice"));
            Assert.That(players[0].CurrentGameScore, Is.EqualTo(0));
            Assert.That(players[0].IsActive, Is.True);
        }
    }
}
