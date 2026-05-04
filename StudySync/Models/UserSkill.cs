using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public class UserSkill
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int SkillId { get; set; }

        [Display(Name = "Is Teaching")]
        public bool IsTeaching { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
    }
}
