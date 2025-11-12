using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TriviaBackend.Models.Enums;

namespace TriviaBackend.Models.Entities
{
    public class TriviaQuestionDTO
    {
        public string QuestionText { get; set; } = string.Empty;
        public string Answer1 { get; set; } = string.Empty;
        public string Answer2 { get; set; } = string.Empty;
        public string Answer3 { get; set; } = string.Empty;
        public string Answer4 { get; set; } = string.Empty;

        public int CorrectAnswerIndex { get; set; }
        public QuestionCategory Category { get; set; }
        public DifficultyLevel Difficulty { get; set; }

        public int TimeLimit { get; set; } = 30;
    }
}
