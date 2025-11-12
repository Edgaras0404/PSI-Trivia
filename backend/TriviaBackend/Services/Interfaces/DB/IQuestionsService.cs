using TriviaBackend.Models.Entities;

namespace TriviaBackend.Services.Interfaces.DB
{
    public interface IQuestionsService
    {
        Task<TriviaQuestion?> GetQuestionByIdAsync(int id);

        Task AddQuestionAsync(TriviaQuestionDTO question);

        Task DeleteQuestionByIdAsync(int id);

    }
}
