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

            // Sessions where this user is enrolled as a student
            var enrolledSessions = await _context.SessionEnrollments
                .Include(e => e.TutorSession)
                    .ThenInclude(ts => ts.Skill)
                .Include(e => e.TutorSession)
                    .ThenInclude(ts => ts.Tutor)
                .Include(e => e.TutorSession)
                    .ThenInclude(ts => ts.Enrollments)
                .Where(e => e.StudentId == user.Id
                    && e.TutorSession.Status != SessionStatus.Cancelled)
                .OrderBy(e => e.TutorSession.ScheduledAt)
                .Select(e => e.TutorSession)
                .ToListAsync();

            // Sessions where this user is the tutor
            var mySessions = await _context.TutorSessions
                .Include(ts => ts.Skill)
                .Include(ts => ts.Tutor)
                .Include(ts => ts.Enrollments)
                .Where(ts => ts.TutorId == user.Id
                    && ts.Status != SessionStatus.Cancelled)
                .OrderBy(ts => ts.ScheduledAt)
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                FullName = user.FullName,
                Major = user.Major,
                TimeCredits = user.TimeCredits,
                FocusPoints = user.FocusPoints,
                UpcomingSessions = enrolledSessions.Select(ts => new DashboardSessionViewModel
                {
                    Id = ts.Id,
                    Title = ts.Title,
                    SkillName = ts.Skill.Name,
                    TutorName = ts.Tutor.FullName,
                    ScheduledAt = ts.ScheduledAt,
                    CreditCost = ts.CreditCost,
                    Status = ts.Status,
                    Role = "Student",
                    EnrolledCount = ts.Enrollments.Count,
                    MaxAttendees = ts.MaxAttendees
                }).ToList(),
                MySessions = mySessions.Select(ts => new DashboardSessionViewModel
                {
                    Id = ts.Id,
                    Title = ts.Title,
                    SkillName = ts.Skill.Name,
                    TutorName = ts.Tutor.FullName,
                    ScheduledAt = ts.ScheduledAt,
                    CreditCost = ts.CreditCost,
                    Status = ts.Status,
                    Role = "Tutor",
                    EnrolledCount = ts.Enrollments.Count,
                    MaxAttendees = ts.MaxAttendees
                }).ToList()
            };

            return View(viewModel);
        }
    }
}
