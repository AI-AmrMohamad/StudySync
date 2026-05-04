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
    public class CommunityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommunityController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? channelId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // 1. Ensure User's Channels Exist (Supports Legacy Users)
            if (!string.IsNullOrWhiteSpace(user.UniversityName))
            {
                if (!await _context.CommunityChannels.AnyAsync(c => c.Tier == ChannelTier.University && c.AssociatedEntityName == user.UniversityName))
                {
                    _context.CommunityChannels.Add(new CommunityChannel { Name = "#" + user.UniversityName.ToLower().Replace(" ", "-") + "-general", Tier = ChannelTier.University, AssociatedEntityName = user.UniversityName, Description = "General university updates." });
                    await _context.SaveChangesAsync();
                }
            }
            if (!string.IsNullOrWhiteSpace(user.Major))
            {
                if (!await _context.CommunityChannels.AnyAsync(c => c.Tier == ChannelTier.Major && c.AssociatedEntityName == user.Major))
                {
                    _context.CommunityChannels.Add(new CommunityChannel { Name = "#" + user.Major.ToLower().Replace(" ", "-") + "-majors", Tier = ChannelTier.Major, AssociatedEntityName = user.Major, Description = "Major-specific discussions." });
                    await _context.SaveChangesAsync();
                }
            }
            if (!await _context.CommunityChannels.AnyAsync(c => c.Tier == ChannelTier.Global))
            {
                _context.CommunityChannels.Add(new CommunityChannel { Name = "#global-lounge", Tier = ChannelTier.Global, Description = "Welcome to StudySync!" });
                await _context.SaveChangesAsync();
            }

            var vm = new CommunityViewModel { CurrentUser = user };

            // 2. Fetch available channels based on constraints
            vm.GlobalChannels = await _context.CommunityChannels.Where(c => c.Tier == ChannelTier.Global).ToListAsync();
            vm.UniversityChannels = await _context.CommunityChannels.Where(c => c.Tier == ChannelTier.University && c.AssociatedEntityName == user.UniversityName).ToListAsync();
            vm.MajorChannels = await _context.CommunityChannels.Where(c => c.Tier == ChannelTier.Major && c.AssociatedEntityName == user.Major).ToListAsync();

            // 2. Select active channel
            if (channelId.HasValue)
            {
                vm.ActiveChannel = await _context.CommunityChannels.FindAsync(channelId.Value);
            }
            if (vm.ActiveChannel == null)
            {
                vm.ActiveChannel = vm.GlobalChannels.FirstOrDefault(); // default to global
            }

            // 4. Prevent rendering if access denied (security) - using Case Insensitive
            if (vm.ActiveChannel != null)
            {
                if (vm.ActiveChannel.Tier == ChannelTier.University && !string.Equals(vm.ActiveChannel.AssociatedEntityName, user.UniversityName, StringComparison.OrdinalIgnoreCase))
                    return Forbid();
                if (vm.ActiveChannel.Tier == ChannelTier.Major && !string.Equals(vm.ActiveChannel.AssociatedEntityName, user.Major, StringComparison.OrdinalIgnoreCase))
                    return Forbid();

                // Load recent messages
                vm.RecentMessages = await _context.ChatMessages
                    .Include(m => m.User)
                    .Where(m => m.CommunityChannelId == vm.ActiveChannel.Id)
                    .OrderBy(m => m.CreatedAt) // Oldest first for chat history
                    .Take(100)
                    .ToListAsync();
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateChannel(string channelName, string description)
        {
            if (string.IsNullOrWhiteSpace(channelName))
            {
                TempData["Error"] = "Channel name cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            // Sanitize: lowercase, no spaces
            var safeName = "#" + channelName.ToLower().Trim().Replace(" ", "-");

            if (await _context.CommunityChannels.AnyAsync(c => c.Name == safeName && c.Tier == ChannelTier.Global))
            {
                TempData["Error"] = $"A channel called {safeName} already exists.";
                return RedirectToAction(nameof(Index));
            }

            var channel = new CommunityChannel
            {
                Name = safeName,
                Tier = ChannelTier.Global,
                Description = string.IsNullOrWhiteSpace(description) ? "Community channel." : description
            };

            _context.CommunityChannels.Add(channel);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Channel {safeName} created successfully!";
            return RedirectToAction(nameof(Index), new { channelId = channel.Id });
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Json(new List<object>());

            var results = await _context.Users
                .Where(u => u.FullName.ToLower().Contains(query.ToLower()) ||
                            (u.Email != null && u.Email.ToLower().Contains(query.ToLower())))
                .Select(u => new { u.Id, u.FullName, u.Major, u.UniversityName })
                .Take(8)
                .ToListAsync();

            return Json(results);
        }
    }
}
