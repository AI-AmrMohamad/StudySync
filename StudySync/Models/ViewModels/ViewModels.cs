using System.ComponentModel.DataAnnotations;

namespace StudySync.Models.ViewModels
{
    public class DashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public int TimeCredits { get; set; }
        public int FocusPoints { get; set; }
        public List<SwapBookingViewModel> UpcomingBookings { get; set; } = new();
    }

    public class SwapBookingViewModel
    {
        public int Id { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Requester" or "Provider"
        public int CreditCost { get; set; }
        public bool RequesterConfirmed { get; set; }
        public bool ProviderConfirmed { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateBookingViewModel
    {
        [Required]
        [Display(Name = "Skill")]
        public int SkillId { get; set; }

        [Required]
        [Display(Name = "Provider Email")]
        public string ProviderEmail { get; set; } = string.Empty;

        [Display(Name = "Credit Cost")]
        [Range(1, 10)]
        public int CreditCost { get; set; } = 1;

        public List<Skill> AvailableSkills { get; set; } = new();
    }
}
