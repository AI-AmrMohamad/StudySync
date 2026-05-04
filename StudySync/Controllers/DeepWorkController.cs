using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudySync.Data;
using StudySync.Models;

namespace StudySync.Controllers
{
    [Authorize]
    public class DeepWorkController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeepWorkController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /DeepWork
        public async Task<IActionResult> Index()
        {
            var activeRooms = await _context.FocusRooms
                .Include(r => r.FocusSessions)
                    .ThenInclude(fs => fs.User)
                .Where(r => r.IsActive && r.EndTime > DateTime.UtcNow)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(activeRooms);
        }

        // POST: /DeepWork/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string title, string topic, int durationMinutes)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(topic) || durationMinutes < 0)
            {
                TempData["Error"] = "Invalid room details.";
                return RedirectToAction(nameof(Index));
            }

            var room = new FocusRoom
            {
                Title = title,
                Topic = topic,
                DurationMinutes = durationMinutes,
                CreatedAt = DateTime.UtcNow,
                EndTime = durationMinutes == 0 ? DateTime.MaxValue : DateTime.UtcNow.AddMinutes(durationMinutes),
                IsActive = true
            };

            _context.FocusRooms.Add(room);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Room), new { id = room.Id });
        }

        // GET: /DeepWork/Room/5
        public async Task<IActionResult> Room(int id)
        {
            var room = await _context.FocusRooms
                .Include(r => r.FocusSessions)
                    .ThenInclude(fs => fs.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null || !room.IsActive || room.EndTime <= DateTime.UtcNow)
            {
                TempData["Error"] = "This room is no longer active.";
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            // Create a session for this user if they haven't already joined this room
            var session = room.FocusSessions.FirstOrDefault(fs => fs.UserId == userId);
            if (session == null)
            {
                session = new FocusSession
                {
                    UserId = userId,
                    RoomId = room.Id,
                    JoinedAt = DateTime.UtcNow,
                    MinutesStayed = 0,
                    PointsEarned = 0
                };
                _context.FocusSessions.Add(session);
                await _context.SaveChangesAsync();
            }

            return View(room);
        }

        // POST: /DeepWork/Leave/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var session = await _context.FocusSessions
                .Include(fs => fs.Room)
                .FirstOrDefaultAsync(fs => fs.RoomId == id && fs.UserId == userId);

            if (session != null)
            {
                var timeSpent = DateTime.UtcNow - session.JoinedAt;
                int minutesStayed = (int)timeSpent.TotalMinutes;
                
                // Cap minutes stayed to the room's duration, unless it's an open room (0)
                if (session.Room.DurationMinutes > 0 && minutesStayed > session.Room.DurationMinutes)
                {
                    minutesStayed = session.Room.DurationMinutes;
                }

                // Award points: 1 point per 10 minutes
                int pointsEarned = minutesStayed / 10;
                
                // Even if less than 10 minutes, let's give at least 1 point if they stayed > 1 min
                if (pointsEarned == 0 && minutesStayed >= 1) pointsEarned = 1;

                session.MinutesStayed = minutesStayed;
                session.PointsEarned = pointsEarned;

                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.FocusPoints += pointsEarned;
                    _context.Update(user);
                }

                // Reap empty rooms: if this user is the last active user (MinutesStayed was 0 before this execution)
                // we close the room so it stops appearing on the dashboard.
                var activeUsersCount = session.Room.FocusSessions.Count(fs => fs.MinutesStayed == 0);
                if (activeUsersCount <= 1)
                {
                    session.Room.IsActive = false;
                    _context.Update(session.Room);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"You left the room after {minutesStayed} minutes. Earned {pointsEarned} Focus Points!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
