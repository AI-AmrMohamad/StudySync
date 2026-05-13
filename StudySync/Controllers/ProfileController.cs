using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudySync.Data;
using StudySync.Models;
using StudySync.Models.ViewModels;

namespace StudySync.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.Users
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

            if (user == null) return Challenge();

            // Setup Demo Content if empty (User Requested Pre-Population)
            if (string.IsNullOrWhiteSpace(user.Bio) && string.IsNullOrWhiteSpace(user.ProfileHeadline))
            {
                user.FullName = "Amr Mohamad";
                user.ProfileHeadline = "Full-Stack Developer | Algorithm Specialist";
                user.Bio = "Hello! I'm a passionate developer specializing in complex algorithms and rapid web development. I love helping students crack difficult data structures and learning new frontend frameworks in my spare time.";
                user.Rating = 4.9;
                
                // Demo skills if missing
                if (!user.UserSkills.Any())
                {
                    var skills = await _context.Skills.Take(3).ToListAsync();
                    foreach (var s in skills)
                    {
                        user.UserSkills.Add(new UserSkill { SkillId = s.Id, IsTeaching = true });
                    }
                }
                await _context.SaveChangesAsync();
            }

            var vm = new ProfileViewModel
            {
                CurrentUser = user,
                AllAvailableSkills = await _context.Skills.ToListAsync(),
                TeachingSkills = user.UserSkills.Where(us => us.IsTeaching).Select(us => us.Skill).ToList(),
                LearningSkills = user.UserSkills.Where(us => !us.IsTeaching).Select(us => us.Skill).ToList()
            };

            vm.SettingsForm = new ProfileSettingsForm
            {
                FullName = user.FullName ?? "",
                ProfileHeadline = user.ProfileHeadline ?? "",
                Bio = user.Bio ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                WhatsAppNumber = user.WhatsAppNumber ?? "",
                LinkedInProfile = user.LinkedInProfile ?? ""
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings([Bind(Prefix = "SettingsForm")] ProfileSettingsForm form)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid form data.");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            user.FullName = form.FullName;
            user.ProfileHeadline = form.ProfileHeadline;
            user.Bio = form.Bio;
            user.PhoneNumber = form.PhoneNumber;
            user.WhatsAppNumber = form.WhatsAppNumber;
            user.LinkedInProfile = form.LinkedInProfile;

            await _userManager.UpdateAsync(user);

            // Return JSON for toast notification
            return Json(new { success = true, message = "Profile updated successfully!", headline = user.ProfileHeadline, bio = user.Bio, name = user.FullName });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "PasswordForm")] ChangePasswordForm form)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join("; ", errors) });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var result = await _userManager.ChangePasswordAsync(user, form.OldPassword, form.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return Json(new { success = true, message = "Password changed securely." });
            }

            var identityErrors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = identityErrors });
        }
        
        [HttpPost]
        public async Task<IActionResult> AddSkill(int skillId, bool isTeaching)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userSkill = await _context.UserSkills.FirstOrDefaultAsync(us => us.UserId == user.Id && us.SkillId == skillId);
            if (userSkill == null)
            {
                _context.UserSkills.Add(new UserSkill { UserId = user.Id, SkillId = skillId, IsTeaching = isTeaching });
            }
            else
            {
                userSkill.IsTeaching = isTeaching;
                _context.Update(userSkill);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        
        [HttpPost]
        public async Task<IActionResult> RemoveSkill(int skillId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userSkill = await _context.UserSkills.FirstOrDefaultAsync(us => us.UserId == user.Id && us.SkillId == skillId);
            if (userSkill != null)
            {
                _context.UserSkills.Remove(userSkill);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGlobalSkill(string name, string category, bool isTeaching)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Skill name cannot be empty." });

            // Check if exists
            var existingSkill = await _context.Skills
                .FirstOrDefaultAsync(s => s.Name.ToLower() == name.Trim().ToLower());

            int targetSkillId;

            if (existingSkill == null)
            {
                // Create new skill in DB
                var newSkill = new Skill { Name = name.Trim(), Category = string.IsNullOrWhiteSpace(category) ? "Other" : category };
                _context.Skills.Add(newSkill);
                await _context.SaveChangesAsync();
                targetSkillId = newSkill.Id;
            }
            else
            {
                targetSkillId = existingSkill.Id;
            }

            // Immediately add to user's profile
            var userSkill = await _context.UserSkills.FirstOrDefaultAsync(us => us.UserId == user.Id && us.SkillId == targetSkillId);
            if (userSkill == null)
            {
                _context.UserSkills.Add(new UserSkill { UserId = user.Id, SkillId = targetSkillId, IsTeaching = isTeaching });
            }
            else
            {
                userSkill.IsTeaching = isTeaching;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, id = targetSkillId, name = name.Trim() });
        }

        // POST: /Profile/UploadPhoto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (photo == null || photo.Length == 0)
                return Json(new { success = false, message = "No file selected." });

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return Json(new { success = false, message = "Only image files (jpg, png, gif, webp) are allowed." });

            // Validate file size (max 5 MB)
            if (photo.Length > 5 * 1024 * 1024)
                return Json(new { success = false, message = "File size must be less than 5 MB." });

            // Build save path
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            Directory.CreateDirectory(uploadsFolder);

            // Delete old photo if exists
            if (!string.IsNullOrEmpty(user.ProfilePhotoPath))
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePhotoPath.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            var fileName = $"{user.Id}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            user.ProfilePhotoPath = $"/uploads/avatars/{fileName}";
            await _userManager.UpdateAsync(user);

            return Json(new { success = true, photoUrl = user.ProfilePhotoPath });
        }

        // GET: /Profile/CreditHistory
        public async Task<IActionResult> CreditHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var transactions = await _context.CreditTransactions
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .ToListAsync();

            return Json(transactions.Select(t => new
            {
                t.Id,
                t.AmountChange,
                Type = t.Type.ToString(),
                t.Note,
                CreatedAt = t.CreatedAt.ToString("MMM dd, yyyy HH:mm")
            }));
        }
    }
}
