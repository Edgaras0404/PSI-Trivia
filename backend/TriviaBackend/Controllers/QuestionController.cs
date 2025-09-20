using Microsoft.AspNetCore.Mvc;
using TriviaBackend.Models;

namespace TriviaBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuestionController : ControllerBase
    {
        private readonly ILogger<QuestionController> _logger;

        public QuestionController(ILogger<QuestionController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "Test")]
        public ActionResult<Question> GetTestQuestion()
        {
            Question testQuestion = new()
            {
                questionText = "How many rs in \'strawberry\'",
                answerText = "3",
                reward = 5
            };
            return Ok(testQuestion);
        }
    }
}
