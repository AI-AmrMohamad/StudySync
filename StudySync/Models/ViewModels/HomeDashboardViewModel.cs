using System.Collections.Generic;

namespace StudySync.Models.ViewModels
{
    public class HomeDashboardViewModel
    {
        public ApplicationUser CurrentUser { get; set; } = null!;
        public List<FocusRoom> LiveRooms { get; set; } = new List<FocusRoom>();
        public List<HelpBounty> OpenJobs { get; set; } = new List<HelpBounty>();
        public List<CommunityChannel> JoinedChannels { get; set; } = new List<CommunityChannel>();
    }
}
