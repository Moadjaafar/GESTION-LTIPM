using GESTION_LTIPN.Data;
using GESTION_LTIPN.Models;
using GESTION_LTIPN.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace GESTION_LTIPN.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingController> _logger;
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;

        public BookingController(
            ApplicationDbContext context,
            ILogger<BookingController> logger,
            IEmailService emailService,
            IOptions<EmailSettings> emailSettings)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
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
                TypeVoyages = new List<string> { "Congelé", "DRY" },
                TypeContenaires = new List<string> { "20P", "40P" }
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
                model.TypeVoyages = new List<string> { "Congelé", "DRY" };
                model.TypeContenaires = new List<string> { "20P", "40P" };
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
                TypeContenaire = model.TypeContenaire,
                NomClient = model.NomClient,
                CreatedByUserId = userId,
                BookingStatus = "Pending",
                Notes = model.Notes,
                CreatedAt = DateTime.Now
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingReference} created by user {UserId}", bookingReference, userId);

            // Send email notification
            try
            {
                var createdByUser = await _context.Users.FindAsync(userId);
                var society = await _context.Societies.FindAsync(model.SocietyId);

                if (createdByUser != null && society != null)
                {

                    // OR send to multiple recipients (example commented out):
                    var recipients = new List<string>
                     {
                         "saad.ourami@kingpelagique.ma",
                         createdByUser.Email  // Include creator's email
                     };
                    await _emailService.SendBookingCreatedEmailAsync(recipients, booking, createdByUser, society);

                    _logger.LogInformation("Email notification sent for booking {BookingReference}", bookingReference);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification for booking {BookingReference}", bookingReference);
                // Continue anyway - don't block the user flow due to email failure
            }

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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
            {
                return NotFound();
            }

            // Only pending bookings can be deleted
            if (booking.BookingStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Seules les réservations en attente peuvent être supprimées.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingId} deleted by user {UserId}", id, userId);

            TempData["SuccessMessage"] = $"Réservation {booking.BookingReference} supprimée avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Booking/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Society)
                .Include(b => b.Voyages)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Only allow editing pending bookings
            if (booking.BookingStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Seules les réservations en attente peuvent être modifiées.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var viewModel = new BookingViewModel
            {
                BookingId = booking.BookingId,
                BookingReference = booking.BookingReference,
                Numero_BK = booking.Numero_BK,
                SocietyId = booking.SocietyId,
                TypeVoyage = booking.TypeVoyage!,
                Nbr_LTC = booking.Nbr_LTC,
                TypeContenaire = booking.TypeContenaire,
                NomClient = booking.NomClient,
                Notes = booking.Notes,
                Societies = await _context.Societies
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SocietyName)
                    .ToListAsync(),
                TypeVoyages = new List<string> { "Congelé", "DRY" },
                TypeContenaires = new List<string> { "20P", "40P" }
            };

            return View(viewModel);
        }

        // POST: Booking/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(BookingViewModel model)
        {
            var booking = await _context.Bookings
                .Include(b => b.Voyages)
                .FirstOrDefaultAsync(b => b.BookingId == model.BookingId);

            if (booking == null)
            {
                return NotFound();
            }

            // Only allow editing pending bookings
            if (booking.BookingStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Seules les réservations en attente peuvent être modifiées.";
                return RedirectToAction(nameof(Details), new { id = model.BookingId });
            }

            // Validate: Cannot decrease Nbr_LTC if voyages already exist
            if (booking.Voyages.Count > 0 && model.Nbr_LTC < booking.Voyages.Count)
            {
                ModelState.AddModelError("Nbr_LTC", $"Impossible de réduire le nombre de LTC en dessous de {booking.Voyages.Count} car des voyages existent déjà.");
            }

            if (!ModelState.IsValid)
            {
                model.Societies = await _context.Societies
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SocietyName)
                    .ToListAsync();
                model.TypeVoyages = new List<string> { "Congelé", "DRY" };
                model.TypeContenaires = new List<string> { "20P", "40P" };
                return View(model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Update booking fields
            booking.Numero_BK = model.Numero_BK;
            booking.SocietyId = model.SocietyId;
            booking.TypeVoyage = model.TypeVoyage;
            booking.Nbr_LTC = model.Nbr_LTC;
            booking.TypeContenaire = model.TypeContenaire;
            booking.NomClient = model.NomClient;
            booking.Notes = model.Notes;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingId} updated by user {UserId}", booking.BookingId, userId);

            TempData["SuccessMessage"] = $"Réservation {booking.BookingReference} modifiée avec succès.";
            return RedirectToAction(nameof(Details), new { id = booking.BookingId });
        }

        // GET: Booking/SuperEdit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SuperEdit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Society)
                .Include(b => b.Voyages)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            var viewModel = new SuperEditViewModel
            {
                BookingId = booking.BookingId,
                BookingReference = booking.BookingReference,
                Numero_BK = booking.Numero_BK,
                SocietyId = booking.SocietyId,
                TypeVoyage = booking.TypeVoyage!,
                Nbr_LTC = booking.Nbr_LTC,
                TypeContenaire = booking.TypeContenaire,
                NomClient = booking.NomClient,
                Notes = booking.Notes,
                BookingStatus = booking.BookingStatus,
                Voyages = booking.Voyages.OrderBy(v => v.VoyageNumber).Select(v => new VoyageEditItem
                {
                    VoyageId = v.VoyageId,
                    VoyageNumber = v.VoyageNumber,
                    Numero_TC = v.Numero_TC!,
                    VoyageStatus = v.VoyageStatus,
                    DepartureType = v.DepartureType,
                    DepartureCity = v.DepartureCity,
                    DepartureDate = v.DepartureDate,
                    CanEdit = v.VoyageStatus == "Planned" // Only planned voyages can be edited
                }).ToList(),
                Societies = await _context.Societies
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SocietyName)
                    .ToListAsync(),
                TypeVoyages = new List<string> { "Congelé", "DRY" },
                TypeContenaires = new List<string> { "20P", "40P" }
            };

            return View(viewModel);
        }

        // POST: Booking/SuperEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SuperEdit(SuperEditViewModel model)
        {
            var booking = await _context.Bookings
                .Include(b => b.Voyages)
                .FirstOrDefaultAsync(b => b.BookingId == model.BookingId);

            if (booking == null)
            {
                return NotFound();
            }

            // Validate: Cannot decrease Nbr_LTC below existing voyage count
            if (booking.Voyages.Count > 0 && model.Nbr_LTC < booking.Voyages.Count)
            {
                ModelState.AddModelError("Nbr_LTC", $"Impossible de réduire le nombre de LTC en dessous de {booking.Voyages.Count} car des voyages existent déjà.");
            }

            // Validate Numero_TC uniqueness in voyages
            if (model.Voyages != null && model.Voyages.Any())
            {
                var numeroTCs = model.Voyages.Select(v => v.Numero_TC).ToList();
                var duplicateTCs = numeroTCs.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

                if (duplicateTCs.Any())
                {
                    ModelState.AddModelError("", $"Numéros TC dupliqués détectés: {string.Join(", ", duplicateTCs)}");
                }
            }

            if (!ModelState.IsValid)
            {
                model.Societies = await _context.Societies
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SocietyName)
                    .ToListAsync();
                model.TypeVoyages = new List<string> { "Congelé", "DRY" };
                model.TypeContenaires = new List<string> { "20P", "40P" };
                return View(model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Update booking fields (only if Pending)
            if (booking.BookingStatus == "Pending")
            {
                booking.Numero_BK = model.Numero_BK;
                booking.SocietyId = model.SocietyId;
                booking.TypeVoyage = model.TypeVoyage;
                booking.Nbr_LTC = model.Nbr_LTC;
                booking.TypeContenaire = model.TypeContenaire;
                booking.NomClient = model.NomClient;
                booking.Notes = model.Notes;
            }

            // Update voyages (only Planned ones)
            if (model.Voyages != null && model.Voyages.Any())
            {
                foreach (var voyageEdit in model.Voyages.Where(v => v.CanEdit))
                {
                    var voyage = booking.Voyages.FirstOrDefault(v => v.VoyageId == voyageEdit.VoyageId);
                    if (voyage != null && voyage.VoyageStatus == "Planned")
                    {
                        // Only update Numero_TC, VoyageNumber is not editable
                        voyage.Numero_TC = voyageEdit.Numero_TC;
                        voyage.UpdatedAt = DateTime.Now;
                    }
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingId} and voyages super-edited by user {UserId}", booking.BookingId, userId);

            TempData["SuccessMessage"] = $"Réservation {booking.BookingReference} et ses voyages modifiés avec succès.";
            return RedirectToAction(nameof(Details), new { id = booking.BookingId });
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
