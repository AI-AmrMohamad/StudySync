using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudySync.Models;
using StudySync.Models.ViewModels;
using StudySync.Data;

namespace StudySync.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new HomeDashboardViewModel();

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);
            if (userId != null)
            {
                vm.CurrentUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            }

            // Fetch exactly 5 upcoming open sessions
            vm.UpcomingSessions = await _context.Sessions
                .Include(s => s.Host)
                .Include(s => s.Enrollments)
                .Where(s => s.Status == SessionStatus.Open && s.ScheduledAt > DateTime.UtcNow)
                .OrderBy(s => s.ScheduledAt)
                .Take(5)
                .Select(s => new UpcomingSessionViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    Topic = s.Topic,
                    Category = s.Category,
                    ScheduledAt = s.ScheduledAt,
                    DurationMinutes = s.DurationMinutes,
                    CreditCost = s.CreditCost,
                    MaxAttendees = s.MaxAttendees,
                    EnrolledCount = s.Enrollments.Count,
                    Status = s.Status,
                    HostName = s.Host.FullName,
                    HostPhotoPath = s.Host.ProfilePhotoPath,
                    IsHost = s.HostId == userId
                })
                .ToListAsync();

            // Fetch all channels (Or the ones joined by user. Assuming simple fetch for now since Joined logic isn't fully M2M yet)
            vm.JoinedChannels = await _context.CommunityChannels.Take(5).ToListAsync();
        }

        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    public async Task<IActionResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return Json(new { sessions = new List<object>() });
        
        var normalizedQuery = query.ToLower();
        
        var sessions = await _context.Sessions
            .Where(s => s.Status == SessionStatus.Open && s.ScheduledAt > DateTime.UtcNow &&
                        (s.Title.ToLower().Contains(normalizedQuery) || s.Topic.ToLower().Contains(normalizedQuery)))
            .Select(s => new { id = s.Id, title = s.Title, type = "Session" })
            .Take(5)
            .ToListAsync();
            
        return Json(new { sessions });
    }
}
