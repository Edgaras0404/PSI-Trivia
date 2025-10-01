using Microsoft.AspNetCore.Mvc;
using TriviaBackend.Models;
using TriviaBackend.Services;

namespace TriviaBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly GameEngineService _gameEngine;
        private readonly QuestionService _questionService;

        public GameController()
        {
            _questionService = new QuestionService();
            var settings = new GameSettings(MaxPlayers: 5, QuestionsPerGame: 10, DefaultTimeLimit: 20);
            _gameEngine = new GameEngineService(_questionService, settings);
        }

        // POST: api/game/start
        [HttpPost("start")]
        public IActionResult StartGame([FromBody] StartGameRequest request)
        {
            try
            {
                QuestionCategory[]? selectedCategories = null;
                if (request.Categories != null && request.Categories.Length > 0)
                {
                    selectedCategories = request.Categories;
                }

                DifficultyLevel? maxDifficulty = request.MaxDifficulty;

                if (_gameEngine.StartGame(selectedCategories, maxDifficulty))
                {
                    return Ok(new { message = "Game started successfully!", status = _gameEngine.Status });
                }

                return BadRequest(new { message = "Failed to start game!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // POST: api/game/players
        [HttpPost("players")]
        public IActionResult AddPlayer([FromBody] AddPlayerRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Name))
                {
                    return BadRequest(new { message = "Player name is required" });
                }

                if (_gameEngine.AddPlayer(request.Name))
                {
                    var players = _gameEngine.GetPlayers();
                    return Ok(new
                    {
                        message = $"Player '{request.Name}' added successfully",
                        players = players.Select(p => new { p.Id, p.Name, p.IsActive })
                    });
                }

                return BadRequest(new { message = "Could not add player (game full or already started)" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // GET: api/game/players
        [HttpGet("players")]
        public IActionResult GetPlayers()
        {
            try
            {
                var players = _gameEngine.GetPlayers();
                return Ok(players.Select(p => new { p.Id, p.Name, p.IsActive, p.CurrentScore }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // GET: api/game/status
        [HttpGet("status")]
        public IActionResult GetGameStatus()
        {
            try
            {
                return Ok(new
                {
                    status = _gameEngine.Status,
                    currentQuestionNumber = _gameEngine.CurrentQuestionNumber,
                    totalQuestions = _gameEngine.Settings.QuestionsPerGame,
                    maxPlayers = _gameEngine.Settings.MaxPlayers
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // GET: api/game/current-question
        [HttpGet("current-question")]
        public IActionResult GetCurrentQuestion()
        {
            try
            {
                var question = _gameEngine.CurrentQuestion;
                if (question == null)
                {
                    return NotFound(new { message = "No current question available" });
                }

                return Ok(new
                {
                    questionNumber = _gameEngine.CurrentQuestionNumber,
                    category = question.Category,
                    difficulty = question.Difficulty,
                    points = question.Points,
                    timeLimit = question.TimeLimit,
                    text = question.QuestionText,
                    options = question.Options.Select((option, index) => new { index = index + 1, text = option })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // POST: api/game/answer
        [HttpPost("answer")]
        public IActionResult SubmitAnswer([FromBody] SubmitAnswerRequest request)
        {
            try
            {
                if (request.PlayerId <= 0 || request.AnswerIndex < 0)
                {
                    return BadRequest(new { message = "Invalid player ID or answer index" });
                }

                var result = _gameEngine.SubmitAnswer(request.PlayerId, request.AnswerIndex);
                var question = _gameEngine.CurrentQuestion;

                return Ok(new
                {
                    result = result.ToString(),
                    isCorrect = result == AnswerResult.Correct,
                    correctAnswer = question?.CorrectAnswerIndex + 1,
                    correctAnswerText = question?.Options[question.CorrectAnswerIndex]
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // POST: api/game/next-question
        [HttpPost("next-question")]
        public IActionResult NextQuestion()
        {
            try
            {
                bool hasNextQuestion = _gameEngine.NextQuestion();

                return Ok(new
                {
                    hasNextQuestion = hasNextQuestion,
                    gameStatus = _gameEngine.Status,
                    currentQuestionNumber = _gameEngine.CurrentQuestionNumber
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // GET: api/game/leaderboard
        [HttpGet("leaderboard")]
        public IActionResult GetLeaderboard()
        {
            try
            {
                var leaderboard = _gameEngine.GetCurrentGameLeaderboard();

                return Ok(leaderboard.Select((player, index) => new
                {
                    rank = index + 1,
                    playerId = player.Id,
                    playerName = player.Name,
                    score = player.CurrentScore,
                    correctAnswers = player.CorrectAnswers,
                    medal = index switch
                    {
                        0 => "🥇",
                        1 => "🥈",
                        2 => "🥉",
                        _ => ""
                    }
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // GET: api/game/categories
        [HttpGet("categories")]
        public IActionResult GetAvailableCategories()
        {
            try
            {
                var categoryStats = _questionService.GetQuestionCountByCategory();
                return Ok(categoryStats.Select(kvp => new
                {
                    category = kvp.Key,
                    questionCount = kvp.Value
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }

    // Request/Response models
    public class StartGameRequest
    {
        public QuestionCategory[]? Categories { get; set; }
        public DifficultyLevel? MaxDifficulty { get; set; }
    }

    public class AddPlayerRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class SubmitAnswerRequest
    {
        public int PlayerId { get; set; }
        public int AnswerIndex { get; set; }
    }
}