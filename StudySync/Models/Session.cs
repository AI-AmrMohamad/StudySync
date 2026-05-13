using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public class Session
    {
        public int Id { get; set; }

        [Required]
        public string HostId { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Topic { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "Scheduled At")]
        public DateTime ScheduledAt { get; set; }

        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; } = 60;

        [Display(Name = "Credit Cost per Attendee")]
        public int CreditCost { get; set; } = 1;

        [Display(Name = "Max Attendees")]
        public int MaxAttendees { get; set; } = 10;

        public SessionStatus Status { get; set; } = SessionStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser Host { get; set; } = null!;
        public ICollection<SessionEnrollment> Enrollments { get; set; } = new List<SessionEnrollment>();
    }
}
