using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public class SwapBooking
    {
        public int Id { get; set; }

        [Required]
        public string RequesterId { get; set; } = string.Empty;

        [Required]
        public string ProviderId { get; set; } = string.Empty;

        [Required]
        public int SkillId { get; set; }

        [Display(Name = "Credit Cost")]
        public int CreditCost { get; set; } = 1;

        [Display(Name = "Requester Confirmed")]
        public bool RequesterConfirmed { get; set; } = false;

        [Display(Name = "Provider Confirmed")]
        public bool ProviderConfirmed { get; set; } = false;

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;

        // Navigation properties
        public ApplicationUser Requester { get; set; } = null!;
        public ApplicationUser Provider { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
    }
}
