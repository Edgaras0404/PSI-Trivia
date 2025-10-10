using Microsoft.AspNetCore.SignalR;
using TriviaBackend.Models;
using TriviaBackend.Services;
using TriviaBackend.Models.Enums;
using TriviaBackend.Models.Entities;

namespace TriviaBackend.Hubs
{
    public class GameHub : Hub
    {
        private static readonly Dictionary<string, GameEngineService> _activeGames = new();
        private static readonly Dictionary<string, string> _playerGameMap = new();
        private static readonly Dictionary<string, CancellationTokenSource> _gameTimers = new();
        private static readonly Dictionary<string, bool> _questionRevealed = new();
        private static IHubContext<GameHub>? _staticHubContext;
        private readonly QuestionService _questionService;

        public GameHub(QuestionService questionService)
        {
            _questionService = questionService;
        }

        public static void SetHubContext(IHubContext<GameHub> hubContext)
        {
            _staticHubContext = hubContext;
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

            var gameEngine = new GameEngineService(_questionService, setting, gameId);
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

        private async Task<int> JoinGameInternal(string gameId, string playerName, GameEngineService gameEngine)
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
            Console.WriteLine($"=== SubmitAnswer called: gameId={gameId}, playerId={playerId}, answer={answerIndex} ===");

            if (!_activeGames.ContainsKey(gameId))
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            var gameEngine = _activeGames[gameId];

            var questionKey = $"{gameId}_{gameEngine.CurrentQuestion?.Id}";
            if (_questionRevealed.ContainsKey(questionKey) && _questionRevealed[questionKey])
            {
                Console.WriteLine($"Question already revealed for {questionKey}");
                await Clients.Caller.SendAsync("Error", "Question already completed");
                return;
            }

            var result = gameEngine.SubmitAnswer(playerId, answerIndex);
            Console.WriteLine($"Answer result: {result}");

            await Clients.Caller.SendAsync("AnswerResult", new
            {
                result = result.ToString(),
                isCorrect = result == AnswerResult.Correct
            });

            if (gameEngine.AllPlayersAnswered())
            {
                Console.WriteLine($"All players answered for game {gameId}");

                if (_gameTimers.ContainsKey(gameId))
                {
                    _gameTimers[gameId].Cancel();
                }

                await RevealAnswerAndProgress(gameId, gameEngine);
            }
            else
            {
                Console.WriteLine($"Waiting for more players to answer in game {gameId}");
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

        private async Task SendCurrentQuestion(string gameId, GameEngineService gameEngine)
        {
            var question = gameEngine.CurrentQuestion;
            if (question == null)
            {
                Console.WriteLine($"ERROR: No current question for game {gameId}");
                return;
            }

            var questionKey = $"{gameId}_{question.Id}";
            _questionRevealed[questionKey] = false;

            if (_gameTimers.ContainsKey(gameId))
            {
                _gameTimers[gameId].Cancel();
                _gameTimers[gameId].Dispose();
            }

            Console.WriteLine($"Sending question {gameEngine.CurrentQuestionNumber} (ID: {question.Id}) to game {gameId}, TimeLimit: {question.TimeLimit}s");

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

            var cts = new CancellationTokenSource();
            _gameTimers[gameId] = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"[TIMER] Started for {question.TimeLimit} seconds for game {gameId}");
                    await Task.Delay(TimeSpan.FromSeconds(question.TimeLimit), cts.Token);

                    // Time's up
                    if (!cts.Token.IsCancellationRequested && _staticHubContext != null)
                    {
                        Console.WriteLine($"[TIMER] Time's up for game {gameId}!");

                        // Use static hub context to send messages from background thread
                        await _staticHubContext.Clients.Group(gameId).SendAsync("TimeUp");
                        Console.WriteLine($"[TIMER] Sent TimeUp message to group {gameId}");

                        await RevealAnswerAndProgress(gameId, gameEngine);
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine($"[TIMER] Cancelled for game {gameId} - all players answered early");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TIMER] ERROR in timer for game {gameId}: {ex.Message}");
                    Console.WriteLine($"[TIMER] Stack trace: {ex.StackTrace}");
                }
                finally
                {
                    if (_gameTimers.ContainsKey(gameId) && _gameTimers[gameId] == cts)
                    {
                        _gameTimers.Remove(gameId);
                    }
                    cts.Dispose();
                }
            });
        }

        private async Task RevealAnswerAndProgress(string gameId, GameEngineService gameEngine)
        {
            Console.WriteLine($"=== RevealAnswerAndProgress called for game {gameId} ===");

            if (_staticHubContext == null)
            {
                Console.WriteLine("ERROR: Hub context is null!");
                return;
            }

            var question = gameEngine.CurrentQuestion;
            if (question == null)
            {
                Console.WriteLine($"ERROR: No current question in RevealAnswerAndProgress for game {gameId}");
                return;
            }

            var questionKey = $"{gameId}_{question.Id}";
            if (_questionRevealed.ContainsKey(questionKey) && _questionRevealed[questionKey])
            {
                Console.WriteLine($"Question {question.Id} already revealed for game {gameId}, skipping");
                return;
            }

            _questionRevealed[questionKey] = true;
            Console.WriteLine($"Revealing answer for question {question.Id} in game {gameId}");

            var leaderboard = gameEngine.GetCurrentGameLeaderboard();

            await _staticHubContext.Clients.Group(gameId).SendAsync("AnswerRevealed", new
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

            Console.WriteLine($"Sent AnswerRevealed to game {gameId}");

            Console.WriteLine($"Waiting 3 seconds before next question...");
            await Task.Delay(3000);

            Console.WriteLine($"Moving to next question for game {gameId}");

            if (!gameEngine.NextQuestion())
            {
                Console.WriteLine($"No more questions, ending game {gameId}");
                await EndGame(gameId, gameEngine);
            }
            else
            {
                Console.WriteLine($"Loading next question for game {gameId}");
                await SendCurrentQuestion(gameId, gameEngine);
            }
        }

        private async Task EndGame(string gameId, GameEngineService gameEngine)
        {
            Console.WriteLine($"=== Ending game {gameId} ===");

            if (_staticHubContext == null)
            {
                Console.WriteLine("ERROR: Hub context is null in EndGame!");
                return;
            }

            if (_gameTimers.ContainsKey(gameId))
            {
                _gameTimers[gameId].Cancel();
                _gameTimers[gameId].Dispose();
                _gameTimers.Remove(gameId);
            }

            var keysToRemove = _questionRevealed.Keys.Where(k => k.StartsWith($"{gameId}_")).ToList();
            foreach (var key in keysToRemove)
            {
                _questionRevealed.Remove(key);
            }

            var finalLeaderboard = gameEngine.GetCurrentGameLeaderboard();

            await _staticHubContext.Clients.Group(gameId).SendAsync("GameEnded", new
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
            Console.WriteLine($"Game {gameId} ended and removed from active games");
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

        private int GeneratePlayerId(GameEngineService gameEngine)
        {
            var players = gameEngine.GetPlayers();
            return players.Count > 0 ? players.Max(p => p.Id) + 1 : 1;
        }
    }
}