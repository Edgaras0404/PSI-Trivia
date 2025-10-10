using System.ComponentModel.DataAnnotations;

namespace TriviaBackend.Models.Entities
{
    public class BaseUser
    {
        [Key]
        public string ID { get; set; } = Guid.NewGuid().ToString();

        public DateTime Created { get; set; } = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;
    }
}
