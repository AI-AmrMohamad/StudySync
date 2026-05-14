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
    public class LiveSessionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LiveSessionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /LiveSessions
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var sessions = await _context.TutorSessions
                .Include(s => s.Tutor)
                .Include(s => s.Skill)
                .Include(s => s.Enrollments)
                .Where(s => s.Status != SessionStatus.Cancelled)
                .OrderBy(s => s.ScheduledAt)
                .ToListAsync();

            var viewModel = sessions.Select(s => new TutorSessionViewModel
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                SkillName = s.Skill.Name,
                SkillCategory = s.Skill.Category,
                TutorName = s.Tutor.FullName,
                TutorId = s.TutorId,
                ScheduledAt = s.ScheduledAt,
                DurationMinutes = s.DurationMinutes,
                CreditCost = s.CreditCost,
                MaxAttendees = s.MaxAttendees,
                EnrolledCount = s.Enrollments.Count,
                Status = s.Status,
                IsEnrolled = s.Enrollments.Any(e => e.StudentId == user.Id),
                IsTutor = s.TutorId == user.Id
            }).ToList();

            ViewBag.CurrentUserId = user.Id;
            return View(viewModel);
        }

        // GET: /LiveSessions/Create
        public async Task<IActionResult> Create()
        {
            var model = new CreateSessionViewModel
            {
                AvailableSkills = await _context.Skills.OrderBy(s => s.Name).ToListAsync()
            };
            return View(model);
        }

        // POST: /LiveSessions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSessionViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (model.ScheduledAt <= DateTime.Now.AddMinutes(1))
            {
                ModelState.AddModelError("ScheduledAt", "Session must be scheduled at least 30 minutes in the future.");
            }

            if (!ModelState.IsValid)
            {
                model.AvailableSkills = await _context.Skills.OrderBy(s => s.Name).ToListAsync();
                return View(model);
            }

            var session = new TutorSession
            {
                TutorId = user.Id,
                SkillId = model.SkillId,
                Title = model.Title,
                Description = model.Description,
                ScheduledAt = model.ScheduledAt,
                DurationMinutes = model.DurationMinutes,
                CreditCost = model.CreditCost,
                MaxAttendees = model.MaxAttendees,
                Status = SessionStatus.Upcoming,
                CreatedAt = DateTime.UtcNow
            };

            _context.TutorSessions.Add(session);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Session created successfully! Students can now enroll.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /LiveSessions/Enroll/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var session = await _context.TutorSessions
                    .Include(s => s.Enrollments)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (session == null)
                {
                    TempData["Error"] = "Session not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (session.TutorId == user.Id)
                {
                    TempData["Error"] = "You cannot enroll in your own session.";
                    return RedirectToAction(nameof(Index));
                }

                if (session.Status != SessionStatus.Upcoming)
                {
                    TempData["Error"] = "This session is no longer accepting enrollments.";
                    return RedirectToAction(nameof(Index));
                }

                if (session.Enrollments.Any(e => e.StudentId == user.Id))
                {
                    TempData["Error"] = "You are already enrolled in this session.";
                    return RedirectToAction(nameof(Index));
                }

                if (session.Enrollments.Count >= session.MaxAttendees)
                {
                    TempData["Error"] = "This session is full.";
                    return RedirectToAction(nameof(Index));
                }

                // Reload user for fresh credit data
                var freshUser = await _context.Users.FindAsync(user.Id);
                if (freshUser == null || freshUser.TimeCredits < session.CreditCost)
                {
                    TempData["Error"] = $"Insufficient Time Credits. You have {freshUser?.TimeCredits ?? 0} but need {session.CreditCost}.";
                    await transaction.RollbackAsync();
                    return RedirectToAction(nameof(Index));
                }

                // Deduct credits from student
                freshUser.TimeCredits -= session.CreditCost;

                // Create enrollment
                var enrollment = new SessionEnrollment
                {
                    TutorSessionId = session.Id,
                    StudentId = user.Id,
                    CreditsPaid = session.CreditCost,
                    EnrolledAt = DateTime.UtcNow
                };

                _context.SessionEnrollments.Add(enrollment);

                // Log transaction
                _context.CreditTransactions.Add(new CreditTransaction
                {
                    UserId = user.Id,
                    AmountChange = -session.CreditCost,
                    Type = TransactionType.SessionDebit,
                    Note = $"Enrolled in session: {session.Title}",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Successfully enrolled! {session.CreditCost} credits deducted.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "An error occurred. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /LiveSessions/Room/5
        public async Task<IActionResult> Room(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var session = await _context.TutorSessions
                .Include(s => s.Tutor)
                .Include(s => s.Skill)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                TempData["Error"] = "Session not found.";
                return RedirectToAction(nameof(Index));
            }

            var isTutor = session.TutorId == user.Id;
            var isEnrolled = session.Enrollments.Any(e => e.StudentId == user.Id);

            // Only tutor or enrolled students can enter the room
            if (!isTutor && !isEnrolled)
            {
                TempData["Error"] = "You must be enrolled to enter this session room.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new SessionRoomViewModel
            {
                SessionId = session.Id,
                Title = session.Title,
                Description = session.Description,
                SkillName = session.Skill.Name,
                TutorName = session.Tutor.FullName,
                TutorId = session.TutorId,
                ScheduledAt = session.ScheduledAt,
                DurationMinutes = session.DurationMinutes,
                Status = session.Status,
                IsTutor = isTutor,
                IsEnrolled = isEnrolled,
                EnrolledCount = session.Enrollments.Count,
                AttendeeNames = session.Enrollments.Select(e => e.Student.FullName).ToList()
            };

            return View(viewModel);
        }

        // POST: /LiveSessions/GoLive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoLive(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var session = await _context.TutorSessions.FindAsync(id);
            if (session == null)
            {
                TempData["Error"] = "Session not found.";
                return RedirectToAction(nameof(Index));
            }

            if (session.TutorId != user.Id)
            {
                TempData["Error"] = "Only the tutor can start this session.";
                return RedirectToAction(nameof(Index));
            }

            if (session.Status != SessionStatus.Upcoming)
            {
                TempData["Error"] = "This session cannot be started.";
                return RedirectToAction(nameof(Index));
            }

            session.Status = SessionStatus.Live;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Session is now LIVE!";
            return RedirectToAction(nameof(Room), new { id });
        }

        // POST: /LiveSessions/EndSession/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndSession(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var session = await _context.TutorSessions
                    .Include(s => s.Enrollments)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (session == null)
                {
                    TempData["Error"] = "Session not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (session.TutorId != user.Id)
                {
                    TempData["Error"] = "Only the tutor can end this session.";
                    return RedirectToAction(nameof(Index));
                }

                if (session.Status != SessionStatus.Live)
                {
                    TempData["Error"] = "This session is not currently live.";
                    return RedirectToAction(nameof(Index));
                }

                session.Status = SessionStatus.Completed;

                // Transfer all enrollment credits to tutor
                var totalCredits = session.Enrollments.Sum(e => e.CreditsPaid);
                var tutor = await _context.Users.FindAsync(session.TutorId);

                if (tutor != null && totalCredits > 0)
                {
                    tutor.TimeCredits += totalCredits;

                    _context.CreditTransactions.Add(new CreditTransaction
                    {
                        UserId = tutor.Id,
                        AmountChange = totalCredits,
                        Type = TransactionType.SessionCredit,
                        Note = $"Earned from session: {session.Title} ({session.Enrollments.Count} attendees)",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                TempData["Success"] = $"Session ended! You earned {totalCredits} credits from {session.Enrollments.Count} attendees.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                TempData["Error"] = "An error occurred. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /LiveSessions/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var session = await _context.TutorSessions
                    .Include(s => s.Enrollments)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (session == null)
                {
                    TempData["Error"] = "Session not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (session.TutorId != user.Id)
                {
                    TempData["Error"] = "Only the tutor can cancel this session.";
                    return RedirectToAction(nameof(Index));
                }

                if (session.Status == SessionStatus.Completed)
                {
                    TempData["Error"] = "Cannot cancel a completed session.";
                    return RedirectToAction(nameof(Index));
                }

                session.Status = SessionStatus.Cancelled;

                // Refund all enrolled students
                foreach (var enrollment in session.Enrollments)
                {
                    var student = await _context.Users.FindAsync(enrollment.StudentId);
                    if (student != null)
                    {
                        student.TimeCredits += enrollment.CreditsPaid;

                        _context.CreditTransactions.Add(new CreditTransaction
                        {
                            UserId = student.Id,
                            AmountChange = enrollment.CreditsPaid,
                            Type = TransactionType.SessionRefund,
                            Note = $"Refund for cancelled session: {session.Title}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                TempData["Success"] = $"Session cancelled. {session.Enrollments.Count} student(s) have been refunded.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                TempData["Error"] = "An error occurred. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
