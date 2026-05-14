using System.Collections.Generic;

namespace StudySync.Models.ViewModels
{
    public class HomeDashboardViewModel
    {
        public ApplicationUser CurrentUser { get; set; } = null!;
        public List<UpcomingSessionViewModel> UpcomingSessions { get; set; } = new List<UpcomingSessionViewModel>();
        public List<CommunityChannel> JoinedChannels { get; set; } = new List<CommunityChannel>();
    }
}
