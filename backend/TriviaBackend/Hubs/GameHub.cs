using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TriviaBackend.Data;
using TriviaBackend.Exceptions;
using TriviaBackend.Models;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Services;
using static System.Net.WebRequestMethods;

namespace TriviaBackend.Hubs
{
    /// <summary>
    /// Class for managing player activity and game progression in a match
    /// </summary>
    /// <param name="questionService"></param>
    /// <param name="dbContext"></param>
    /// <param name="logger"></param>
    public class GameHub(QuestionService questionService, TriviaDbContext dbContext, ILogger<ExceptionHandler> logger) : Hub
    {
        private static readonly Dictionary<string, GameEngineService> _activeGames = new();
        private static readonly Dictionary<string, string> _playerGameMap = new();
        private static readonly Dictionary<string, CancellationTokenSource> _gameTimers = new();
        private static readonly Dictionary<string, bool> _questionRevealed = new();
        private static readonly Dictionary<string, Dictionary<int, string>> _gamePlayerUsernames = new();
        private static IHubContext<GameHub>? _staticHubContext;
        private readonly QuestionService _questionService = questionService;
        private ILogger<ExceptionHandler> _logger = logger;
        private readonly TriviaDbContext _dbContext = dbContext;

        public static void SetHubContext(IHubContext<GameHub> hubContext)
        {
            _staticHubContext = hubContext;
        }
        /// <summary>
        /// Create a new game with random Id
        /// </summary>
        /// <param name="playerName"></param>
        /// <param name="maxPlayers"></param>
        /// <param name="questionsPerGame"></param>
        /// <returns></returns>
        public async Task CreateGame(string playerName, int maxPlayers = 10, int questionsPerGame = 10)
        {
            _logger.LogInformation("=== CreateGame called ===");
            var gameId = Guid.NewGuid().ToString()[..6].ToUpper();
            _logger.LogInformation($"Generated gameId: {gameId}");

            var setting = new GameSettings
            {
                MaxPlayers = maxPlayers,
                QuestionsPerGame = questionsPerGame,
                DefaultTimeLimit = 30
            };

            var gameEngine = new GameEngineService(_questionService, _logger, setting, gameId);
            var playerId = await JoinGameInternal(gameId, playerName, gameEngine);

            _activeGames[gameId] = gameEngine;
            _gamePlayerUsernames[gameId] = new Dictionary<int, string>();
            _gamePlayerUsernames[gameId][playerId] = playerName;

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

        /// <summary>
        /// Connect a player to a game
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="playerName"></param>
        /// <returns></returns>
        public async Task JoinGame(string gameId, string playerName)
        {
            if (!_activeGames.ContainsKey(gameId))
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            var gameEngine = _activeGames[gameId];
            var playerId = await JoinGameInternal(gameId, playerName, gameEngine);

            if (!_gamePlayerUsernames.ContainsKey(gameId))
                _gamePlayerUsernames[gameId] = [];

            _gamePlayerUsernames[gameId][playerId] = playerName;
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

        /// <summary>
        /// Start the trivia match
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="categories"></param>
        /// <param name="difficulty"></param>
        /// <returns></returns>
        public async Task StartGame(string gameId, string[]? categories, string? difficulty)
        {
            try
            {
                _logger.LogInformation($"=== StartGame called with gameId: {gameId} ===");

                if (!_activeGames.ContainsKey(gameId))
                {
                    _logger.LogError($"ERROR: Game {gameId} not found in _activeGames");
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                var gameEngine = _activeGames[gameId];
                _logger.LogInformation($"Game found. Status: {gameEngine.Status}, Players: {gameEngine.GetPlayers().Count}");

                var allCategories = _questionService.GetQuestionCountByCategory();
                _logger.LogInformation($"Total questions available: {allCategories.Values.Sum()}");

                QuestionCategory[]? selectedCategories = null;
                if (categories != null && categories.Length > 0)
                {
                    selectedCategories = categories
                        .Select(c => Enum.TryParse<QuestionCategory>(c, true, out var cat) ? cat : (QuestionCategory?)null)
                        .Where(c => c.HasValue)
                        .Select(c => c!.Value)
                        .ToArray();
                    _logger.LogInformation($"Selected categories: {string.Join(", ", selectedCategories)}");
                }
                else
                {
                    _logger.LogInformation("No categories specified - using all categories");
                }

                DifficultyLevel? maxDifficulty = null;
                if (!string.IsNullOrEmpty(difficulty) &&
                    Enum.TryParse<DifficultyLevel>(difficulty, true, out var diff))
                {
                    maxDifficulty = diff;
                    _logger.LogInformation($"Max difficulty: {maxDifficulty}");
                }

                _logger.LogInformation("Calling gameEngine.StartGame...");
                if (gameEngine.StartGame(selectedCategories, maxDifficulty))
                {
                    _logger.LogInformation("Game started successfully!");
                    await Clients.Group(gameId).SendAsync("GameStarted");
                    await SendCurrentQuestion(gameId, gameEngine);
                }
                else
                {
                    _logger.LogError("ERROR: gameEngine.StartGame returned false");
                    await Clients.Caller.SendAsync("Error", "Failed to start game");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR starting game: {ex.Message}");
                throw new StartGameException("Game could not be started");
            }
        }

        /// <summary>
        /// Send player's answer to be processed
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="playerId"></param>
        /// <param name="answerIndex"></param>
        /// <returns></returns>
        public async Task SubmitAnswer(string gameId, int playerId, int answerIndex)
        {
            _logger.LogInformation($"=== SubmitAnswer called: gameId={gameId}, playerId={playerId}, answer={answerIndex} ===");

            if (!_activeGames.ContainsKey(gameId))
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            var gameEngine = _activeGames[gameId];

            var questionKey = $"{gameId}_{gameEngine.CurrentQuestion?.Id}";
            if (_questionRevealed.ContainsKey(questionKey) && _questionRevealed[questionKey])
            {
                _logger.LogInformation($"Question already revealed for {questionKey}");
                await Clients.Caller.SendAsync("Error", "Question already completed");
                return;
            }

            var result = gameEngine.SubmitAnswer(playerId, answerIndex);
            _logger.LogInformation($"Answer result: {result}");

            await Clients.Caller.SendAsync("AnswerResult", new
            {
                result = result.ToString(),
                isCorrect = result == AnswerResult.Correct
            });

            if (gameEngine.AllPlayersAnswered())
            {
                _logger.LogInformation($"All players answered for game {gameId}");

                if (_gameTimers.ContainsKey(gameId))
                {
                    _gameTimers[gameId].Cancel();
                }

                await RevealAnswerAndProgress(gameId, 5000, gameEngine);
            }
            else
            {
                _logger.LogInformation($"Waiting for more players to answer in game {gameId}");
            }
        }

        /// <summary>
        /// Get the next question in the game sequence
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
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
                _logger.LogError($"ERROR: No current question for game {gameId}");
                return;
            }

            var questionKey = $"{gameId}_{question.Id}";
            _questionRevealed[questionKey] = false;

            if (_gameTimers.ContainsKey(gameId))
            {
                _gameTimers[gameId].Cancel();
                _gameTimers[gameId].Dispose();
            }

            _logger.LogInformation($"Sending question {gameEngine.CurrentQuestionNumber} (ID: {question.Id}) to game {gameId}, TimeLimit: {question.TimeLimit}s");

            await Clients.Group(gameId).SendAsync("NewQuestion", new
            {
                questionNumber = gameEngine.CurrentQuestionNumber,
                questionText = question.QuestionText,
                options = question.AnswerOptions,
                category = question.Category.ToString(),
                difficulty = question.Difficulty.ToString(),
                timeLimit = question.TimeLimit,
                points = question.Points
            });

            var cancelTokenSource = new CancellationTokenSource();
            _gameTimers[gameId] = cancelTokenSource;

            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation($"[TIMER] Started for {question.TimeLimit} seconds for game {gameId}");
                    await Task.Delay(TimeSpan.FromSeconds(question.TimeLimit), cancelTokenSource.Token);

                    if (!cancelTokenSource.Token.IsCancellationRequested && _staticHubContext != null)
                    {
                        _logger.LogInformation($"[TIMER] Time's up for game {gameId}!");

                        await _staticHubContext.Clients.Group(gameId).SendAsync("TimeUp");
                        _logger.LogInformation($"[TIMER] Sent TimeUp message to group {gameId}");

                        await RevealAnswerAndProgress(gameId, 5000, gameEngine);
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation($"[TIMER] Cancelled for game {gameId} - all players answered early");
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"[TIMER] ERROR in timer for game {gameId}: {ex.Message}");
                    _logger.LogInformation($"[TIMER] Stack trace: {ex.StackTrace}");
                }
                finally
                {
                    if (_gameTimers.ContainsKey(gameId) && _gameTimers[gameId] == cancelTokenSource)
                    {
                        _gameTimers.Remove(gameId);
                    }
                    cancelTokenSource.Dispose();
                }
            });
        }

