using StudySync.Models;
using System.ComponentModel.DataAnnotations;

namespace StudySync.Models.ViewModels
{
    public class ProfileViewModel
    {
        public ApplicationUser CurrentUser { get; set; } = null!;
        
        // Split Skills
        public List<Skill> TeachingSkills { get; set; } = new List<Skill>();
        public List<Skill> LearningSkills { get; set; } = new List<Skill>();
        public List<Skill> AllAvailableSkills { get; set; } = new List<Skill>();

        // Past Work (Bounties)
        public List<HelpBounty> CompletedBounties { get; set; } = new List<HelpBounty>();

        // Editor Form Models
        public ProfileSettingsForm SettingsForm { get; set; } = new ProfileSettingsForm();
        public ChangePasswordForm PasswordForm { get; set; } = new ChangePasswordForm();
    }

    public class ProfileSettingsForm
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Profile Headline")]
        [StringLength(200)]
        public string ProfileHeadline { get; set; } = string.Empty;

        [Display(Name = "About Me")]
        [StringLength(2000)]
        public string Bio { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "WhatsApp Number")]
        [StringLength(20)]
        public string WhatsAppNumber { get; set; } = string.Empty;

        [Display(Name = "LinkedIn Profile URL")]
        [StringLength(255)]
        public string LinkedInProfile { get; set; } = string.Empty;
    }

    public class ChangePasswordForm
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
