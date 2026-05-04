using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public enum ChannelTier
    {
        Global,
        University,
        Major,
        Subject
    }

    public class CommunityChannel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // e.g. "#global-lounge"
        
        public string Description { get; set; } = string.Empty;

        public ChannelTier Tier { get; set; } = ChannelTier.Subject;

        // E.g. "NYU" for Uni tier, "Computer Science" for Major tier. Empty for Global.
        public string AssociatedEntityName { get; set; } = string.Empty;

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
