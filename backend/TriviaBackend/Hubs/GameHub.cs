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
using System.Collections.Concurrent; // ADDED: Import for concurrent collections

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
        private static readonly ConcurrentDictionary<string, GameEngineService> _activeGames = new();

        private static readonly ConcurrentDictionary<string, string> _playerGameMap = new();

        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _gameTimers = new();

        private static readonly ConcurrentDictionary<string, bool> _questionRevealed = new();

        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, string>> _gamePlayerUsernames = new();

        private static IHubContext<GameHub>? _staticHubContext;
        private readonly QuestionService _questionService = questionService;
        private ILogger<ExceptionHandler> _logger = logger;
        private readonly TriviaDbContext _dbContext = dbContext;

        public static void SetHubContext(IHubContext<GameHub> hubContext)
        {
            _staticHubContext = hubContext;
        }

        /// <summary>
        /// Create a new game with random Id and initialize in lobby state
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        public async Task CreateGame(string playerName)
        {
            _logger.LogInformation("=== CreateGame called ===");
            var gameId = Guid.NewGuid().ToString()[..6].ToUpper();
            _logger.LogInformation($"Generated gameId: {gameId}");

            var setting = new GameSettings
            {
                MaxPlayers = 10,
                QuestionsPerGame = 10,
                DefaultTimeLimit = 30,
                QuestionCategories = Enum.GetValues<QuestionCategory>(),
                MaxDifficulty = DifficultyLevel.Hard
            };

            var gameEngine = new GameEngineService(_questionService, _logger, setting, gameId);

            if (!_activeGames.TryAdd(gameId, gameEngine))
            {
                await Clients.Caller.SendAsync("Error", "Failed to create game with this ID");
                return;
            }

            _gamePlayerUsernames.TryAdd(gameId, new ConcurrentDictionary<int, string>());

            // Add the creator as a player using a modified join method that doesn't send JoinedGame
            var playerId = await JoinGameInternalForCreator(gameId, playerName, gameEngine);

            if (_gamePlayerUsernames.TryGetValue(gameId, out var playerDict))
            {
                playerDict.TryAdd(playerId, playerName);
            }

            var allCategories = Enum.GetValues<QuestionCategory>().Select(c => c.ToString()).ToArray();
            var allDifficulties = Enum.GetValues<DifficultyLevel>().Select(d => d.ToString()).ToArray();

            // Send GameCreated as the final event so isHost stays true
            await Clients.Caller.SendAsync("GameCreated", new
            {
                gameId,
                playerId,
                playerName,
                settings = new
                {
                    maxPlayers = setting.MaxPlayers,
                    questionsPerGame = setting.QuestionsPerGame,
                    defaultTimeLimit = setting.DefaultTimeLimit,
                    questionCategories = setting.QuestionCategories.Select(c => c.ToString()).ToArray(),
                    maxDifficulty = setting.MaxDifficulty.ToString()
                },
                availableCategories = allCategories,
                availableDifficulties = allDifficulties
            });
        }

        /// <summary>
        /// Update game settings in the lobby before starting the game
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="maxPlayers"></param>
        /// <param name="questionsPerGame"></param>
        /// <param name="categories"></param>
        /// <param name="maxDifficulty"></param>
        /// <returns></returns>
        public async Task UpdateGameSettings(string gameId, int? maxPlayers = null, int? questionsPerGame = null,
            string[]? categories = null, string? maxDifficulty = null)
        {
            try
            {
                if (!_activeGames.TryGetValue(gameId, out var gameEngine))
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");

                    return;
                }

                if (gameEngine.Status != GameStatus.Waiting)
                {
                    await Clients.Caller.SendAsync("Error", "Cannot update settings after game has started");
                    return;
                }


                var existingSettings = gameEngine.GetSettings();
                var currentPlayers = gameEngine.GetPlayers();

                var currentSettings = new GameSettings
                {
                    MaxPlayers = maxPlayers ?? existingSettings.MaxPlayers,
                    QuestionsPerGame = questionsPerGame ?? existingSettings.QuestionsPerGame,
                    DefaultTimeLimit = existingSettings.DefaultTimeLimit
                };

                if (categories != null && categories.Length > 0)
                {
                    currentSettings.QuestionCategories = categories
                        .Select(c => Enum.TryParse<QuestionCategory>(c, true, out var cat) ? cat : (QuestionCategory?)null)
                        .Where(c => c.HasValue)
                        .Select(c => c!.Value)
                        .ToArray();
                }
                else
                {
                    currentSettings.QuestionCategories = existingSettings.QuestionCategories;
                }

                if (!string.IsNullOrEmpty(maxDifficulty) && Enum.TryParse<DifficultyLevel>(maxDifficulty, true, out var difficulty))
                {
                    currentSettings.MaxDifficulty = difficulty;
                }
                else
                {
                    currentSettings.MaxDifficulty = existingSettings.MaxDifficulty;
                }

                // Create new game engine with updated settings
                var newGameEngine = new GameEngineService(_questionService, _logger, currentSettings, gameId);

                // Re-add all existing players
                foreach (var player in currentPlayers)
                {
                    newGameEngine.AddPlayer(player.Name, player.Id, player.JoinedGameAt);
                }

                // Replace the game engine
                _activeGames.TryUpdate(gameId, newGameEngine, gameEngine);

                // Notify all players in the game about the updated settings
                await Clients.Group(gameId).SendAsync("SettingsUpdated", new
                {
                    settings = new
                    {
                        maxPlayers = currentSettings.MaxPlayers,
                        questionsPerGame = currentSettings.QuestionsPerGame,
                        defaultTimeLimit = currentSettings.DefaultTimeLimit,
                        questionCategories = currentSettings.QuestionCategories.Select(c => c.ToString()).ToArray(),
                        maxDifficulty = currentSettings.MaxDifficulty.ToString()
                    }
                });
            }
            catch (Exception)
            {
                _logger.LogError($"ERROR: Cannot update player stats");
                throw new GameUpdateException("Error updating player stats");
            }
        }

        /// <summary>
        /// Connect a player to a game
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="playerName"></param>
        /// <returns></returns>
        public async Task JoinGame(string gameId, string playerName)
        {
            if (!_activeGames.TryGetValue(gameId, out var gameEngine))
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            var playerId = await JoinGameInternal(gameId, playerName, gameEngine);

            var playerDict = _gamePlayerUsernames.GetOrAdd(gameId, _ => new ConcurrentDictionary<int, string>());
            playerDict.TryAdd(playerId, playerName);
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

            _playerGameMap.TryAdd(Context.ConnectionId, gameId);

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

        // Special version for game creator - doesn't send JoinedGame event
        private async Task<int> JoinGameInternalForCreator(string gameId, string playerName, GameEngineService gameEngine)
        {
            var playerId = GeneratePlayerId(gameEngine);

            if (!gameEngine.AddPlayer(playerName, playerId))
            {
                await Clients.Caller.SendAsync("Error", "Could not join game");
                return -1;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

            _playerGameMap.TryAdd(Context.ConnectionId, gameId);

            // Only send PlayerJoined to the group (not JoinedGame to caller)
            var players = gameEngine.GetPlayers();
            await Clients.Group(gameId).SendAsync("PlayerJoined", new
            {
                playerId,
                playerName,
                players = players.Select(p => new { p.Id, p.Name, p.IsActive })
            });

            return playerId;
        }

        /// <summary>
        /// Remove a player from a game and cleanup if needed
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public async Task LeaveGame(string gameId, int playerId)
        {
            _logger.LogInformation($"=== LeaveGame called: gameId={gameId}, playerId={playerId} ===");

            if (!_activeGames.TryGetValue(gameId, out var gameEngine))
            {
                _logger.LogWarning($"Game {gameId} not found when trying to leave");
                return;
            }

            var players = gameEngine.GetPlayers();
            var player = players.FirstOrDefault(p => p.Id == playerId);

            if (player != null)
            {
                players.Remove(player);

                if (_gamePlayerUsernames.TryGetValue(gameId, out var playerDict))
                {
                    playerDict.TryRemove(playerId, out _);
                }

                _logger.LogInformation($"Player {playerId} removed from game {gameId}. Remaining players: {players.Count}");

                _playerGameMap.TryRemove(Context.ConnectionId, out _);

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);

                if (players.Count == 0)
                {
                    _logger.LogInformation($"No players left in game {gameId}, cleaning up...");

                    if (_gameTimers.TryRemove(gameId, out var cts))
                    {
                        cts.Cancel();
                        cts.Dispose();
                    }

                    var keysToRemove = _questionRevealed.Keys.Where(k => k.StartsWith($"{gameId}_")).ToList();
                    foreach (var key in keysToRemove)
                    {
                        _questionRevealed.TryRemove(key, out _);
                    }

                    _activeGames.TryRemove(gameId, out _);
                    _gamePlayerUsernames.TryRemove(gameId, out _);

                    _logger.LogInformation($"Game {gameId} completely removed");
                }
                else
                {
                    await Clients.Group(gameId).SendAsync("PlayerLeft", new
                    {
                        playerId,
                        playerName = player.Name,
                        players = players.Select(p => new { p.Id, p.Name, p.IsActive })
                    });

                    _logger.LogInformation($"Notified remaining players in game {gameId} about player {playerId} leaving");
                }
            }
            else
            {
                _logger.LogWarning($"Player {playerId} not found in game {gameId}");
            }
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

                if (!_activeGames.TryGetValue(gameId, out var gameEngine))
                {
                    _logger.LogError($"ERROR: Game {gameId} not found in _activeGames");
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                Console.WriteLine($"Game found. Status: {gameEngine.Status}, Players: {gameEngine.GetPlayers().Count}");
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

            if (!_activeGames.TryGetValue(gameId, out var gameEngine))
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            var questionKey = $"{gameId}_{gameEngine.CurrentQuestion?.Id}";

            if (_questionRevealed.TryGetValue(questionKey, out var revealed) && revealed)
            {
                _logger.LogInformation($"Question already revealed for {questionKey}");
                await Clients.Caller.SendAsync("Error", "Question already completed");
                return;
            }

            // Get player's score before submitting answer
            var player = gameEngine.GetPlayers().FirstOrDefault(p => p.Id == playerId);
            var scoreBefore = player?.CurrentGameScore ?? 0;

            var result = gameEngine.SubmitAnswer(playerId, answerIndex);
            _logger.LogInformation($"Answer result: {result}");

            // Calculate earned points by comparing scores
            var scoreAfter = player?.CurrentGameScore ?? 0;
            var earnedPoints = scoreAfter - scoreBefore;

            await Clients.Caller.SendAsync("AnswerResult", new
            {
                result = result.ToString(),
                isCorrect = result == AnswerResult.Correct,
                earnedPoints = earnedPoints,
                correctAnswer = gameEngine.CurrentQuestion?.CorrectAnswerIndex
            });

            if (gameEngine.AllPlayersAnswered())
            {
                _logger.LogInformation($"All players answered for game {gameId}");

                if (_gameTimers.TryGetValue(gameId, out var cts))
                {
                    cts.Cancel();
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
            if (!_activeGames.TryGetValue(gameId, out var gameEngine))
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

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

            _questionRevealed.AddOrUpdate(questionKey, false, (key, oldValue) => false);

            if (_gameTimers.TryRemove(gameId, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }

            _logger.LogInformation($"Sending question {gameEngine.CurrentQuestionNumber} (ID: {question.Id}) to game {gameId}, TimeLimit: {question.TimeLimit}s");

            await Clients.Group(gameId).SendAsync("QuestionSent", new
            {
                questionNumber = gameEngine.CurrentQuestionNumber,
                questionText = question.QuestionText,
                answerOptions = question.AnswerOptions,
                category = question.Category.ToString(),
                difficulty = question.Difficulty.ToString(),
                timeLimit = question.TimeLimit,
                points = question.Points
            });

            var cancelTokenSource = new CancellationTokenSource();

            _gameTimers.TryAdd(gameId, cancelTokenSource);

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
                    if (_gameTimers.TryGetValue(gameId, out var storedCts) && storedCts == cancelTokenSource)
                    {
                        _gameTimers.TryRemove(gameId, out _);
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

            if (_questionRevealed.TryGetValue(questionKey, out var wasRevealed) && wasRevealed)
            {
                _logger.LogInformation($"Question {question.Id} already revealed for game {gameId}, skipping");
                return;
            }

            _questionRevealed.TryUpdate(questionKey, true, false);
            Console.WriteLine($"Revealing answer for question {question.Id} in game {gameId}");
            _questionRevealed[questionKey] = true;
            _logger.LogInformation($"Revealing answer for question {question.Id} in game {gameId}");

            var leaderboard = gameEngine.GetCurrentGameLeaderboard();

            await _staticHubContext.Clients.Group(gameId).SendAsync("QuestionRevealed", new
            {
                correctAnswer = question.CorrectAnswerIndex,
                correctAnswerText = question.AnswerOptions[question.CorrectAnswerIndex],
                leaderboard = leaderboard.Select(p => new
                {
                    p.Id,
                    p.Name,
                    score = p.CurrentGameScore,
                    correctAnswers = p.CorrectAnswersInGame
                })
            });

            _logger.LogInformation($"Sent AnswerRevealed to game {gameId}");

            Console.WriteLine($"Waiting {autoProgressMillisecodns} milliseconds before next question...");
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

            if (_gameTimers.TryRemove(gameId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            var keysToRemove = _questionRevealed.Keys.Where(k => k.StartsWith($"{gameId}_")).ToList();
            foreach (var key in keysToRemove)
            {
                _questionRevealed.TryRemove(key, out _);
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

            _activeGames.TryRemove(gameId, out _);
            _gamePlayerUsernames.TryRemove(gameId, out _);
            Console.WriteLine($"Game {gameId} ended and removed from active games");
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

                if (!_gamePlayerUsernames.TryGetValue(gameId, out var playerUsernames))
                {
                    _logger.LogError($"ERROR: No player usernames found for game {gameId}");
                    return;
                }

                Console.WriteLine($"Found {playerUsernames.Count} players in game");

                _logger.LogInformation($"Found {playerUsernames.Count} players in game");

                foreach (var gamePlayer in finalLeaderboard)
                {
                    _logger.LogInformation($"Processing player ID {gamePlayer.Id}...");

                    if (!playerUsernames.TryGetValue(gamePlayer.Id, out var username))
                    {
                        _logger.LogError($"ERROR: No username found for player ID {gamePlayer.Id}");
                        continue;
                    }

                    Console.WriteLine($"Looking up username: {username}");

                    _logger.LogInformation($"Looking up username: {username}");

                    var player = await _dbContext.Users
                        .OfType<Player>()
                        .FirstOrDefaultAsync(p => p.Username == username);

                    if (player == null)
                    {
                        _logger.LogError($"Player {username} not found");
                        throw new GameUpdateException($"Player {username} not found");
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
                throw new PlayerStatsUpdateException("Error while updating player statistics");
            }

        }

        /// <summary>
        /// Calculate elo difference after the game to change elo points
        /// </summary>
        /// <param name="player"></param>
        /// <param name="leaderboard"></param>
        /// <seealso href="https://en.wikipedia.org/wiki/Elo_rating_system"/>
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
            _logger.LogInformation($"=== OnDisconnectedAsync called for connection {Context.ConnectionId} ===");

            if (_playerGameMap.TryGetValue(Context.ConnectionId, out var gameId))
            {
                _logger.LogInformation($"Connection {Context.ConnectionId} was in game {gameId}");

                if (_activeGames.TryGetValue(gameId, out var gameEngine))
                {
                    var players = gameEngine.GetPlayers();

                    _logger.LogInformation($"Game {gameId} still active with {players.Count} players");

                    await Clients.Group(gameId).SendAsync("PlayerDisconnected", Context.ConnectionId);

                    if (gameEngine.Status == GameStatus.Waiting && players.Count == 1)
                    {
                        _logger.LogInformation($"Last player disconnected from waiting game {gameId}, cleaning up");

                        if (_gameTimers.TryRemove(gameId, out var cts))
                        {
                            cts.Cancel();
                            cts.Dispose();
                        }

                        _activeGames.TryRemove(gameId, out _);
                        _gamePlayerUsernames.TryRemove(gameId, out _);
                    }
                }

                _playerGameMap.TryRemove(Context.ConnectionId, out _);
            }
            else
            {
                _logger.LogInformation($"Connection {Context.ConnectionId} was not in any game");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }