namespace TriviaBackend.Models.Objects
{
    public class GamePlayer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CurrentScore { get; set; } = 0;
        public int CorrectAnswers { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime JoinedGameAt { get; set; }
        public int CurrentGameScore { get; set; } = 0;
        public int CorrectAnswersInGame { get; set; } = 0;
    }
}
