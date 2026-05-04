using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Major { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Display(Name = "University Name")]
        public string UniversityName { get; set; } = string.Empty;

        [Display(Name = "Time Credits")]
        public int TimeCredits { get; set; } = 5;

        public int FocusPoints { get; set; } = 0;

        [StringLength(2000)]
        public string Bio { get; set; } = string.Empty;

        [StringLength(150)]
        public string ProfileHeadline { get; set; } = string.Empty;

        public double Rating { get; set; } = 5.0;

        public string ActiveRole { get; set; } = "LearningMode"; // "LearningMode" or "TeachingMode"

        public bool IsBanned { get; set; } = false;

        // Contact Information
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(20)]
        public string WhatsAppNumber { get; set; } = string.Empty;

        [StringLength(255)]
        public string LinkedInProfile { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
        public ICollection<SwapBooking> RequestedBookings { get; set; } = new List<SwapBooking>();
        public ICollection<SwapBooking> ProvidedBookings { get; set; } = new List<SwapBooking>();
        public ICollection<FocusSession> FocusSessions { get; set; } = new List<FocusSession>();
        public ICollection<CreditTransaction> CreditTransactions { get; set; } = new List<CreditTransaction>();
    }
}
