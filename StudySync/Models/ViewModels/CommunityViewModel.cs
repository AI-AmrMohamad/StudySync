using StudySync.Models;

namespace StudySync.Models.ViewModels
{
    public class CommunityViewModel
    {
        public ApplicationUser CurrentUser { get; set; } = null!;
        public List<CommunityChannel> GlobalChannels { get; set; } = new List<CommunityChannel>();
        public List<CommunityChannel> UniversityChannels { get; set; } = new List<CommunityChannel>();
        public List<CommunityChannel> MajorChannels { get; set; } = new List<CommunityChannel>();
        public CommunityChannel ActiveChannel { get; set; } = null!;
        public List<ChatMessage> RecentMessages { get; set; } = new List<ChatMessage>();
    }
}
