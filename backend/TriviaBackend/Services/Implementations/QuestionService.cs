using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Services.Interfaces;

namespace TriviaBackend.Services.Implementations
{
    /// <summary>
    /// Service for working with questions in the ongoing trivia match 
    /// </summary>
    public class QuestionService(ITriviaDbContext dbContext) : IQuestionService
    {
        private readonly ITriviaDbContext _dbContext = dbContext;

        /// <summary>
        /// Get filtered questions
        /// </summary>
        /// <param name="categories"></param>
        /// <param name="maxDifficulty"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<TriviaQuestion> GetQuestions(QuestionCategory[]? categories = null,
                                               DifficultyLevel? maxDifficulty = null,
                                               int count = 6)
        {
            var query = _dbContext.Questions.AsEnumerable();

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

        /// <summary>
        /// Get the amount of questions that have the category in question
        /// </summary>
        /// <returns></returns>
        public Dictionary<QuestionCategory, int> GetQuestionCountByCategory()
        {
            var categoryCounts = new Dictionary<QuestionCategory, int>();

            foreach (var category in Enum.GetValues<QuestionCategory>())
            {
                categoryCounts[category] = 0;
            }
            var query = _dbContext.Questions.ToList();

            foreach (var question in query)
            {
                categoryCounts[question.Category]++;
            }

            return categoryCounts;
        }
    }
}