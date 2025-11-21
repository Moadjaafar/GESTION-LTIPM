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
                ETD = model.ETD,
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

            // Load active temporisation if booking is temporised
            if (booking.BookingStatus == "Temporised")
            {
                var temporisation = await _context.BookingTemporisations
                    .Include(t => t.TemporisedByUser)
                    .FirstOrDefaultAsync(t => t.BookingId == id && t.IsActive);

                if (temporisation != null)
                {
                    ViewBag.Temporisation = temporisation;
                }
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
                ETD = booking.ETD ?? DateTime.Now,
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
            booking.ETD = model.ETD;

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

        // =======================================================================
        // TEMPORISATION FEATURE
        // =======================================================================

        // GET: Booking/Temporiser/5
        [Authorize(Roles = "Admin,Validator")]
        public async Task<IActionResult> Temporiser(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Society)
                .Include(b => b.CreatedByUser)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Only pending bookings can be temporised
            if (booking.BookingStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Seules les réservations en attente peuvent être temporisées.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var viewModel = new TemporiseBookingViewModel
            {
                BookingId = booking.BookingId,
                BookingReference = booking.BookingReference,
                Numero_BK = booking.Numero_BK,
                SocietyName = booking.Society?.SocietyName,
                TypeVoyage = booking.TypeVoyage,
                CreatedByUserName = booking.CreatedByUser?.FullName,
                EstimatedValidationDate = DateTime.Today.AddDays(7) // Default: 7 days from today
            };

            return View(viewModel);
        }

        // POST: Booking/Temporiser
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Validator")]
        public async Task<IActionResult> Temporiser(TemporiseBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var booking = await _context.Bookings
                .Include(b => b.CreatedByUser)
                .Include(b => b.Society)
                .FirstOrDefaultAsync(b => b.BookingId == model.BookingId);

            if (booking == null)
            {
                return NotFound();
            }

            // Validate booking status
            if (booking.BookingStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Seules les réservations en attente peuvent être temporisées.";
                return RedirectToAction(nameof(Details), new { id = model.BookingId });
            }

            // Validate estimated date is in the future
            if (model.EstimatedValidationDate <= DateTime.Today)
            {
                ModelState.AddModelError("EstimatedValidationDate", "La date estimée doit être dans le futur.");
                return View(model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Deactivate any existing active temporisations for this booking
            var existingTemporisations = await _context.BookingTemporisations
                .Where(bt => bt.BookingId == model.BookingId && bt.IsActive)
                .ToListAsync();

            foreach (var temp in existingTemporisations)
            {
                temp.IsActive = false;
                temp.UpdatedAt = DateTime.Now;
            }

            // Create new temporisation record
            var temporisation = new BookingTemporisation
            {
                BookingId = model.BookingId,
                TemporisedByUserId = userId,
                TemporisedAt = DateTime.Now,
                ReasonTemporisation = model.ReasonTemporisation,
                EstimatedValidationDate = model.EstimatedValidationDate,
                CreatorResponse = "Pending",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.BookingTemporisations.Add(temporisation);

            // Update booking status to Temporised
            booking.BookingStatus = "Temporised";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingId} temporised by user {UserId} until {EstimatedDate}",
                model.BookingId, userId, model.EstimatedValidationDate);

            // Send email notification to booking creator
            try
            {
                _logger.LogInformation("Attempting to send temporisation email for booking {BookingId}", model.BookingId);

                if (booking.CreatedByUser == null)
                {
                    _logger.LogWarning("Cannot send temporisation email: CreatedByUser is null for booking {BookingId}", model.BookingId);
                }
                else if (string.IsNullOrEmpty(booking.CreatedByUser.Email))
                {
                    _logger.LogWarning("Cannot send temporisation email: CreatedByUser email is empty for booking {BookingId}", model.BookingId);
                }
                else if (booking.Society == null)
                {
                    _logger.LogWarning("Cannot send temporisation email: Society is null for booking {BookingId}", model.BookingId);
                }
                else
                {
                    var temporisedByUser = await _context.Users.FindAsync(userId);
                    if (temporisedByUser == null)
                    {
                        _logger.LogWarning("Cannot send temporisation email: temporisedByUser not found for userId {UserId}", userId);
                    }
                    else
                    {
                        _logger.LogInformation("Sending email to {Email} for booking {BookingId}", booking.CreatedByUser.Email, model.BookingId);
                        await _emailService.SendBookingTemporisedEmailAsync(
                            booking.CreatedByUser.Email,
                            booking,
                            temporisation,
                            temporisedByUser,
                            booking.Society
                        );
                        _logger.LogInformation("Email notification successfully sent to {Email} for temporisation of booking {BookingId}",
                            booking.CreatedByUser.Email, model.BookingId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send temporisation email for booking {BookingId}", model.BookingId);
            }

            TempData["SuccessMessage"] = $"Réservation {booking.BookingReference} temporisée avec succès jusqu'au {model.EstimatedValidationDate:dd/MM/yyyy}.";
            return RedirectToAction(nameof(Details), new { id = model.BookingId });
        }

        // GET: Booking/RespondToTemporisation/5
        [Authorize(Roles = "Booking_Agent")]
        public async Task<IActionResult> RespondToTemporisation(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var temporisation = await _context.BookingTemporisations
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Society)
                .Include(t => t.TemporisedByUser)
                .FirstOrDefaultAsync(t => t.TemporisationId == id && t.IsActive);

            if (temporisation == null)
            {
                return NotFound();
            }

            var booking = temporisation.Booking;
            if (booking == null)
            {
                return NotFound();
            }

            // Verify the current user is the booking creator
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (booking.CreatedByUserId != userId)
            {
                TempData["ErrorMessage"] = "Vous n'êtes pas autorisé à répondre à cette temporisation.";
                return RedirectToAction(nameof(Index));
            }

            // Check if already responded
            if (temporisation.CreatorResponse != "Pending")
            {
                TempData["ErrorMessage"] = "Vous avez déjà répondu à cette temporisation.";
                return RedirectToAction(nameof(Details), new { id = booking.BookingId });
            }

            var viewModel = new RespondToTemporisationViewModel
            {
                TemporisationId = temporisation.TemporisationId,
                BookingId = booking.BookingId,
                BookingReference = booking.BookingReference,
                Numero_BK = booking.Numero_BK,
                SocietyName = booking.Society?.SocietyName,
                TypeVoyage = booking.TypeVoyage,
                TemporisedByUserName = temporisation.TemporisedByUser?.FullName,
                TemporisedAt = temporisation.TemporisedAt,
                ReasonTemporisation = temporisation.ReasonTemporisation,
                EstimatedValidationDate = temporisation.EstimatedValidationDate
            };

            return View(viewModel);
        }

        // POST: Booking/RespondToTemporisation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Booking_Agent")]
        public async Task<IActionResult> RespondToTemporisation(RespondToTemporisationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate response value
            if (model.CreatorResponse != "Accepted" && model.CreatorResponse != "Refused")
            {
                ModelState.AddModelError("CreatorResponse", "Réponse invalide. Veuillez accepter ou refuser.");
                return View(model);
            }

            var temporisation = await _context.BookingTemporisations
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Society)
                .Include(t => t.TemporisedByUser)
                .FirstOrDefaultAsync(t => t.TemporisationId == model.TemporisationId);

            if (temporisation == null)
            {
                return NotFound();
            }

            var booking = temporisation.Booking;
            if (booking == null)
            {
                return NotFound();
            }

            // Verify the current user is the booking creator
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (booking.CreatedByUserId != userId)
            {
                return Forbid();
            }

            // Check if already responded
            if (temporisation.CreatorResponse != "Pending")
            {
                TempData["ErrorMessage"] = "Vous avez déjà répondu à cette temporisation.";
                return RedirectToAction(nameof(Details), new { id = booking.BookingId });
            }

            // Update temporisation with creator response
            temporisation.CreatorResponse = model.CreatorResponse;
            temporisation.CreatorRespondedAt = DateTime.Now;
            temporisation.CreatorResponseNotes = model.CreatorResponseNotes;
            temporisation.UpdatedAt = DateTime.Now;

            // Handle response actions
            if (model.CreatorResponse == "Refused")
            {
                // Creator refused: Return booking to Pending status
                booking.BookingStatus = "Pending";

                // Deactivate the temporisation
                temporisation.IsActive = false;

                _logger.LogInformation("Temporisation {TemporisationId} refused by user {UserId}. Booking {BookingId} returned to Pending.",
                    model.TemporisationId, userId, booking.BookingId);

                TempData["SuccessMessage"] = "Vous avez refusé la temporisation. La réservation est revenue au statut En attente.";
            }
            else // Accepted
            {
                // Creator accepted: Booking stays in Temporised status
                _logger.LogInformation("Temporisation {TemporisationId} accepted by user {UserId}. Booking {BookingId} remains Temporised until {EstimatedDate}.",
                    model.TemporisationId, userId, booking.BookingId, temporisation.EstimatedValidationDate);

                TempData["SuccessMessage"] = $"Vous avez accepté la temporisation. La réservation sera validée autour du {temporisation.EstimatedValidationDate:dd/MM/yyyy}.";
            }

            await _context.SaveChangesAsync();

            // Send email notification to admin/validator who temporised the booking
            try
            {
                if (temporisation.TemporisedByUser != null && !string.IsNullOrEmpty(temporisation.TemporisedByUser.Email))
                {
                    var creatorUser = await _context.Users.FindAsync(userId);
                    if (creatorUser != null && booking.Society != null)
                    {
                        await _emailService.SendTemporisationResponseEmailAsync(
                            temporisation.TemporisedByUser.Email,
                            booking,
                            temporisation,
                            creatorUser,
                            booking.Society
                        );
                        _logger.LogInformation("Email notification sent to {Email} about creator response for temporisation {TemporisationId}",
                            temporisation.TemporisedByUser.Email, model.TemporisationId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send response notification email for temporisation {TemporisationId}", model.TemporisationId);
            }

            return RedirectToAction(nameof(Details), new { id = booking.BookingId });
        }

        // GET: Booking/PendingTemporisations
        [Authorize(Roles = "Booking_Agent")]
        public async Task<IActionResult> PendingTemporisations()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var pendingTemporisations = await _context.BookingTemporisations
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Society)
                .Include(t => t.TemporisedByUser)
                .Where(t => t.Booking.CreatedByUserId == userId &&
                           t.IsActive &&
                           t.CreatorResponse == "Pending")
                .OrderByDescending(t => t.TemporisedAt)
                .Select(t => new PendingTemporisationItem
                {
                    TemporisationId = t.TemporisationId,
                    BookingId = t.BookingId,
                    BookingReference = t.Booking.BookingReference,
                    Numero_BK = t.Booking.Numero_BK,
                    SocietyName = t.Booking.Society != null ? t.Booking.Society.SocietyName : null,
                    TypeVoyage = t.Booking.TypeVoyage,
                    TemporisedByFullName = t.TemporisedByUser != null ? t.TemporisedByUser.FullName : null,
                    TemporisedAt = t.TemporisedAt,
                    ReasonTemporisation = t.ReasonTemporisation,
                    EstimatedValidationDate = t.EstimatedValidationDate,
                    DaysUntilEstimatedValidation = (int)(t.EstimatedValidationDate.Date - DateTime.Today).TotalDays
                })
                .ToListAsync();

            var viewModel = new PendingTemporisationsViewModel
            {
                PendingTemporisations = pendingTemporisations,
                TotalPending = pendingTemporisations.Count
            };

            return View(viewModel);
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
