using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public class SessionEnrollment
    {
        public int Id { get; set; }

        [Required]
        public int SessionId { get; set; }

        [Required]
        public string AttendeeId { get; set; } = string.Empty;

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Session Session { get; set; } = null!;
        public ApplicationUser Attendee { get; set; } = null!;
    }
}
