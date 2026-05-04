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

            // Fetch exactly 5 active rooms
            vm.LiveRooms = await _context.FocusRooms
                .Where(r => r.IsActive && r.EndTime > DateTime.UtcNow)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Fetch 5 open bounties
            vm.OpenJobs = await _context.HelpBounties
                .Where(b => b.Status == BountyStatus.Open)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
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
        if (string.IsNullOrWhiteSpace(query)) return Json(new { rooms = new List<object>(), jobs = new List<object>() });
        
        var normalizedQuery = query.ToLower();
        
        var rooms = await _context.FocusRooms
            .Where(r => r.Title.ToLower().Contains(normalizedQuery) && r.IsActive)
            .Select(r => new { id = r.Id, title = r.Title, type = "Room" })
            .Take(5)
            .ToListAsync();
            
        var jobs = await _context.HelpBounties
            .Where(j => j.Title.ToLower().Contains(normalizedQuery) && j.Status == BountyStatus.Open)
            .Select(j => new { id = j.Id, title = j.Title, type = "Job" })
            .Take(5)
            .ToListAsync();
            
        return Json(new { rooms, jobs });
    }
}