        /// <summary>
        /// Send answer to all players and automatically go to next question after set amount of time
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="autoProgressMillisecodns"></param>
        /// <param name="gameEngine"></param>
        /// <returns></returns>
        private async Task RevealAnswerAndProgress(string gameId, int autoProgressMillisecodns, GameEngineService gameEngine)
        {
            _logger.LogInformation($"=== RevealAnswerAndProgress called for game {gameId} ===");

            if (_staticHubContext == null)
            {
                _logger.LogError("ERROR: Hub context is null!");
                return;
            }

            var question = gameEngine.CurrentQuestion;
            if (question == null)
            {
                _logger.LogError($"ERROR: No current question in RevealAnswerAndProgress for game {gameId}");
                return;
            }

            var questionKey = $"{gameId}_{question.Id}";
            if (_questionRevealed.ContainsKey(questionKey) && _questionRevealed[questionKey])
            {
                _logger.LogInformation($"Question {question.Id} already revealed for game {gameId}, skipping");
                return;
            }

            _questionRevealed[questionKey] = true;
            _logger.LogInformation($"Revealing answer for question {question.Id} in game {gameId}");

            var leaderboard = gameEngine.GetCurrentGameLeaderboard();

            await _staticHubContext.Clients.Group(gameId).SendAsync("AnswerRevealed", new
            {
                correctAnswer = question.CorrectAnswerIndex,
                correctText = question.AnswerOptions[question.CorrectAnswerIndex],
                leaderboard = leaderboard.Select(p => new
                {
                    p.Id,
                    p.Name,
                    score = p.CurrentGameScore,
                    correctAnswers = p.CorrectAnswersInGame
                })
            });

            _logger.LogInformation($"Sent AnswerRevealed to game {gameId}");

            _logger.LogInformation($"Waiting {autoProgressMillisecodns} seconds before next question...");
            await Task.Delay(autoProgressMillisecodns);

            _logger.LogInformation($"Moving to next question for game {gameId}");

            if (!gameEngine.NextQuestion())
            {
                _logger.LogInformation($"No more questions, ending game {gameId}");
                await EndGame(gameId, gameEngine);
            }
            else
            {
                _logger.LogInformation($"Loading next question for game {gameId}");
                await SendCurrentQuestion(gameId, gameEngine);
            }
        }

