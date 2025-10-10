using Microsoft.AspNetCore.Routing.Constraints;

namespace TriviaBackend.Models.Entities
{
    public class Admin : BaseUser
    {
        public bool CanKickUsers { get; set; } = true;
        public bool CanEditTrivias { get; set; } = true;
    }
}
