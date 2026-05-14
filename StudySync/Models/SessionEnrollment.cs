using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public class SessionEnrollment
    {
        public int Id { get; set; }

        [Required]
        public int TutorSessionId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Display(Name = "Credits Paid")]
        public int CreditsPaid { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public TutorSession TutorSession { get; set; } = null!;
        public ApplicationUser Student { get; set; } = null!;
    }
}
