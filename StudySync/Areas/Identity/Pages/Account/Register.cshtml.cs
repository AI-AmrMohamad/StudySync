using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudySync.Models;

namespace StudySync.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = string.Empty;

            [Required]
            [StringLength(150)]
            [Display(Name = "University")]
            public string UniversityName { get; set; } = string.Empty;

            [Required]
            [StringLength(100)]
            [Display(Name = "Major")]
            public string Major { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/Dashboard");

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FullName = Input.FullName,
                    UniversityName = Input.UniversityName,
                    Major = Input.Major,
                    TimeCredits = 5,
                    FocusPoints = 0
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    var dbContext = HttpContext.RequestServices.GetService(typeof(StudySync.Data.ApplicationDbContext)) as StudySync.Data.ApplicationDbContext;
                    if (dbContext != null)
                    {
                        if (!dbContext.CommunityChannels.Any(c => c.Tier == StudySync.Models.ChannelTier.Global))
                        {
                            dbContext.CommunityChannels.Add(new StudySync.Models.CommunityChannel { Name = "#global-lounge", Tier = StudySync.Models.ChannelTier.Global, Description = "Welcome to StudySync!" });
                        }
                        
                        string uniChannelName = "#" + user.UniversityName.ToLower().Replace(" ", "-") + "-general";
                        if (!dbContext.CommunityChannels.Any(c => c.Tier == StudySync.Models.ChannelTier.University && c.AssociatedEntityName == user.UniversityName))
                        {
                            dbContext.CommunityChannels.Add(new StudySync.Models.CommunityChannel { Name = uniChannelName, Tier = StudySync.Models.ChannelTier.University, AssociatedEntityName = user.UniversityName, Description = "General university updates." });
                        }

                        string majorChannelName = "#" + user.Major.ToLower().Replace(" ", "-") + "-majors";
                        if (!dbContext.CommunityChannels.Any(c => c.Tier == StudySync.Models.ChannelTier.Major && c.AssociatedEntityName == user.Major))
                        {
                            dbContext.CommunityChannels.Add(new StudySync.Models.CommunityChannel { Name = majorChannelName, Tier = StudySync.Models.ChannelTier.Major, AssociatedEntityName = user.Major, Description = "Major-specific discussions." });
                        }
                        
                        await dbContext.SaveChangesAsync();
                    }

                    _logger.LogInformation("User created a new account with password.");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }
    }
}
