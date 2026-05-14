using System.ComponentModel.DataAnnotations;

namespace StudySync.Models.ViewModels
{
<<<<<<< HEAD
    // ── Legacy ViewModels (kept for backward compat) ──

=======
    // ── Dashboard ──────────────────────────────────────────────────────
>>>>>>> 76461639f11a644f5445e0d487f22318f27277d5
    public class DashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public int TimeCredits { get; set; }
<<<<<<< HEAD
        public int FocusPoints { get; set; }
        public List<DashboardSessionViewModel> UpcomingSessions { get; set; } = new();
        public List<DashboardSessionViewModel> MySessions { get; set; } = new();
=======
        public int SessionsHosted { get; set; }
        public int SessionsJoined { get; set; }
        public List<UpcomingSessionViewModel> UpcomingSessions { get; set; } = new();
        public List<TransactionLogViewModel> RecentTransactions { get; set; } = new();
>>>>>>> 76461639f11a644f5445e0d487f22318f27277d5
    }

    public class UpcomingSessionViewModel
    {
        public int Id { get; set; }
<<<<<<< HEAD
        public string SkillName { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
=======
        public string Title { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; }
>>>>>>> 76461639f11a644f5445e0d487f22318f27277d5
        public int CreditCost { get; set; }
        public string HostName { get; set; } = string.Empty;
        public string? HostPhotoPath { get; set; }
        public int EnrolledCount { get; set; }
        public int MaxAttendees { get; set; }
        public SessionStatus Status { get; set; }
        public bool IsHost { get; set; }
    }

    // ── Session Create Form ────────────────────────────────────────────
    public class CreateSessionViewModel
    {
        [Required]
        [StringLength(150)]
        [Display(Name = "Session Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Topic / Skill")]
        public string Topic { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Date & Time")]
        public DateTime ScheduledAt { get; set; } = DateTime.Now.AddDays(1);

        [Required]
        [Range(15, 480)]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; } = 60;

        [Required]
        [Range(1, 50)]
        [Display(Name = "Credit Cost per Attendee")]
        public int CreditCost { get; set; } = 2;

        [Required]
        [Range(1, 100)]
        [Display(Name = "Max Attendees")]
        public int MaxAttendees { get; set; } = 10;
    }

    // ── Session Browse ─────────────────────────────────────────────────
    public class SessionBrowseViewModel
    {
        public List<SessionCardViewModel> Sessions { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string? SelectedCategory { get; set; }
        public string? SearchQuery { get; set; }
        public int TotalCount { get; set; }
    }

    public class SessionCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; }
        public int CreditCost { get; set; }
        public int MaxAttendees { get; set; }
        public int EnrolledCount { get; set; }
        public string HostName { get; set; } = string.Empty;
        public string? HostPhotoPath { get; set; }
        public double HostRating { get; set; }
        public SessionStatus Status { get; set; }
        public bool IsEnrolled { get; set; }
        public bool IsHost { get; set; }
    }

    // ── Session Details ────────────────────────────────────────────────
    public class SessionDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; }
        public int CreditCost { get; set; }
        public int MaxAttendees { get; set; }
        public int EnrolledCount { get; set; }
        public SessionStatus Status { get; set; }
        // Host info
        public string HostId { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public string HostMajor { get; set; } = string.Empty;
        public string HostHeadline { get; set; } = string.Empty;
        public double HostRating { get; set; }
        public string? HostPhotoPath { get; set; }
        // Viewer context
        public bool IsHost { get; set; }
        public bool IsEnrolled { get; set; }
        public bool CanJoin { get; set; }
        // Attendee list (visible to host)
        public List<AttendeeRowViewModel> Attendees { get; set; } = new();
    }

    public class AttendeeRowViewModel
    {
        public string AttendeeId { get; set; } = string.Empty;
        public string AttendeeName { get; set; } = string.Empty;
        public string? AttendeePhotoPath { get; set; }
        public DateTime EnrolledAt { get; set; }
    }

    // ── Admin Portal ────────────────────────────────────────────────
    public class AdminUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public int TimeCredits { get; set; }
        public bool IsBanned { get; set; }
        public int SessionsHosted { get; set; }
        public int SessionsJoined { get; set; }
    }

    public class TransactionLogViewModel
    {
        public int Id { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int AmountChange { get; set; }
        public TransactionType Type { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
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
