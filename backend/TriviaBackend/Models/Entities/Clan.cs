using System.ComponentModel.DataAnnotations;

namespace TriviaBackend.Models.Entities
{
    public class Clan
    {
        [Key]
        public int Id { get; set; }
        public int Name { get; set; }
        public List<string> MemberIds { get; set; } = [];
    }
}
