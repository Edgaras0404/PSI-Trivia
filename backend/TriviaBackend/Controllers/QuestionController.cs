using Microsoft.AspNetCore.Mvc;
using TriviaBackend.Models;

namespace TriviaBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuestionController(ILogger<QuestionController> logger) : ControllerBase
    {
        private readonly ILogger<QuestionController> _logger = logger;

        [HttpGet("{ID}")]
        public ActionResult<Question> GetQuestionByID(int ID)
        {
            List<Question> testQuestions = [
                new(){
                    questionText = "How many rs in \'strawberry\'",
                    answerText = "3",
                    reward = 5
                },
                new(){
                    questionText = "1 + 1",
                    answerText = "2",
                    reward = 3
                },
                new(){
                    questionText = "If a woodchuck could chuck wood how much wood would a woodchuck chuck",
                    answerText = "2 metric tons",
                    reward = 10
                },
            ];

            if (ID < 0 || ID >= testQuestions.Count)
            {
                return NotFound();
            }

            return Ok(testQuestions[ID]);
        }
    }
}
