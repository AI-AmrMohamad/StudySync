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

            var userId = user.Id;

            // Upcoming sessions I'm hosting
            var hostedSessions = await _context.Sessions
                .Include(s => s.Enrollments)
                .Where(s => s.HostId == userId && s.Status == SessionStatus.Open && s.ScheduledAt > DateTime.UtcNow)
                .OrderBy(s => s.ScheduledAt)
                .Take(5)
                .ToListAsync();

            // Upcoming sessions I've joined
            var joinedEnrollments = await _context.SessionEnrollments
                .Include(e => e.Session)
                    .ThenInclude(s => s.Host)
                .Include(e => e.Session.Enrollments)
                .Where(e => e.AttendeeId == userId
                            && e.Session.Status == SessionStatus.Open
                            && e.Session.ScheduledAt > DateTime.UtcNow)
                .OrderBy(e => e.Session.ScheduledAt)
                .Take(5)
                .ToListAsync();

            // Stats
            var sessionsHostedCount = await _context.Sessions.CountAsync(s => s.HostId == userId);
            var sessionsJoinedCount = await _context.SessionEnrollments.CountAsync(e => e.AttendeeId == userId);

            // Recent transactions
            var recentTx = await _context.CreditTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(8)
                .ToListAsync();

            // Merge upcoming sessions (host + joined) and sort by time
            var upcoming = new List<UpcomingSessionViewModel>();

            upcoming.AddRange(hostedSessions.Select(s => new UpcomingSessionViewModel
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
                IsHost = true,
                HostName = user.FullName,
                HostPhotoPath = user.ProfilePhotoPath
            }));

            upcoming.AddRange(joinedEnrollments.Select(e => new UpcomingSessionViewModel
            {
                Id = e.Session.Id,
                Title = e.Session.Title,
                Topic = e.Session.Topic,
                Category = e.Session.Category,
                ScheduledAt = e.Session.ScheduledAt,
                DurationMinutes = e.Session.DurationMinutes,
                CreditCost = e.Session.CreditCost,
                MaxAttendees = e.Session.MaxAttendees,
                EnrolledCount = e.Session.Enrollments.Count,
                Status = e.Session.Status,
                IsHost = false,
                HostName = e.Session.Host.FullName,
                HostPhotoPath = e.Session.Host.ProfilePhotoPath
            }));

            upcoming = upcoming.OrderBy(s => s.ScheduledAt).Take(5).ToList();

            var viewModel = new DashboardViewModel
            {
                FullName = user.FullName,
                Major = user.Major,
                TimeCredits = user.TimeCredits,
                SessionsHosted = sessionsHostedCount,
                SessionsJoined = sessionsJoinedCount,
                UpcomingSessions = upcoming,
                RecentTransactions = recentTx.Select(t => new TransactionLogViewModel
                {
                    Id = t.Id,
                    AmountChange = t.AmountChange,
                    Type = t.Type,
                    Note = t.Note,
                    CreatedAt = t.CreatedAt
                }).ToList()
            };

            return View(viewModel);
        }
    }
}
