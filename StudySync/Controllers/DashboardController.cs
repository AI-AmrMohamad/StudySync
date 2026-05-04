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
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var bookings = await _context.SwapBookings
                .Include(b => b.Skill)
                .Include(b => b.Requester)
                .Include(b => b.Provider)
                .Where(b => (b.RequesterId == user.Id || b.ProviderId == user.Id)
                         && b.Status != BookingStatus.Cancelled)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                FullName = user.FullName,
                Major = user.Major,
                TimeCredits = user.TimeCredits,
                FocusPoints = user.FocusPoints,
                UpcomingBookings = bookings.Select(b => new SwapBookingViewModel
                {
                    Id = b.Id,
                    SkillName = b.Skill.Name,
                    OtherUserName = b.RequesterId == user.Id ? b.Provider.FullName : b.Requester.FullName,
                    Role = b.RequesterId == user.Id ? "Requester" : "Provider",
                    CreditCost = b.CreditCost,
                    RequesterConfirmed = b.RequesterConfirmed,
                    ProviderConfirmed = b.ProviderConfirmed,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                }).ToList()
            };

            return View(viewModel);
        }
    }
}
