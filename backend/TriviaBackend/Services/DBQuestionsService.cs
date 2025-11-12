using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace TriviaBackend.Services
{
    public class DBQuestionsService(TriviaDbContext context)
    {
        private readonly TriviaDbContext _context = context;

        public async Task<TriviaQuestion?> GetQuestionByIdAsync(int id) =>
            await _context.Questions.FindAsync(id);

        public async Task AddQuestionAsync(TriviaQuestionDTO question)
        {
            _context.Questions.Add(new TriviaQuestion
            {
                QuestionText = question.QuestionText,
                CorrectAnswerIndex = question.CorrectAnswerIndex,
                AnswerOptions = [question.Answer1, question.Answer2, question.Answer3, question.Answer4],
                Category = question.Category,
                Difficulty = question.Difficulty,
                TimeLimit = question.TimeLimit
            });
            await _context.SaveChangesAsync();
        }

        public async Task DeleteQuestionByIdAsync(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question != null)
            {
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
            }
        }
    }
}
