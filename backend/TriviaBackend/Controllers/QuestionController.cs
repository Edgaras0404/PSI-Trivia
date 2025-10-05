using Microsoft.AspNetCore.Mvc;
using TriviaBackend.Models;
using TriviaBackend.Services;

namespace TriviaBackend.Controllers
{
    public class GameController : Controller
    {
        private readonly GameEngine _gameEngine;
        private readonly QuestionService _questionService;
        private readonly GameSettings _settings;

        public GameController()
        {
            _settings = new GameSettings(MaxPlayers: 5, QuestionsPerGame: 10, DefaultTimeLimit: 20);
            _questionService = new QuestionService();
            _gameEngine = new GameEngine(_questionService, _settings);
        }

        public void StartInteractiveGame()
        {
            Console.WriteLine("== Welcome to the Trivia Game! ==");

            RegisterPlayers();

            if (_gameEngine.GetPlayers().Count == 0)
            {
                Console.WriteLine("No players registered. Exiting game......");
                return;
            }

            SetupGame();
            PlayGame();
            ShowResults();
        }
        private void RegisterPlayers()
        {
            Console.WriteLine("\n--- Player Registration ---");
            Console.WriteLine("Enter player names (press Enter with empty name to finish):");

            while (true)
            {
                Console.Write($"Enter player name (max {_settings.MaxPlayers} players): ");
                string name = Console.ReadLine().Trim();

                if (string.IsNullOrEmpty(name))
                    break;

                if (_gameEngine.AddPlayer(name))
                {
                    Console.WriteLine($"Player '(name)' added successfully. ");
                }
                else
                {
                    Console.WriteLine("Could not add player (game full or already started)");
                    break;
                }
            }
            var players = _gameEngine.GetPlayers();
            Console.WriteLine($"\nRegistered players: {string.Join(", ", players.Select(p => p.Name))}");
        }
        private void SetupGame()
        {
            Console.WriteLine("\n--- Game Setup ---");
            var categoryStats = _questionService.GetQuestionCountByCategory();
            Console.WriteLine("Available Categories:");

            foreach (var category in categoryStats)
            {
                Console.WriteLine($"- {category.Key} ({category.Value} questions)");
            }

            var players = _gameEngine.GetPlayers();
            Console.WriteLine($"\nRegistered players: {string.Join(", ", players.Select(p => p.Name))}");

            // FIX: Prompt for category input and assign to categoryInput
            Console.Write("Select categories (comma-separated, or press Enter for all): ");
            string categoryInput = Console.ReadLine()?.Trim();

            QuestionCategory[] selectedCategories = null;
            if (!string.IsNullOrEmpty(categoryInput))
            {
                selectedCategories = ParseCategories(categoryInput);
            }

            Console.Write("Select max difficulty (Easy/Medium/Hard) or press Enter for all: ");
            string difficultyInput = Console.ReadLine()?.Trim();

            DifficultyLevel? maxDifficulty = null;
            if (!string.IsNullOrEmpty(difficultyInput) &&
                Enum.TryParse<DifficultyLevel>(difficultyInput, true, out var difficulty))
            {
                maxDifficulty = difficulty;
            }

            if (_gameEngine.StartGame(selectedCategories, maxDifficulty))
            {
                Console.WriteLine("Game started successfully!");
            }
            else
            {
                Console.WriteLine("Failed to start game!");
            }
        }
        private void PlayGame()
        {
            Console.WriteLine("\n--- Game Started ---");

            while (_gameEngine.Status == GameStatus.InProgress)
            {
                DisplayCurrentQuestion();
                CollectAnswers();

                if (!_gameEngine.NextQuestion())
                {
                    break;
                }

                ShowCurrentScores();
                Console.WriteLine("\nPress Enter to continue to next question...");
                Console.ReadLine();
            }
        }
        private void DisplayCurrentQuestion()
        {
            var question = _gameEngine.CurrentQuestion;
            if (question == null) return;

            Console.Clear();
            Console.WriteLine($"\n=== Question {_gameEngine.CurrentQuestionNumber} ===");
            Console.WriteLine($"Category: {question.Category} | Difficulty: {question.Difficulty}");
            Console.WriteLine($"Points: {question.Points} | Time Limit: {question.TimeLimit}s");
            Console.WriteLine($"\n{question.QuestionText}");

            for (int i = 0; i < question.Options.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {question.Options[i]}");
            }
            Console.WriteLine();
        }

