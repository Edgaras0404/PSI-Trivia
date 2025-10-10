using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TriviaBackend.Models.Enums;
using TriviaBackend.Models.Records;
using TriviaBackend.Models.Entities;
using TriviaBackend.Data;
using Microsoft.AspNetCore.Mvc;

namespace TriviaBackend.Services
{
    public class QuestionService
    {
        private readonly TriviaDbContext _dbContext;
        private List<TriviaQuestion> _questionBank;

        public QuestionService(TriviaDbContext dbContext)
        {
            _dbContext = dbContext;
            _questionBank = new List<TriviaQuestion>();
            //LoadDefaultQuestions();
            LoadDBQuestions();
        }

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

        public List<TriviaQuestion> GetQuestions(QuestionCategory[]? categories = null,
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

            return query.OrderBy(q => Guid.NewGuid())
                       .Take(count)
                       .ToList();
        }

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
                    QuestionText = "What is the capital of Finland?",
                    Options = new List<string> { "Stockholm", "Vilnius", "Helsinki", "Madrid" },
                    CorrectAnswerIndex = 2,
                    Category = QuestionCategory.Geography,
                    Difficulty = DifficultyLevel.Easy
                },
                new TriviaQuestion
                {
                    Id = 2,
                    QuestionText = "Who wrote 'Romeo and Juliet'?",
                    Options = new List<string> { "Charles Dickens", "William Shakespeare", "Jane Austen", "Mark Twain" },
                    CorrectAnswerIndex = 1,
                    Category = QuestionCategory.Literature,
                    Difficulty = DifficultyLevel.Medium
                },
                new TriviaQuestion
                {
                    Id = 3,
                    QuestionText = "What is the chemical symbol for helium?",
                    Options = new List<string> { "Pb", "O", "He", "Ag" },
                    CorrectAnswerIndex = 2,
                    Category = QuestionCategory.Science,
                    Difficulty = DifficultyLevel.Hard
                },
                new TriviaQuestion
                {
                    Id = 4,
                    QuestionText = "In which year did World War II end?",
                    Options = new List<string> { "1944", "1945", "1946", "1947" },
                    CorrectAnswerIndex = 1,
                    Category = QuestionCategory.History,
                    Difficulty = DifficultyLevel.Medium
                },
                new TriviaQuestion
                {
                    Id = 5,
                    QuestionText = "Which sport is played at Wimbledon?",
                    Options = new List<string> { "Football", "Cricket", "Tennis", "Golf" },
                    CorrectAnswerIndex = 2,
                    Category = QuestionCategory.Sports,
                    Difficulty = DifficultyLevel.Easy
                }
            };

            _questionBank.AddRange(defaultQuestions);
        }

        private void LoadDBQuestions()
        {
            var questions = _dbContext.Questions.ToList();
            _questionBank.AddRange(questions);
        }
    }
}