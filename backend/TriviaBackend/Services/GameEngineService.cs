using TriviaBackend.Exceptions;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Models.Records;

namespace TriviaBackend.Services
{
    /// <summary>
    /// Service for processing actions and events in a match. Works together with GameHub
    /// <paramref name="questionService"/>
    /// <paramref name="settings"/>
    /// <paramref name="gameId"/>
    /// </summary>
    public class GameEngineService(QuestionService questionService, ILogger<ExceptionHandler> logger, GameSettings settings = default, string? gameId = null)
    {
        private readonly QuestionService _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));
        private readonly ILogger<ExceptionHandler> _logger = logger;
        private List<GamePlayer> _players = new List<GamePlayer>();
        private Queue<TriviaQuestion> _gameQuestions = new Queue<TriviaQuestion>();
        private Dictionary<int, List<GameAnswer>> _gameAnswers = new Dictionary<int, List<GameAnswer>>();
        private TriviaQuestion? _currentQuestion;
        private DateTime _questionStartTime;
        private GameSettings _settings = settings.MaxPlayers == 0 ? new GameSettings() : settings;

        public GameStatus Status { get; private set; } = GameStatus.Waiting;
        public int CurrentQuestionNumber { get; private set; } = 0;
        public string GameId { get; private set; } = gameId ?? Guid.NewGuid().ToString();
        public TriviaQuestion? CurrentQuestion => _currentQuestion;

        public TimeSpan TimeRemaining => _currentQuestion != null ?
            TimeSpan.FromSeconds(_currentQuestion.TimeLimit) - (DateTime.Now - _questionStartTime) : TimeSpan.Zero;

        public GameSettings GetSettings() => _settings;

        public bool AddPlayer(string playerName, int? playerId = null, DateTime? joinTime = null)
        {
            if (_players.Count >= _settings.MaxPlayers)
                return false;

            if (Status == GameStatus.InProgress && !_settings.AllowLateJoining)
                return false;

            var player = new GamePlayer
            {
                Id = playerId ?? GeneratePlayerId(),
                Name = playerName,
                JoinedGameAt = joinTime ?? DateTime.Now,
                CurrentGameScore = 0,
                CorrectAnswersInGame = 0,
                IsActive = true
            };

            _players.Add(player);
            _gameAnswers[player.Id] = new List<GameAnswer>();
            return true;
        }

        public bool StartGame(QuestionCategory[]? categories = null, DifficultyLevel? maxDifficulty = null)
        {
            if (_players.Count == 0 || Status != GameStatus.Waiting)
                return false;

            try
            {
                var categoriesToUse = categories ?? _settings.QuestionCategories;
                var difficultyToUse = maxDifficulty ?? _settings.MaxDifficulty;

                var questions = _questionService.GetQuestions(categoriesToUse, difficultyToUse, _settings.QuestionsPerGame);

                _logger.LogInformation($"Requested {_settings.QuestionsPerGame} questions, got {questions?.Count ?? 0} questions");
                _logger.LogInformation($"Categories: {string.Join(", ", categoriesToUse.Select(c => c.ToString()))}");
                _logger.LogInformation($"Max Difficulty: {difficultyToUse}");

                if (questions == null || questions.Count == 0)
                {
                    Console.WriteLine("No questions returned from QuestionService");
                    _logger.LogError("No questions returned from QuestionService");
                    return false;
                }

                _gameQuestions.Clear();
                foreach (var question in questions)
                {
                    _gameQuestions.Enqueue(question);
                    _logger.LogInformation($"Enqueued question {question.Id}: {question.QuestionText} ({question.Category}, {question.Difficulty})");
                }

                Status = GameStatus.InProgress;
                CurrentQuestionNumber = 0;
                NextQuestion();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR starting game: {ex.Message}");
                throw new StartGameException("Game could not be started");
            }
        }

        public bool NextQuestion()
        {
            // Only enforce 5-second delay if we've already shown a question
            if (CurrentQuestionNumber > 0 && DateTime.Now - _questionStartTime < TimeSpan.FromSeconds(5))
                return true;

            if (_gameQuestions.Count == 0)
            {
                EndGame();
                return false;
            }

            _currentQuestion = _gameQuestions.Dequeue();
            _questionStartTime = DateTime.Now;
            CurrentQuestionNumber++;
            return true;
        }

        public AnswerResult SubmitAnswer(int playerId, int selectedAnswer)
        {
            if (_currentQuestion == null || Status != GameStatus.InProgress)
                return AnswerResult.TimeUp;

            var player = _players.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return AnswerResult.TimeUp;

            var submissionTime = DateTime.Now;

            if (submissionTime - _questionStartTime > TimeSpan.FromSeconds(_currentQuestion.TimeLimit))
            {
                return AnswerResult.TimeUp;
            }

            bool isCorrect = selectedAnswer == _currentQuestion.CorrectAnswerIndex;

            var timeBonus = isCorrect ? Math.Max(0, _currentQuestion.TimeLimit - (int)(submissionTime - _questionStartTime).TotalSeconds) : 0;
            var pointsEarned = isCorrect ? _currentQuestion.Points + timeBonus : 0;

            player.CurrentGameScore += pointsEarned;
            if (isCorrect) player.CorrectAnswersInGame++;

            var answer = new GameAnswer(playerId, _currentQuestion.Id, selectedAnswer, submissionTime);
            _gameAnswers[playerId].Add(answer);

            return isCorrect ? AnswerResult.Correct : AnswerResult.Incorrect;
        }

        public List<GamePlayer> GetCurrentGameLeaderboard()
        {
            var activePlayers = _players.Where(p => p.IsActive).ToList();
            activePlayers.Sort();
            return activePlayers;
        }

        public void EndGame()
        {
            Status = GameStatus.Finished;
            _currentQuestion = null;
        }

        public List<GamePlayer> GetPlayers()
        {
            return _players.ToList();
        }

        public Dictionary<int, List<GameAnswer>> GetGameAnswers()
        {
            return _gameAnswers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
        }

        private int GeneratePlayerId()
        {
            return _players.Count > 0 ? _players.Max(p => p.Id) + 1 : 1;
        }

        public bool AllPlayersAnswered()
        {
            if (_currentQuestion == null) return false;

            return _players.Where(p => p.IsActive)
                          .All(p => _gameAnswers[p.Id].Any(a => a.QuestionId == _currentQuestion.Id));
        }
    }
}