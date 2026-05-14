using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public class TutorSession
    {
        public int Id { get; set; }

        [Required]
        public string TutorId { get; set; } = string.Empty;

        [Required]
        public int SkillId { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Scheduled Date & Time")]
        public DateTime ScheduledAt { get; set; }

        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; } = 60;

        [Display(Name = "Credit Cost")]
        [Range(1, 10)]
        public int CreditCost { get; set; } = 1;

        [Display(Name = "Max Attendees")]
        [Range(1, 50)]
        public int MaxAttendees { get; set; } = 10;

        public SessionStatus Status { get; set; } = SessionStatus.Upcoming;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser Tutor { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
        public ICollection<SessionEnrollment> Enrollments { get; set; } = new List<SessionEnrollment>();
    }
}
