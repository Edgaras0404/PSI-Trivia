namespace TriviaBackend.Models
{
    public class Question
    {
        public required string questionText { get; set; }
        public required string answerText { get; set; }

        public int reward { get; set; }
    }

}
