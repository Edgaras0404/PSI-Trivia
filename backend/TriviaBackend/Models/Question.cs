namespace TriviaBackend.Models
{
    public enum QuestionCategory
    {
        Science,
        History,
        Sports,
        Geography
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
    public enum AnswerStatus
    {
        Correct,
        Incorrect,
        Timesup
    }
    public class TriviaQuestion
    {
        public int Id { get; set; }
        public string QuestionText { get; set; }
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
    public record PlayerAnswer(int QuestionId, int SelectedOptionIndex, AnswerStatus Status, int PointsEarned);

    public struct GameSettings
    {
        public int MaxPlayers { get; set; }
        public int QuestionsPerGame { get; set; }
        public int DefaultTimeLimit { get; set; }
        public bool AllowLateJoining { get; set; }
        public QuestionCategory[] questionCategories { get; set; }

        public GameSettings(int MaxPlayers = 10, int QuestionsPerGame = 10, int DefaultTimeLimit = 30)
        {
            MaxPlayers = MaxPlayers;
            QuestionsPerGame = QuestionsPerGame;
            DefaultTimeLimit = DefaultTimeLimit;
            AllowLateJoining = false;
            questionCategories = Enum.GetValues<QuestionCategory>();
        }
        public class GamePlayer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int CurrentScore { get; set; }
            public int CorrectAnswers { get; set; }
            public bool IsActive { get; set; }
        }

    }
}