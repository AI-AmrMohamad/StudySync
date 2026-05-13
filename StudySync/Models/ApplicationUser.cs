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

        [StringLength(2000)]
        public string Bio { get; set; } = string.Empty;

        [StringLength(150)]
        public string ProfileHeadline { get; set; } = string.Empty;

        public double Rating { get; set; } = 5.0;

        public bool IsBanned { get; set; } = false;

        // Contact Information
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(20)]
        public string WhatsAppNumber { get; set; } = string.Empty;

        [StringLength(255)]
        public string LinkedInProfile { get; set; } = string.Empty;

        // Profile Photo
        [StringLength(500)]
        public string? ProfilePhotoPath { get; set; }

        // Navigation properties
        public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
        public ICollection<Session> HostedSessions { get; set; } = new List<Session>();
        public ICollection<SessionEnrollment> SessionEnrollments { get; set; } = new List<SessionEnrollment>();
        public ICollection<CreditTransaction> CreditTransactions { get; set; } = new List<CreditTransaction>();
    }
}
