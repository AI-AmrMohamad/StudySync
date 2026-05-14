using System.ComponentModel.DataAnnotations;

namespace StudySync.Models.ViewModels
{
    // ── Legacy ViewModels (kept for backward compat) ──

    public class DashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public int TimeCredits { get; set; }
        public int FocusPoints { get; set; }
        public List<DashboardSessionViewModel> UpcomingSessions { get; set; } = new();
        public List<DashboardSessionViewModel> MySessions { get; set; } = new();
    }

    public class SwapBookingViewModel
    {
        public int Id { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
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

    // ── Live Sessions ViewModels ──

    public class TutorSessionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SkillName { get; set; } = string.Empty;
        public string SkillCategory { get; set; } = string.Empty;
        public string TutorName { get; set; } = string.Empty;
        public string TutorId { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; }
        public int CreditCost { get; set; }
        public int MaxAttendees { get; set; }
        public int EnrolledCount { get; set; }
        public SessionStatus Status { get; set; }
        public bool IsEnrolled { get; set; }
        public bool IsTutor { get; set; }
    }

    public class CreateSessionViewModel
    {
        [Required]
        [Display(Name = "Subject")]
        public int SkillId { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Session Date & Time")]
        public DateTime ScheduledAt { get; set; } = DateTime.Now.AddDays(1);

        [Display(Name = "Duration")]
        [Range(15, 180)]
        public int DurationMinutes { get; set; } = 60;

        [Display(Name = "Credit Cost")]
        [Range(1, 10)]
        public int CreditCost { get; set; } = 1;

        [Display(Name = "Max Attendees")]
        [Range(1, 50)]
        public int MaxAttendees { get; set; } = 10;

        public List<Skill> AvailableSkills { get; set; } = new();
    }

    public class SessionRoomViewModel
    {
        public int SessionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SkillName { get; set; } = string.Empty;
        public string TutorName { get; set; } = string.Empty;
        public string TutorId { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; }
        public SessionStatus Status { get; set; }
        public bool IsTutor { get; set; }
        public bool IsEnrolled { get; set; }
        public int EnrolledCount { get; set; }
        public List<string> AttendeeNames { get; set; } = new();
    }

    public class DashboardSessionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SkillName { get; set; } = string.Empty;
        public string TutorName { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public int CreditCost { get; set; }
        public SessionStatus Status { get; set; }
        public string Role { get; set; } = string.Empty; // "Tutor" or "Student"
        public int EnrolledCount { get; set; }
        public int MaxAttendees { get; set; }
    }
}
