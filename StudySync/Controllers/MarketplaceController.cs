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
    public class MarketplaceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MarketplaceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Marketplace
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var bookings = await _context.SwapBookings
                .Include(b => b.Skill)
                .Include(b => b.Requester)
                .Include(b => b.Provider)
                .Where(b => b.RequesterId == user.Id || b.ProviderId == user.Id)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var viewModel = bookings.Select(b => new SwapBookingViewModel
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
            }).ToList();

            return View(viewModel);
        }

        // GET: /Marketplace/Create
        public async Task<IActionResult> Create()
        {
            var model = new CreateBookingViewModel
            {
                AvailableSkills = await _context.Skills.OrderBy(s => s.Name).ToListAsync()
            };
            return View(model);
        }

        // POST: /Marketplace/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Find the provider by email
            var provider = await _userManager.FindByEmailAsync(model.ProviderEmail);
            if (provider == null)
            {
                ModelState.AddModelError("ProviderEmail", "No user found with that email.");
                model.AvailableSkills = await _context.Skills.OrderBy(s => s.Name).ToListAsync();
                return View(model);
            }

            if (provider.Id == user.Id)
            {
                ModelState.AddModelError("ProviderEmail", "You cannot create a booking with yourself.");
                model.AvailableSkills = await _context.Skills.OrderBy(s => s.Name).ToListAsync();
                return View(model);
            }

            if (model.CreditCost <= 0)
            {
                ModelState.AddModelError("CreditCost", "Credit cost must be greater than zero.");
                model.AvailableSkills = await _context.Skills.OrderBy(s => s.Name).ToListAsync();
                return View(model);
            }

            // Validate sufficient credits
            if (user.TimeCredits < model.CreditCost)
            {
                ModelState.AddModelError("", $"Insufficient Time Credits. You have {user.TimeCredits} but need {model.CreditCost}.");
                model.AvailableSkills = await _context.Skills.OrderBy(s => s.Name).ToListAsync();
                return View(model);
            }

            var booking = new SwapBooking
            {
                RequesterId = user.Id,
                ProviderId = provider.Id,
                SkillId = model.SkillId,
                CreditCost = model.CreditCost,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.SwapBookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking created successfully! Both parties must confirm to complete the swap.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Marketplace/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Use a database transaction for the double-handshake
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var booking = await _context.SwapBookings
                    .Include(b => b.Requester)
                    .Include(b => b.Provider)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                {
                    TempData["Error"] = "Booking not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    TempData["Error"] = "This booking has already been processed.";
                    return RedirectToAction(nameof(Index));
                }

                // Determine which party is confirming
                if (booking.RequesterId == user.Id)
                {
                    booking.RequesterConfirmed = true;
                }
                else if (booking.ProviderId == user.Id)
                {
                    booking.ProviderConfirmed = true;
                }
                else
                {
                    TempData["Error"] = "You are not part of this booking.";
                    return RedirectToAction(nameof(Index));
                }

                // Double-Handshake: Only transfer credits if BOTH have confirmed
                if (booking.RequesterConfirmed && booking.ProviderConfirmed)
                {
                    // Reload users to get fresh data within transaction
                    var requester = await _context.Users.FindAsync(booking.RequesterId);
                    var provider = await _context.Users.FindAsync(booking.ProviderId);

                    if (requester == null || provider == null)
                    {
                        TempData["Error"] = "User data not found.";
                        await transaction.RollbackAsync();
                        return RedirectToAction(nameof(Index));
                    }

                    // Final credit check
                    if (requester.TimeCredits < booking.CreditCost)
                    {
                        TempData["Error"] = "Requester has insufficient Time Credits.";
                        await transaction.RollbackAsync();
                        return RedirectToAction(nameof(Index));
                    }

                    // Transfer credits
                    requester.TimeCredits -= booking.CreditCost;
                    provider.TimeCredits += booking.CreditCost;

                    // Mark booking as confirmed
                    booking.Status = BookingStatus.Confirmed;

                    // Log transactions
                    _context.CreditTransactions.Add(new CreditTransaction
                    {
                        UserId = requester.Id,
                        AmountChange = -booking.CreditCost,
                        Type = TransactionType.SwapDebit,
                        Note = $"Swap booking #{booking.Id} - Paid for skill session",
                        CreatedAt = DateTime.UtcNow
                    });

                    _context.CreditTransactions.Add(new CreditTransaction
                    {
                        UserId = provider.Id,
                        AmountChange = booking.CreditCost,
                        Type = TransactionType.SwapCredit,
                        Note = $"Swap booking #{booking.Id} - Earned from skill session",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (booking.RequesterConfirmed && booking.ProviderConfirmed)
                {
                    TempData["Success"] = "Both parties confirmed! Credits have been transferred.";
                }
                else
                {
                    TempData["Success"] = "Your confirmation has been recorded. Waiting for the other party to confirm.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "A concurrency conflict occurred. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
