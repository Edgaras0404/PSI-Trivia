using TriviaBackend.Models.Enums;

namespace TriviaBackend.Models.Objects
{
    public class TriviaQuestion
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; }
        public int CorrectAnswerIndex { get; set; }
        public QuestionCategory Category { get; set; }
        public DifficultyLevel Difficulty { get; set; }
        public int TimeLimit { get; set; } = 30;
        public int Points => (int)Difficulty * 10;

        public TriviaQuestion()
        {
            Options = new List<string>();
        }
    }
}
