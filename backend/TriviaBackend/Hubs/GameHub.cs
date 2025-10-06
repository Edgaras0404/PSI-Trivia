using Microsoft.AspNetCore.SignalR;
using TriviaBackend.Models;
using TriviaBackend.Services;

namespace TriviaBackend.Hubs
{
    public class GameHub : Hub
    {
        private static readonly Dictionary<string, GameEngine> _activeGames = new();
        private static readonly Dictionary<string, string> _playerGameMap = new();
        private readonly QuestionService _questionService;

        public GameHub(QuestionService questionService)
        {
            _questionService = questionService;
        }

        public async Task CreateGame(string playerName, int maxPlayers = 10, int questionsPerGame = 10)
        {
            Console.WriteLine("=== CreateGame called ===");
            var gameId = Guid.NewGuid().ToString()[..6].ToUpper();
            Console.WriteLine($"Generated gameId: {gameId}");

            var setting = new GameSettings
            {
                MaxPlayers = maxPlayers,
                QuestionsPerGame = questionsPerGame,
                DefaultTimeLimit = 30
            };

            var gameEngine = new GameEngine(_questionService, setting, gameId);
            var playerId = await JoinGameInternal(gameId, playerName, gameEngine);

            _activeGames[gameId] = gameEngine;

            await Clients.Caller.SendAsync("GameCreated", new
            {
                gameId,
                playerId,
                playerName,
                settings = new
                {
                    maxPlayers = setting.MaxPlayers,
                    questionsPerGame = setting.QuestionsPerGame,
                    defaultTimeLimit = setting.DefaultTimeLimit
                }
            });
        }

        public async Task JoinGame(string gameId, string playerName)
        {
            if (!_activeGames.ContainsKey(gameId))
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            var gameEngine = _activeGames[gameId];
            await JoinGameInternal(gameId, playerName, gameEngine);
        }

