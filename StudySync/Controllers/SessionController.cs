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
    public class SessionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SessionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Session/Browse  ← the new Marketplace
        [AllowAnonymous]
        public async Task<IActionResult> Browse(string? q, string? category)
        {
            var currentUserId = _userManager.GetUserId(User);

            // Get all distinct categories from existing sessions
            var categories = await _context.Sessions
                .Where(s => s.Status == SessionStatus.Open && s.ScheduledAt > DateTime.UtcNow)
                .Select(s => s.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var query = _context.Sessions
                .Include(s => s.Host)
                .Include(s => s.Enrollments)
                .Where(s => s.Status == SessionStatus.Open && s.ScheduledAt > DateTime.UtcNow
                            && !s.Host.IsBanned);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(s => s.Category == category);

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(s =>
                    s.Title.Contains(q) ||
                    s.Topic.Contains(q) ||
                    s.Description.Contains(q) ||
                    s.Host.FullName.Contains(q));

            var sessions = await query.OrderBy(s => s.ScheduledAt).ToListAsync();

            var enrolledSessionIds = new HashSet<int>();
            if (currentUserId != null)
            {
                enrolledSessionIds = (await _context.SessionEnrollments
                    .Where(e => e.AttendeeId == currentUserId)
                    .Select(e => e.SessionId)
                    .ToListAsync()).ToHashSet();
            }

            var vm = new SessionBrowseViewModel
            {
                Categories = categories,
                SelectedCategory = category,
                SearchQuery = q,
                TotalCount = sessions.Count,
                Sessions = sessions.Select(s => new SessionCardViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    Topic = s.Topic,
                    Category = s.Category,
                    Description = s.Description,
                    ScheduledAt = s.ScheduledAt,
                    DurationMinutes = s.DurationMinutes,
                    CreditCost = s.CreditCost,
                    MaxAttendees = s.MaxAttendees,
                    EnrolledCount = s.Enrollments.Count,
                    HostName = s.Host.FullName,
                    HostPhotoPath = s.Host.ProfilePhotoPath,
                    HostRating = s.Host.Rating,
                    Status = s.Status,
                    IsEnrolled = enrolledSessionIds.Contains(s.Id),
                    IsHost = s.HostId == currentUserId
                }).ToList()
            };

            return View(vm);
        }

        // GET: /Session/Create
        public IActionResult Create()
        {
            return View(new CreateSessionViewModel
            {
                ScheduledAt = DateTime.Now.AddDays(1).Date.AddHours(18)
            });
        }

        // POST: /Session/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSessionViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (vm.ScheduledAt.ToUniversalTime() <= DateTime.UtcNow.AddMinutes(30))
            {
                ModelState.AddModelError("ScheduledAt", "Session must be scheduled at least 30 minutes from now.");
                return View(vm);
            }

            var session = new Session
            {
                HostId = user.Id,
                Title = vm.Title,
                Description = vm.Description,
                Topic = vm.Topic,
                Category = vm.Category,
                ScheduledAt = vm.ScheduledAt.ToUniversalTime(),
                DurationMinutes = vm.DurationMinutes,
                CreditCost = vm.CreditCost,
                MaxAttendees = vm.MaxAttendees,
                Status = SessionStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Session \"{session.Title}\" created! It will appear in the marketplace.";
            return RedirectToAction(nameof(Details), new { id = session.Id });
        }

        // GET: /Session/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var currentUserId = _userManager.GetUserId(User);

            var session = await _context.Sessions
                .Include(s => s.Host)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Attendee)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

            bool isEnrolled = currentUserId != null &&
                              session.Enrollments.Any(e => e.AttendeeId == currentUserId);
            bool isHost = session.HostId == currentUserId;
            int spots = session.MaxAttendees - session.Enrollments.Count;

            var currentUser = currentUserId != null
                ? await _userManager.GetUserAsync(User)
                : null;

            bool canJoin = !isHost && !isEnrolled &&
                           session.Status == SessionStatus.Open &&
                           spots > 0 &&
                           session.ScheduledAt > DateTime.UtcNow &&
                           (currentUser?.TimeCredits ?? 0) >= session.CreditCost;

            var vm = new SessionDetailsViewModel
            {
                Id = session.Id,
                Title = session.Title,
                Topic = session.Topic,
                Category = session.Category,
                Description = session.Description,
                ScheduledAt = session.ScheduledAt,
                DurationMinutes = session.DurationMinutes,
                CreditCost = session.CreditCost,
                MaxAttendees = session.MaxAttendees,
                EnrolledCount = session.Enrollments.Count,
                Status = session.Status,
                HostId = session.HostId,
                HostName = session.Host.FullName,
                HostMajor = session.Host.Major,
                HostHeadline = session.Host.ProfileHeadline,
                HostRating = session.Host.Rating,
                HostPhotoPath = session.Host.ProfilePhotoPath,
                IsHost = isHost,
                IsEnrolled = isEnrolled,
                CanJoin = canJoin,
                Attendees = isHost ? session.Enrollments.Select(e => new AttendeeRowViewModel
                {
                    AttendeeId = e.AttendeeId,
                    AttendeeName = e.Attendee.FullName,
                    AttendeePhotoPath = e.Attendee.ProfilePhotoPath,
                    EnrolledAt = e.EnrolledAt
                }).ToList() : new()
            };

            return View(vm);
        }

        // POST: /Session/Join/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var session = await _context.Sessions
                .Include(s => s.Enrollments)
                .Include(s => s.Host)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                TempData["Error"] = "Session not found.";
                return RedirectToAction(nameof(Browse));
            }

            if (session.HostId == user.Id)
            {
                TempData["Error"] = "You cannot join your own session.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (session.Status != SessionStatus.Open || session.ScheduledAt <= DateTime.UtcNow)
            {
                TempData["Error"] = "This session is no longer open for enrollment.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (session.Enrollments.Any(e => e.AttendeeId == user.Id))
            {
                TempData["Error"] = "You are already enrolled in this session.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (session.Enrollments.Count >= session.MaxAttendees)
            {
                TempData["Error"] = "This session is full.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (user.TimeCredits < session.CreditCost)
            {
                TempData["Error"] = $"You need {session.CreditCost} credits to join, but you only have {user.TimeCredits}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Deduct from attendee
            user.TimeCredits -= session.CreditCost;
            await _userManager.UpdateAsync(user);

            // Credit the host
            var host = session.Host;
            host.TimeCredits += session.CreditCost;
            await _userManager.UpdateAsync(host);

            // Log transactions
            _context.CreditTransactions.Add(new CreditTransaction
            {
                UserId = user.Id,
                AmountChange = -session.CreditCost,
                Type = TransactionType.SessionJoin,
                Note = $"Joined session: \"{session.Title}\"",
                CreatedAt = DateTime.UtcNow
            });
            _context.CreditTransactions.Add(new CreditTransaction
            {
                UserId = host.Id,
                AmountChange = session.CreditCost,
                Type = TransactionType.SessionEarned,
                Note = $"Attendee joined: \"{session.Title}\"",
                CreatedAt = DateTime.UtcNow
            });

            // Create enrollment
            _context.SessionEnrollments.Add(new SessionEnrollment
            {
                SessionId = id,
                AttendeeId = user.Id,
                EnrolledAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = $"You've joined \"{session.Title}\"! {session.CreditCost} credit(s) have been transferred to the host.";
            return RedirectToAction(nameof(MyJoined));
        }

        // POST: /Session/Complete/5  (host marks session done)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var session = await _context.Sessions.FindAsync(id);
            if (session == null || session.HostId != user.Id)
            {
                TempData["Error"] = "Session not found or you are not the host.";
                return RedirectToAction(nameof(MyHosted));
            }

            session.Status = SessionStatus.Completed;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"\"{session.Title}\" has been marked as completed.";
            return RedirectToAction(nameof(MyHosted));
        }

        // POST: /Session/Cancel/5  (host cancels — refunds all attendees)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var session = await _context.Sessions
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Attendee)
                .Include(s => s.Host)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null || session.HostId != user.Id)
            {
                TempData["Error"] = "Session not found or you are not the host.";
                return RedirectToAction(nameof(MyHosted));
            }

            if (session.Status != SessionStatus.Open)
            {
                TempData["Error"] = "Only open sessions can be cancelled.";
                return RedirectToAction(nameof(MyHosted));
            }

            // Refund all attendees and claw back from host
            foreach (var enrollment in session.Enrollments)
            {
                var attendee = enrollment.Attendee;
                attendee.TimeCredits += session.CreditCost;
                await _userManager.UpdateAsync(attendee);

                _context.CreditTransactions.Add(new CreditTransaction
                {
                    UserId = attendee.Id,
                    AmountChange = session.CreditCost,
                    Type = TransactionType.SessionRefund,
                    Note = $"Refund: session \"{session.Title}\" was cancelled by host",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Deduct from host
            int totalRefund = session.Enrollments.Count * session.CreditCost;
            user.TimeCredits = Math.Max(0, user.TimeCredits - totalRefund);
            await _userManager.UpdateAsync(user);

            if (totalRefund > 0)
            {
                _context.CreditTransactions.Add(new CreditTransaction
                {
                    UserId = user.Id,
                    AmountChange = -totalRefund,
                    Type = TransactionType.SessionRefund,
                    Note = $"Host cancelled session: \"{session.Title}\" — refunded {session.Enrollments.Count} attendee(s)",
                    CreatedAt = DateTime.UtcNow
                });
            }

            session.Status = SessionStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Session \"{session.Title}\" was cancelled. {session.Enrollments.Count} attendee(s) refunded.";
            return RedirectToAction(nameof(MyHosted));
        }

        // GET: /Session/MyHosted
        public async Task<IActionResult> MyHosted()
        {
            var userId = _userManager.GetUserId(User);
            var sessions = await _context.Sessions
                .Include(s => s.Enrollments)
                .Where(s => s.HostId == userId)
                .OrderByDescending(s => s.ScheduledAt)
                .ToListAsync();

            var vm = sessions.Select(s => new UpcomingSessionViewModel
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
                IsHost = true
            }).ToList();

            return View(vm);
        }

        // GET: /Session/MyJoined
        public async Task<IActionResult> MyJoined()
        {
            var userId = _userManager.GetUserId(User);
            var enrollments = await _context.SessionEnrollments
                .Include(e => e.Session)
                    .ThenInclude(s => s.Host)
                .Include(e => e.Session.Enrollments)
                .Where(e => e.AttendeeId == userId)
                .OrderByDescending(e => e.Session.ScheduledAt)
                .ToListAsync();

            var vm = enrollments.Select(e => new UpcomingSessionViewModel
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
                HostName = e.Session.Host.FullName,
                HostPhotoPath = e.Session.Host.ProfilePhotoPath,
                Status = e.Session.Status,
                IsHost = false
            }).ToList();

            return View(vm);
        }
    }
}
