using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TriviaBackend.Models;

namespace TriviaBackend.Services
{
    public class QuestionService
    {
        private List<TriviaQuestion> _questionBank;

        public QuestionService()
        {
            _questionBank = new List<TriviaQuestion>();
            LoadDefaultQuestions();
        }

        // Data load from file
        public bool LoadQuestionsFromFile(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var reader = new StreamReader(stream);

                string jsonContent = reader.ReadToEnd();
                var questions = JsonSerializer.Deserialize<List<TriviaQuestion>>(jsonContent);

                if (questions != null)
                {
                    _questionBank.AddRange(questions);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading questions from file: {ex.Message}");
            }
            return false;
        }

        // Question filtering and retrieval (using LINQ)
        public List<TriviaQuestion> GetQuestions(QuestionCategory[] categories = null,
                                               DifficultyLevel? maxDifficulty = null,
                                               int count = 10)
        {
            var query = _questionBank.AsEnumerable();

            if (categories != null && categories.Length > 0)
            {
                query = query.Where(q => categories.Contains(q.Category));
            }

            if (maxDifficulty.HasValue)
            {
                query = query.Where(q => q.Difficulty <= maxDifficulty.Value);
            }

            // Randomize and take specified question count
            return query.OrderBy(q => Guid.NewGuid())
                       .Take(count)
                       .ToList();
        }

        // Collections usage
        public Dictionary<QuestionCategory, int> GetQuestionCountByCategory()
        {
            var categoryCounts = new Dictionary<QuestionCategory, int>();

            foreach (var category in Enum.GetValues<QuestionCategory>())
            {
                categoryCounts[category] = 0;
            }

            foreach (var question in _questionBank)
            {
                categoryCounts[question.Category]++;
            }

            return categoryCounts;
        }

        public void AddQuestion(TriviaQuestion question)
        {
            question.Id = _questionBank.Count > 0 ? _questionBank.Max(q => q.Id) + 1 : 1;
            _questionBank.Add(question);
        }

        private void LoadDefaultQuestions()
        {
            var defaultQuestions = new List<TriviaQuestion>
            {
                new TriviaQuestion
                {
                    Id = 1,
                    Text = "What is the capital of Finland?",
                    Options = new List<string> { "Stockholm", "Vilnius", "Helsinki", "Madrid" },
                    CorrectAnswerIndex = 2,
                    Category = QuestionCategory.Geography,
                    Difficulty = DifficultyLevel.Easy
                },
                new TriviaQuestion
                {
                    Id = 2,
                    Text = "Who wrote 'Romeo and Juliet'?",
                    Options = new List<string> { "Charles Dickens", "William Shakespeare", "Jane Austen", "Mark Twain" },
                    CorrectAnswerIndex = 1,
                    Category = QuestionCategory.Literature,
                    Difficulty = DifficultyLevel.Medium
                },
                new TriviaQuestion
                {
                    Id = 3,
                    Text = "What is the chemical symbol for helium?",
                    Options = new List<string> { "Pb", "O", "He", "Ag" },
                    CorrectAnswerIndex = 2,
                    Category = QuestionCategory.Science,
                    Difficulty = DifficultyLevel.Hard
                },
                new TriviaQuestion
                {
                    Id = 4,
                    Text = "In which year did World War II end?",
                    Options = new List<string> { "1944", "1945", "1946", "1947" },
                    CorrectAnswerIndex = 1,
                    Category = QuestionCategory.History,
                    Difficulty = DifficultyLevel.Medium
                },
                new TriviaQuestion
                {
                    Id = 5,
                    Text = "Which sport is played at Wimbledon?",
                    Options = new List<string> { "Football", "Cricket", "Tennis", "Golf" },
                    CorrectAnswerIndex = 2,
                    Category = QuestionCategory.Sports,
                    Difficulty = DifficultyLevel.Easy
                }
            };

            _questionBank.AddRange(defaultQuestions);
        }
    }
}