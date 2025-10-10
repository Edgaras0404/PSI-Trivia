namespace TriviaBackend.Models.Entities
{
    public class Player : BaseUser
    {
        public int Elo { get; set; }
        public int GamesPlayed { get; set; }
    }
}
