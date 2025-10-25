using GESTION_LTIPN.Data;
using GESTION_LTIPN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GESTION_LTIPN.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingController> _logger;

        public BookingController(ApplicationDbContext context, ILogger<BookingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Booking/Index
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            IQueryable<Booking> bookingsQuery = _context.Bookings
                .Include(b => b.Society)
                .Include(b => b.CreatedByUser)
                .Include(b => b.ValidatedByUser);

            // Filter based on user role
            if (userRole == "Booking_Agent")
            {
                // Booking agents can only see their own bookings
                bookingsQuery = bookingsQuery.Where(b => b.CreatedByUserId == userId);
            }

            var bookings = await bookingsQuery
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // GET: Booking/Create
        [Authorize(Roles = "Admin,Booking_Agent")]
        public async Task<IActionResult> Create()
        {
            var viewModel = new BookingViewModel
            {
                Societies = await _context.Societies
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SocietyName)
                    .ToListAsync(),
                TypeVoyages = new List<string> { "Congolé", "Conserve" }
            };

            return View(viewModel);
        }

        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Booking_Agent")]
        public async Task<IActionResult> Create(BookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Societies = await _context.Societies
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SocietyName)
                    .ToListAsync();
                model.TypeVoyages = new List<string> { "Congolé", "Conserve" };
                return View(model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Generate booking reference
            var bookingReference = await GenerateBookingReference();

            var booking = new Booking
            {
                BookingReference = bookingReference,
                Numero_BK = model.Numero_BK,
                SocietyId = model.SocietyId,
                TypeVoyage = model.TypeVoyage,
                Nbr_LTC = model.Nbr_LTC,
                CreatedByUserId = userId,
                BookingStatus = "Pending",
                Notes = model.Notes,
                CreatedAt = DateTime.Now
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingReference} created by user {UserId}", bookingReference, userId);

            TempData["SuccessMessage"] = $"Réservation {bookingReference} créée avec succès.";
            return RedirectToAction(nameof(Details), new { id = booking.BookingId });
        }

        // GET: Booking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Society)
                .Include(b => b.CreatedByUser)
                .Include(b => b.ValidatedByUser)
                .Include(b => b.Voyages)
                    .ThenInclude(v => v.SocietyPrincipale)
                .Include(b => b.Voyages)
                    .ThenInclude(v => v.SocietySecondaire)
                .Include(b => b.Voyages)
                    .ThenInclude(v => v.CamionFirst)
                .Include(b => b.Voyages)
                    .ThenInclude(v => v.CamionSecond)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Check authorization
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "Booking_Agent" && booking.CreatedByUserId != userId)
            {
                return Forbid();
            }

            return View(booking);
        }

        // POST: Booking/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Only creator or admin can delete
            if (userRole != "Admin" && booking.CreatedByUserId != userId)
            {
                TempData["ErrorMessage"] = "Vous n'êtes pas autorisé à supprimer cette réservation.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Only pending bookings can be deleted
            if (booking.BookingStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Seules les réservations en attente peuvent être supprimées.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingId} deleted by user {UserId}", id, userId);

            TempData["SuccessMessage"] = $"Réservation {booking.BookingReference} supprimée avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // Helper method to generate unique booking reference
        private async Task<string> GenerateBookingReference()
        {
            var date = DateTime.Now;
            var prefix = $"BK{date:yyyyMMdd}";

            var lastBooking = await _context.Bookings
                .Where(b => b.BookingReference.StartsWith(prefix))
                .OrderByDescending(b => b.BookingReference)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastBooking != null)
            {
                var lastSequence = lastBooking.BookingReference.Substring(prefix.Length);
                if (int.TryParse(lastSequence, out int num))
                {
                    sequence = num + 1;
                }
            }

            return $"{prefix}{sequence:D3}";
        }
    }
}
