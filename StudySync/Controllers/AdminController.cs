using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudySync.Data;
using StudySync.Models;
using StudySync.Models.ViewModels;

namespace StudySync.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            var userCount = await _userManager.Users.CountAsync();
            var sessionCount = await _context.Sessions.CountAsync();
            var transactionCount = await _context.CreditTransactions.CountAsync();
            var bannedCount = await _userManager.Users.CountAsync(u => u.IsBanned);

            ViewBag.UserCount = userCount;
            ViewBag.SessionCount = sessionCount;
            ViewBag.TransactionCount = transactionCount;
            ViewBag.BannedCount = bannedCount;

            return View();
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users(string? search)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search) || u.Major.Contains(search));

            var users = await query.OrderBy(u => u.FullName).ToListAsync();

            var hostedCounts = await _context.Sessions
                .GroupBy(s => s.HostId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var joinedCounts = await _context.SessionEnrollments
                .GroupBy(e => e.AttendeeId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var viewModels = users.Select(u => new AdminUserViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                Major = u.Major,
                TimeCredits = u.TimeCredits,
                IsBanned = u.IsBanned,
                SessionsHosted = hostedCounts.TryGetValue(u.Id, out var h) ? h : 0,
                SessionsJoined = joinedCounts.TryGetValue(u.Id, out var j) ? j : 0
            }).ToList();

            ViewBag.Search = search;
            return View(viewModels);
        }

        // POST: /Admin/BanUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Cannot ban an admin account.";
                return RedirectToAction(nameof(Users));
            }

            user.IsBanned = !user.IsBanned;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = user.IsBanned
                ? $"{user.FullName} has been banned."
                : $"{user.FullName} has been unbanned.";

            return RedirectToAction(nameof(Users));
        }

        // POST: /Admin/AdjustCredits
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustCredits(string userId, int amount, string? note)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            user.TimeCredits += amount;
            if (user.TimeCredits < 0) user.TimeCredits = 0;
            await _userManager.UpdateAsync(user);

            _context.CreditTransactions.Add(new CreditTransaction
            {
                UserId = userId,
                AmountChange = amount,
                Type = TransactionType.AdminAdjustment,
                Note = string.IsNullOrWhiteSpace(note) ? "Admin credit adjustment" : note,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Adjusted {user.FullName}'s credits by {(amount >= 0 ? "+" : "")}{amount}. New balance: {user.TimeCredits}.";
            return RedirectToAction(nameof(Users));
        }

        // GET: /Admin/Transactions
        public async Task<IActionResult> Transactions(string? userId, int page = 1)
        {
            const int pageSize = 25;

            var query = _context.CreditTransactions
                .Include(t => t.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(t => t.UserId == userId);

            var total = await query.CountAsync();
            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = transactions.Select(t => new TransactionLogViewModel
            {
                Id = t.Id,
                UserFullName = t.User.FullName,
                UserEmail = t.User.Email ?? string.Empty,
                AmountChange = t.AmountChange,
                Type = t.Type,
                Note = t.Note,
                CreatedAt = t.CreatedAt
            }).ToList();

            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.FilterUserId = userId;

            return View(vm);
        }
    }
}
