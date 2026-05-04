using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public class CreditTransaction
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Display(Name = "Amount Change")]
        public int AmountChange { get; set; }

        public TransactionType Type { get; set; }

        [StringLength(250)]
        public string? Note { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
    }
}
