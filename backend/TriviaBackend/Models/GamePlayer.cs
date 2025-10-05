using System;

namespace TriviaBackend.Models
{
    public class GamePlayer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CurrentGameScore { get; set; }
        public int CorrectAnswersInGame { get; set; }
        public bool IsActive { get; set; }
        public DateTime JoinedGameAt { get; set; }
    }

    public record GameAnswer(
        int PlayerId,
        int QuestionId, 
        int SelectedAnswer,
        DateTime SubmittedAt
    );

    public enum AnswerResult
    {
        Correct,
        Incorrect,
        TimeUp
    }
}
