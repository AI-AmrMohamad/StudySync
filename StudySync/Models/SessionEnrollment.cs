using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public class SessionEnrollment
    {
        public int Id { get; set; }

        [Required]
<<<<<<< HEAD
        public int TutorSessionId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Display(Name = "Credits Paid")]
        public int CreditsPaid { get; set; }
=======
        public int SessionId { get; set; }

        [Required]
        public string AttendeeId { get; set; } = string.Empty;
>>>>>>> 76461639f11a644f5445e0d487f22318f27277d5

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
<<<<<<< HEAD
        public TutorSession TutorSession { get; set; } = null!;
        public ApplicationUser Student { get; set; } = null!;
=======
        public Session Session { get; set; } = null!;
        public ApplicationUser Attendee { get; set; } = null!;
>>>>>>> 76461639f11a644f5445e0d487f22318f27277d5
    }
}