        /// <summary>
        /// End the ongoing match
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="gameEngine"></param>
        /// <returns></returns>
        private async Task EndGame(string gameId, GameEngineService gameEngine)
        {
            _logger.LogInformation($"=== Ending game {gameId} ===");

            if (_staticHubContext == null)
            {
                _logger.LogError("ERROR: Hub context is null in EndGame!");
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

            await UpdatePlayerStats(gameId, finalLeaderboard);

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
            _gamePlayerUsernames.Remove(gameId);
            _logger.LogInformation($"Game {gameId} ended and removed from active games");
        }

        /// <summary>
        /// Update player information in-game
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="finalLeaderboard"></param>
        /// <returns></returns>
        private async Task UpdatePlayerStats(string gameId, List<GamePlayer> finalLeaderboard)
        {
            try
            {
                _logger.LogInformation($"=== UpdatePlayerStats called for game {gameId} ===");

                if (!_gamePlayerUsernames.ContainsKey(gameId))
                {
                    _logger.LogError($"ERROR: No player usernames found for game {gameId}");
                    return;
                }

                var playerUsernames = _gamePlayerUsernames[gameId];
                _logger.LogInformation($"Found {playerUsernames.Count} players in game");

                foreach (var gamePlayer in finalLeaderboard)
                {
                    _logger.LogInformation($"Processing player ID {gamePlayer.Id}...");

                    if (!playerUsernames.ContainsKey(gamePlayer.Id))
                    {
                        _logger.LogError($"ERROR: No username found for player ID {gamePlayer.Id}");
                        continue;
                    }

                    var username = playerUsernames[gamePlayer.Id];
                    _logger.LogInformation($"Looking up username: {username}");

                    var player = await _dbContext.Users
                        .OfType<Player>()
                        .FirstOrDefaultAsync(p => p.Username == username);

                    if (player == null)
                    {
                        _logger.LogError($"Player {username} not found");
                        throw new PlayerNotFoundException($"Player {username} not found");
                    }
                    else
                    {
                        _logger.LogInformation($"Found player in DB: {player.Username}, Current ELO: {player.Elo}, Current Points: {player.TotalPoints}, Current Games: {player.GamesPlayed}");

                        var eloChange = CalculateEloChange(gamePlayer, finalLeaderboard);
                        player.Elo += eloChange;
                        player.GamesPlayed++;
                        player.TotalPoints += gamePlayer.CurrentGameScore;

                        _logger.LogInformation($"Updated {username}: +{eloChange} ELO, +{gamePlayer.CurrentGameScore} Points, New ELO: {player.Elo}, New Total Points: {player.TotalPoints}, New Games: {player.GamesPlayed}");
                    }
                }

                var changes = await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Saved {changes} changes to database for game {gameId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR updating player statistics: {ex.Message}");
                throw new UpdatePlayerStatsException("Error while updating player statistics");
            }

        }

        /// <summary>
        /// Calculate elo difference after the game to change elo points
        /// </summary>
        /// <param name="player"></param>
        /// <param name="leaderboard"></param>
        /// <seealso cref="https://en.wikipedia.org/wiki/Elo_rating_system"/>
        /// <returns></returns>
        private static int CalculateEloChange(GamePlayer player, List<GamePlayer> leaderboard)
        {
            var position = leaderboard.FindIndex(p => p.Id == player.Id);
            var totalPlayers = leaderboard.Count;

            if (position == 0) return 25;
            if (position == 1) return 15;
            if (position == 2) return 10;
            if (position < totalPlayers / 2) return 5;
            return -5;
        }

        /// <summary>
        /// Get a list of every existing category
        /// </summary>
        /// <returns></returns>
        public async Task GetAvailableCategories()
        {
            var categories = _questionService.GetQuestionCountByCategory();
            await Clients.Caller.SendAsync("AvailableCategories",
                categories.Select(c => new { category = c.Key.ToString(), count = c.Value }));
        }

        /// <summary>
        /// Remove player from game if connection drops
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
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

        private static int GeneratePlayerId(GameEngineService gameEngine)
        {
            var players = gameEngine.GetPlayers();
            return players.Count > 0 ? players.Max(p => p.Id) + 1 : 1;
        }
    }
}