        private void CollectAnswers()
        {
            var players = _gameEngine.GetPlayers().Where(p => p.IsActive).ToList();
            var startTime = DateTime.Now;
            var timeLimit = _gameEngine.CurrentQuestion.TimeLimit;

            foreach (var player in players)
            {
                var timeElapsed = (DateTime.Now - startTime).TotalSeconds;
                var remainingTime = Math.Max(0, timeLimit - (int)timeElapsed);

                if (remainingTime <= 0)
                {
                    Console.WriteLine($"Time up for {player.Name}!");
                    continue;
                }

                Console.Write($"{player.Name}, enter your answer (1-{_gameEngine.CurrentQuestion.Options.Count}) " +
                             $"[{remainingTime}s remaining]: ");

                string input = Console.ReadLine()?.Trim();

                if (int.TryParse(input, out int answer) && answer >= 1 && answer <= _gameEngine.CurrentQuestion.Options.Count)
                {
                    var result = _gameEngine.SubmitAnswer(player.Id, answer - 1);

                    switch (result)
                    {
                        case AnswerResult.Correct:
                            Console.WriteLine("✓ Correct!");
                            break;
                        case AnswerResult.Incorrect:
                            Console.WriteLine("✗ Incorrect!");
                            break;
                        case AnswerResult.TimeUp:
                            Console.WriteLine("⏰ Time's up!");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid answer!");
                }
            }

            var correctOption = _gameEngine.CurrentQuestion.Options[_gameEngine.CurrentQuestion.CorrectAnswerIndex];
            Console.WriteLine($"\nCorrect answer: {_gameEngine.CurrentQuestion.CorrectAnswerIndex + 1}. {correctOption}");
        }

        private void ShowCurrentScores()
        {
            Console.WriteLine("\n--- Current Scores ---");
            var leaderboard = _gameEngine.GetCurrentGameLeaderboard();

            for (int i = 0; i < leaderboard.Count; i++)
            {
                var player = leaderboard[i];
                Console.WriteLine($"{i + 1}. {player.Name}: {player.CurrentGameScore} points " +
                                $"({player.CorrectAnswersInGame} correct)");
            }
        }

        private void ShowResults()
        {
            Console.WriteLine("\n=== GAME FINISHED ===");
            Console.WriteLine("Final Results:");

            var finalLeaderboard = _gameEngine.GetCurrentGameLeaderboard();

            for (int i = 0; i < finalLeaderboard.Count; i++)
            {
                var player = finalLeaderboard[i];
                string medal = i switch
                {
                    0 => "🥇",
                    1 => "🥈",
                    2 => "🥉",
                    _ => "  "
                };

                Console.WriteLine($"{medal} {i + 1}. {player.Name}");
                Console.WriteLine($"     Score: {player.CurrentGameScore} points");
                Console.WriteLine($"     Correct Answers: {player.CorrectAnswersInGame}");
                Console.WriteLine();
            }

            if (finalLeaderboard.Count > 0)
            {
                Console.WriteLine($"🎉 Congratulations {finalLeaderboard[0].Name}! You won!");
            }
        }

        private QuestionCategory[] ParseCategories(string input)
        {
            var categories = new List<QuestionCategory>();
            var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (Enum.TryParse<QuestionCategory>(part.Trim(), true, out var category))
                {
                    categories.Add(category);
                }
            }

            return categories.ToArray();
        }
    }
}

