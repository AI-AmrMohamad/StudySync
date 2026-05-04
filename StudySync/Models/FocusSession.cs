using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public class FocusSession
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int RoomId { get; set; }

        [Display(Name = "Minutes Stayed")]
        public int MinutesStayed { get; set; }

        [Display(Name = "Points Earned")]
        public int PointsEarned { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public FocusRoom Room { get; set; } = null!;
    }
}
