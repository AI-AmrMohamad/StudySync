using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public class HelpBounty
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Credit Reward")]
        public int CreditReward { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public BountyStatus Status { get; set; } = BountyStatus.Open;

        public bool IsOpen => Status == BountyStatus.Open;

        [Required]
        public string RequesterId { get; set; } = string.Empty;
        public ApplicationUser Requester { get; set; } = null!;
    }
}
