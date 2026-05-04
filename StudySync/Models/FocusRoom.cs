using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public class FocusRoom
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Topic { get; set; } = string.Empty;

        [Display(Name = "Duration (Minutes)")]
        public int DurationMinutes { get; set; }

        public DateTime EndTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<FocusSession> FocusSessions { get; set; } = new List<FocusSession>();
    }
}
