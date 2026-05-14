using System.ComponentModel.DataAnnotations;

namespace StudySync.Models
{
    public class Skill
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();

    }
}
