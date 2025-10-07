using TriviaBackend.Models.Enums;

namespace TriviaBackend.Models.Objects
{
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
}
