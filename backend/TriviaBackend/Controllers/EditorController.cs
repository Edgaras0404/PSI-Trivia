using Microsoft.AspNetCore.Mvc;
using TriviaBackend.Models.Entities;
using TriviaBackend.Services;

namespace TriviaBackend.Controllers
{
    /// <summary>
    /// Controller for managing trivia question editing
    /// </summary>
    /// <param name="_DBService"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class EditorController(DBQuestionsService _DBService) : ControllerBase
    {
        [HttpGet("getquestion/{id}")]
        public async Task<ActionResult<TriviaQuestion>> GetQuestion(int id)
        {
            var question = await _DBService.GetQuestionByIdAsync(id);

            if (question == null)
            {
                return NotFound();
            }

            return Ok(question);
        }

        [HttpPost("addquestion")]
        public async Task AddQuestion(TriviaQuestionDTO request)
        {
            await _DBService.AddQuestionAsync(request);
        }

        [HttpDelete("deletequestion/{id}")]
        public async Task DeleteQuestion(int id)
        {
            await _DBService.DeleteQuestionByIdAsync(id);
        }
    }
}