        private async Task<int> JoinGameInternal(string gameId, string playerName, GameEngine gameEngine)
        {
            var playerId = GeneratePlayerId(gameEngine);

            if (!gameEngine.AddPlayer(playerName, playerId))
            {
                await Clients.Caller.SendAsync("Error", "Could not join game");
                return -1;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            _playerGameMap[Context.ConnectionId] = gameId;

            var players = gameEngine.GetPlayers();
            await Clients.Group(gameId).SendAsync("PlayerJoined", new
            {
                playerId,
                playerName,
                players = players.Select(p => new { p.Id, p.Name, p.IsActive })
            });

            await Clients.Caller.SendAsync("JoinedGame", new
            {
                gameId,
                playerId,
                playerName,
                players = players.Select(p => new { p.Id, p.Name, p.IsActive })
            });

            return playerId;
        }

        public async Task StartGame(string gameId, string[]? categories, string? difficulty)
        {
            try
            {
                Console.WriteLine($"=== StartGame called with gameId: {gameId} ===");

                if (!_activeGames.ContainsKey(gameId))
                {
                    Console.WriteLine($"ERROR: Game {gameId} not found in _activeGames");
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                var gameEngine = _activeGames[gameId];
                Console.WriteLine($"Game found. Status: {gameEngine.Status}, Players: {gameEngine.GetPlayers().Count}");

                var allCategories = _questionService.GetQuestionCountByCategory();
                Console.WriteLine($"Total questions available: {allCategories.Values.Sum()}");

                QuestionCategory[]? selectedCategories = null;
                if (categories != null && categories.Length > 0)
                {
                    selectedCategories = categories
                        .Select(c => Enum.TryParse<QuestionCategory>(c, true, out var cat) ? cat : (QuestionCategory?)null)
                        .Where(c => c.HasValue)
                        .Select(c => c!.Value)
                        .ToArray();
                    Console.WriteLine($"Selected categories: {string.Join(", ", selectedCategories)}");
                }
                else
                {
                    Console.WriteLine("No categories specified - using all categories");
                }

                DifficultyLevel? maxDifficulty = null;
                if (!string.IsNullOrEmpty(difficulty) &&
                    Enum.TryParse<DifficultyLevel>(difficulty, true, out var diff))
                {
                    maxDifficulty = diff;
                    Console.WriteLine($"Max difficulty: {maxDifficulty}");
                }

                Console.WriteLine("Calling gameEngine.StartGame...");
                if (gameEngine.StartGame(selectedCategories, maxDifficulty))
                {
                    Console.WriteLine("Game started successfully!");
                    await Clients.Group(gameId).SendAsync("GameStarted");
                    await SendCurrentQuestion(gameId, gameEngine);
                }
                else
                {
                    Console.WriteLine("ERROR: gameEngine.StartGame returned false");
                    await Clients.Caller.SendAsync("Error", "Failed to start game");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION in StartGame: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("Error", $"Error: {ex.Message}");
                throw;
            }
        }

        public async Task SubmitAnswer(string gameId, int playerId, int answerIndex)
        {
            if (!_activeGames.ContainsKey(gameId))
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            var gameEngine = _activeGames[gameId];
            var result = gameEngine.SubmitAnswer(playerId, answerIndex);

            await Clients.Caller.SendAsync("AnswerResult", new
            {
                result = result.ToString(),
                isCorrect = result == AnswerResult.Correct
            });

            if (gameEngine.AllPlayersAnswered())
            {
                await RevealAnswer(gameId, gameEngine);
            }
        }

        public async Task NextQuestion(string gameId)
        {
            if (!_activeGames.ContainsKey(gameId))
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            var gameEngine = _activeGames[gameId];

            if (!gameEngine.NextQuestion())
            {
                await EndGame(gameId, gameEngine);
                return;
            }

            await SendCurrentQuestion(gameId, gameEngine);
        }

        private async Task SendCurrentQuestion(string gameId, GameEngine gameEngine)
        {
            var question = gameEngine.CurrentQuestion;
            if (question == null) return;

            await Clients.Group(gameId).SendAsync("NewQuestion", new
            {
                questionNumber = gameEngine.CurrentQuestionNumber,
                questionText = question.QuestionText,
                options = question.Options,
                category = question.Category.ToString(),
                difficulty = question.Difficulty.ToString(),
                timeLimit = question.TimeLimit,
                points = question.Points
            });
        }

        private async Task RevealAnswer(string gameId, GameEngine gameEngine)
        {
            var question = gameEngine.CurrentQuestion;
            if (question == null) return;

            var leaderboard = gameEngine.GetCurrentGameLeaderboard();

            await Clients.Group(gameId).SendAsync("AnswerRevealed", new
            {
                correctAnswer = question.CorrectAnswerIndex,
                correctText = question.Options[question.CorrectAnswerIndex],
                leaderboard = leaderboard.Select(p => new
                {
                    p.Id,
                    p.Name,
                    score = p.CurrentGameScore,
                    correctAnswers = p.CorrectAnswersInGame
                })
            });
        }

        private async Task EndGame(string gameId, GameEngine gameEngine)
        {
            var finalLeaderboard = gameEngine.GetCurrentGameLeaderboard();

            await Clients.Group(gameId).SendAsync("GameEnded", new
            {
                leaderboard = finalLeaderboard.Select(p => new
                {
                    p.Id,
                    p.Name,
                    score = p.CurrentGameScore,
                    correctAnswers = p.CorrectAnswersInGame
                })
            });

            _activeGames.Remove(gameId);
        }

        public async Task GetAvailableCategories()
        {
            var categories = _questionService.GetQuestionCountByCategory();
            await Clients.Caller.SendAsync("AvailableCategories",
                categories.Select(c => new { category = c.Key.ToString(), count = c.Value }));
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_playerGameMap.TryGetValue(Context.ConnectionId, out var gameId))
            {
                _playerGameMap.Remove(Context.ConnectionId);

                if (_activeGames.ContainsKey(gameId))
                {
                    await Clients.Group(gameId).SendAsync("PlayerDisconnected", Context.ConnectionId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        private int GeneratePlayerId(GameEngine gameEngine)
        {
            var players = gameEngine.GetPlayers();
            return players.Count > 0 ? players.Max(p => p.Id) + 1 : 1;
        }
    }
}