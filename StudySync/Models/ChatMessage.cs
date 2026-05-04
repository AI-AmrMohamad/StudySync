using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int CommunityChannelId { get; set; }
        public CommunityChannel Channel { get; set; } = null!;
    }
}
