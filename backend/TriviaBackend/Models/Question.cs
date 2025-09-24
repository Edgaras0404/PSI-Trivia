namespace TriviaBackend.Models
{
    public enum QuestionCategory
    {
        Science,
        History,
        Sports,
        Geography,
        Literature
    }

    public enum DifficultyLevel
    {
        Easy = 1,
        Medium = 2,
        Hard = 3
    }

    public enum GameStatus
    {
        Waiting,
        InProgress,
        Finished,
        Paused
    }

    public enum AnswerResult
    {
        Correct,
        Incorrect,
        TimeUp
    }

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

    public record GameAnswer(int PlayerId, int QuestionId, int SelectedOptionIndex, DateTime SubmissionTime);

    public record PlayerAnswer(int QuestionId, int SelectedOptionIndex, AnswerResult Status, int PointsEarned);

    public struct GameSettings
    {
        public int MaxPlayers { get; set; }
        public int QuestionsPerGame { get; set; }
        public int DefaultTimeLimit { get; set; }
        public bool AllowLateJoining { get; set; }
        public QuestionCategory[] QuestionCategories { get; set; }

        public GameSettings(int MaxPlayers = 10, int QuestionsPerGame = 10, int DefaultTimeLimit = 30)
        {
            this.MaxPlayers = MaxPlayers;
            this.QuestionsPerGame = QuestionsPerGame;
            this.DefaultTimeLimit = DefaultTimeLimit;
            this.AllowLateJoining = false;
            this.QuestionCategories = Enum.GetValues<QuestionCategory>();
        }
    }

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