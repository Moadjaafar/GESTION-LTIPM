using GESTION_LTIPN.Data;
using GESTION_LTIPN.Models;
using GESTION_LTIPN.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GESTION_LTIPN.Controllers
{
    [Authorize(Roles = "Admin,Trans_Respo")]
    public class VoyageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VoyageController> _logger;
        private readonly IEmailService _emailService;

        public VoyageController(ApplicationDbContext context, ILogger<VoyageController> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        // GET: Voyage/Index - List pending bookings for validation
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Society)
                .Include(b => b.CreatedByUser)
                .Include(b => b.ValidatedByUser)
                .Where(b => b.BookingStatus == "Pending" || b.BookingStatus == "Validated")
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // POST: Voyage/Validate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Validate(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Society)
                .Include(b => b.CreatedByUser)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.BookingStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Seules les réservations en attente peuvent être validées.";
                return RedirectToAction(nameof(Index));
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var validatedByUser = await _context.Users.FindAsync(userId);

            booking.BookingStatus = "Validated";
            booking.ValidatedByUserId = userId;
            booking.ValidatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingId} validated by user {UserId}", id, userId);

            // Send validation email to booking creator
            if (booking.CreatedByUser != null && !string.IsNullOrEmpty(booking.CreatedByUser.Email) && validatedByUser != null)
            {
                await _emailService.SendBookingValidatedEmailAsync(
                    booking.CreatedByUser.Email,
                    booking,
                    validatedByUser,
                    booking.Society
                );
            }

            TempData["SuccessMessage"] = "Réservation validée avec succès. Vous pouvez maintenant assigner des voyages.";
            return RedirectToAction(nameof(AssignVoyages), new { bookingId = id });
        }

        // GET: Voyage/AssignVoyages/5
        public async Task<IActionResult> AssignVoyages(int bookingId)
        {
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
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.BookingStatus == "Pending")
            {
                TempData["ErrorMessage"] = "Cette réservation doit d'abord être validée.";
                return RedirectToAction(nameof(Index));
            }

            var remainingVoyages = booking.Nbr_LTC - booking.Voyages.Count;
            var canAddVoyage = remainingVoyages > 0;

            var viewModel = new AssignVoyagesViewModel
            {
                Booking = booking,
                Voyages = booking.Voyages.OrderBy(v => v.VoyageNumber).ToList(),
                RemainingVoyages = remainingVoyages,
                CanAddVoyage = canAddVoyage
            };

            return View(viewModel);
        }

        // GET: Voyage/Create?bookingId=5
        public async Task<IActionResult> Create(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Voyages)
                .Include(b => b.Society)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.BookingStatus != "Validated")
            {
                TempData["ErrorMessage"] = "Seules les réservations validées peuvent avoir des voyages.";
                return RedirectToAction(nameof(Index));
            }

            if (booking.Voyages.Count >= booking.Nbr_LTC)
            {
                TempData["ErrorMessage"] = $"Le nombre maximum de voyages ({booking.Nbr_LTC}) a été atteint pour cette réservation.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId });
            }

            var nextVoyageNumber = booking.Voyages.Any() ? booking.Voyages.Max(v => v.VoyageNumber) + 1 : 1;

            var viewModel = new VoyageViewModel
            {
                BookingId = bookingId,
                VoyageNumber = nextVoyageNumber,
                BookingReference = booking.BookingReference,
                SocietyPrincipaleId = booking.SocietyId, // Auto-set from booking
                SocietyPrincipaleName = booking.Society?.SocietyName
            };

            return View(viewModel);
        }

        // POST: Voyage/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VoyageViewModel model)
        {
            // Get booking to set SocietyPrincipaleId
            var booking = await _context.Bookings
                .Include(b => b.Voyages)
                .Include(b => b.Society)
                .FirstOrDefaultAsync(b => b.BookingId == model.BookingId);

            if (booking == null)
            {
                return NotFound();
            }

            // Auto-set SocietyPrincipaleId from booking
            model.SocietyPrincipaleId = booking.SocietyId;

            if (!ModelState.IsValid)
            {
                model.BookingReference = booking.BookingReference;
                model.SocietyPrincipaleName = booking.Society?.SocietyName;
                return View(model);
            }

            if (booking.Voyages.Count >= booking.Nbr_LTC)
            {
                TempData["ErrorMessage"] = $"Le nombre maximum de voyages ({booking.Nbr_LTC}) a été atteint.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = model.BookingId });
            }

            var voyage = new Voyage
            {
                BookingId = model.BookingId,
                VoyageNumber = model.VoyageNumber,
                Numero_TC = model.Numero_TC,
                SocietyPrincipaleId = model.SocietyPrincipaleId,
                DepartureType = "Empty", // Default value, can be changed during departure
                VoyageStatus = "Planned",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Voyages.Add(voyage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Voyage {VoyageId} created for booking {BookingId}", voyage.VoyageId, model.BookingId);

            TempData["SuccessMessage"] = $"Voyage #{model.VoyageNumber} créé avec succès.";
            return RedirectToAction(nameof(AssignVoyages), new { bookingId = model.BookingId });
        }

        // GET: Voyage/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var voyage = await _context.Voyages
                .Include(v => v.Booking)
                .Include(v => v.SocietyPrincipale)
                .Include(v => v.SocietySecondaire)
                .Include(v => v.CamionFirst)
                .Include(v => v.CamionSecond)
                .Include(v => v.ValidatedByUser)
                .FirstOrDefaultAsync(m => m.VoyageId == id);

            if (voyage == null)
            {
                return NotFound();
            }

            return View(voyage);
        }

        // GET: Voyage/Depart/5
        public async Task<IActionResult> Depart(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var voyage = await _context.Voyages
                .Include(v => v.Booking)
                .Include(v => v.SocietyPrincipale)
                .Include(v => v.SocietySecondaire)
                .Include(v => v.CamionFirst)
                .FirstOrDefaultAsync(v => v.VoyageId == id);

            if (voyage == null)
            {
                return NotFound();
            }

            if (voyage.VoyageStatus != "Planned")
            {
                TempData["ErrorMessage"] = "Seuls les voyages planifiés peuvent partir.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
            }

            var viewModel = new VoyageViewModel
            {
                VoyageId = voyage.VoyageId,
                BookingId = voyage.BookingId,
                VoyageNumber = voyage.VoyageNumber,
                Numero_TC = voyage.Numero_TC,
                BookingReference = voyage.Booking?.BookingReference,
                SocietyPrincipaleName = voyage.SocietyPrincipale?.SocietyName,
                SocietySecondaireId = voyage.SocietySecondaireId,
                SocietySecondaireName = voyage.SocietySecondaire?.SocietyName,
                CamionFirstDepart = voyage.CamionFirstDepart,
                CamionFirstMatricule = voyage.CamionFirst?.CamionMatricule,
                DepartureCity = voyage.DepartureCity,
                DepartureType = voyage.DepartureType,
                Type_Emballage = voyage.Type_Emballage,
                DepartureDate = DateTime.Today,
                DepartureTime = TimeSpan.FromHours(DateTime.Now.Hour).Add(TimeSpan.FromMinutes(DateTime.Now.Minute)),
                Societies = await _context.Societies.Where(s => s.IsActive).OrderBy(s => s.SocietyName).ToListAsync(),
                SocietiesTransp = await _context.SocietiesTransp.Where(s => s.IsActive).OrderBy(s => s.SocietyTranspName).ToListAsync(),
                Camions = new List<Camion>(), // Will be loaded via AJAX
                DepartureCities = new List<string> { "Agadir", "Casablanca" },
                DepartureTypes = new List<string> { "Emballage", "Empty" }
            };

            return View(viewModel);
        }

        // POST: Voyage/Depart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Depart(VoyageViewModel model)
        {
            var voyage = await _context.Voyages
                .Include(v => v.Booking)
                .FirstOrDefaultAsync(v => v.VoyageId == model.VoyageId);

            if (voyage == null)
            {
                return NotFound();
            }

            if (voyage.VoyageStatus != "Planned")
            {
                TempData["ErrorMessage"] = "Seuls les voyages planifiés peuvent partir.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
            }

            // Validate DepartureType
            if (string.IsNullOrEmpty(model.DepartureType))
            {
                ModelState.AddModelError("DepartureType", "Le type de départ est requis.");
            }

            // Validate business rules for DepartureType
            if (model.DepartureType == "Emballage" && !model.SocietySecondaireId.HasValue)
            {
                ModelState.AddModelError("SocietySecondaireId", "La société secondaire est requise pour un départ de type Emballage.");
            }

            if (model.DepartureType == "Empty" && model.SocietySecondaireId.HasValue)
            {
                model.SocietySecondaireId = null; // Force null for Empty type
                model.Type_Emballage = null; // Clear Type_Emballage for Empty type
            }

            if (!model.DepartureDate.HasValue)
            {
                ModelState.AddModelError("DepartureDate", "La date de départ est requise.");
            }

            if (string.IsNullOrEmpty(model.DepartureCity))
            {
                ModelState.AddModelError("DepartureCity", "La ville de départ est requise.");
            }

            // Validate camion: either from society or externe
            if (model.IsFirstDepartExterne)
            {
                if (string.IsNullOrWhiteSpace(model.ExterneSocietyTranspName_First))
                {
                    ModelState.AddModelError("ExterneSocietyTranspName_First", "Le nom de la société de transport est requis.");
                }
                if (string.IsNullOrWhiteSpace(model.ExterneCamionMatricule_First))
                {
                    ModelState.AddModelError("ExterneCamionMatricule_First", "Le matricule du camion est requis.");
                }
                if (string.IsNullOrWhiteSpace(model.ExterneDriverName_First))
                {
                    ModelState.AddModelError("ExterneDriverName_First", "Le nom du chauffeur est requis.");
                }
                if (string.IsNullOrWhiteSpace(model.ExterneDriverPhone_First))
                {
                    ModelState.AddModelError("ExterneDriverPhone_First", "Le téléphone du chauffeur est requis.");
                }
            }
            else
            {
                if (!model.CamionFirstDepart.HasValue)
                {
                    ModelState.AddModelError("CamionFirstDepart", "Le camion pour le premier départ est requis.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Reload data for view
                voyage = await _context.Voyages
                    .Include(v => v.Booking)
                    .Include(v => v.SocietyPrincipale)
                    .Include(v => v.SocietySecondaire)
                    .Include(v => v.CamionFirst)
                    .FirstOrDefaultAsync(v => v.VoyageId == model.VoyageId);

                model.BookingReference = voyage.Booking?.BookingReference;
                model.SocietyPrincipaleName = voyage.SocietyPrincipale?.SocietyName;
                model.SocietySecondaireName = voyage.SocietySecondaire?.SocietyName;
                model.CamionFirstMatricule = voyage.CamionFirst?.CamionMatricule;
                model.Societies = await _context.Societies.Where(s => s.IsActive).OrderBy(s => s.SocietyName).ToListAsync();
                model.SocietiesTransp = await _context.SocietiesTransp.Where(s => s.IsActive).OrderBy(s => s.SocietyTranspName).ToListAsync();
                model.Camions = model.SocietyTranspFirstId.HasValue
                    ? await _context.Camions.Where(c => c.IsActive && c.SocietyTranspId == model.SocietyTranspFirstId).OrderBy(c => c.CamionMatricule).ToListAsync()
                    : new List<Camion>();
                model.DepartureCities = new List<string> { "Agadir", "Casablanca" };
                model.DepartureTypes = new List<string> { "Emballage", "Empty" };

                return View(model);
            }

            // Save DepartureType, SocietySecondaire, and Type_Emballage
            voyage.DepartureType = model.DepartureType;
            voyage.SocietySecondaireId = model.SocietySecondaireId;
            voyage.Type_Emballage = model.Type_Emballage;

            // Save camion info based on type (externe or from society)
            if (model.IsFirstDepartExterne)
            {
                // Create or get SocietyTransp
                var societyTransp = await _context.SocietiesTransp
                    .FirstOrDefaultAsync(s => s.SocietyTranspName == model.ExterneSocietyTranspName_First);

                if (societyTransp == null)
                {
                    societyTransp = new SocietyTransp
                    {
                        SocietyTranspName = model.ExterneSocietyTranspName_First,
                        Address = "N/A",
                        City = "N/A",
                        Phone = "N/A",
                        Email = "N/A",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.SocietiesTransp.Add(societyTransp);
                    await _context.SaveChangesAsync();
                }

                // Create Camion
                var camion = new Camion
                {
                    CamionMatricule = model.ExterneCamionMatricule_First,
                    DriverName = model.ExterneDriverName_First,
                    DriverPhone = model.ExterneDriverPhone_First,
                    CamionType = "EXTERNE",
                    SocietyTranspId = societyTransp.SocietyTranspId,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.Camions.Add(camion);
                await _context.SaveChangesAsync();

                voyage.CamionFirstDepart = camion.CamionId;
            }
            else
            {
                voyage.CamionFirstDepart = model.CamionFirstDepart;
            }

            voyage.DepartureCity = model.DepartureCity;
            voyage.DepartureDate = model.DepartureDate;
            voyage.DepartureTime = model.DepartureTime;
            voyage.VoyageStatus = "InProgress";
            voyage.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var camionInfo = model.IsFirstDepartExterne
                ? $"externe ({model.ExterneCamionMatricule_First})"
                : $"ID {model.CamionFirstDepart}";
            _logger.LogInformation("Voyage {VoyageId} departed with truck {CamionInfo}", model.VoyageId, camionInfo);

            TempData["SuccessMessage"] = $"Départ du voyage #{model.VoyageNumber} enregistré avec succès.";
            return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
        }

        // GET: Voyage/Reception/5
        public async Task<IActionResult> Reception(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var voyage = await _context.Voyages
                .Include(v => v.Booking)
                .Include(v => v.SocietyPrincipale)
                .Include(v => v.SocietySecondaire)
                .Include(v => v.CamionFirst)
                .Include(v => v.CamionSecond)
                .FirstOrDefaultAsync(v => v.VoyageId == id);

            if (voyage == null)
            {
                return NotFound();
            }

            if (voyage.VoyageStatus != "InProgress")
            {
                TempData["ErrorMessage"] = "Seuls les voyages en cours peuvent enregistrer une réception.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
            }

            if (!voyage.DepartureDate.HasValue)
            {
                TempData["ErrorMessage"] = "Le voyage doit d'abord être parti.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
            }

            var viewModel = new VoyageViewModel
            {
                VoyageId = voyage.VoyageId,
                BookingId = voyage.BookingId,
                VoyageNumber = voyage.VoyageNumber,
                Numero_TC = voyage.Numero_TC,
                BookingReference = voyage.Booking?.BookingReference,
                SocietyPrincipaleName = voyage.SocietyPrincipale?.SocietyName,
                SocietySecondaireName = voyage.SocietySecondaire?.SocietyName,
                CamionFirstMatricule = voyage.CamionFirst?.CamionMatricule,
                CamionSecondMatricule = voyage.CamionSecond?.CamionMatricule,
                DepartureCity = voyage.DepartureCity,
                DepartureDate = voyage.DepartureDate,
                DepartureTime = voyage.DepartureTime,
                DepartureType = voyage.DepartureType,
                ReceptionDate = DateTime.Today,
                ReceptionTime = TimeSpan.FromHours(DateTime.Now.Hour).Add(TimeSpan.FromMinutes(DateTime.Now.Minute))
            };

            return View(viewModel);
        }

        // POST: Voyage/Reception
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reception(VoyageViewModel model)
        {
            var voyage = await _context.Voyages
                .Include(v => v.Booking)
                .FirstOrDefaultAsync(v => v.VoyageId == model.VoyageId);

            if (voyage == null)
            {
                return NotFound();
            }

            if (voyage.VoyageStatus != "InProgress")
            {
                TempData["ErrorMessage"] = "Seuls les voyages en cours peuvent enregistrer une réception.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
            }

            if (!model.ReceptionDate.HasValue)
            {
                ModelState.AddModelError("ReceptionDate", "La date de réception est requise.");
            }

            if (model.ReceptionDate.HasValue && voyage.DepartureDate.HasValue && model.ReceptionDate < voyage.DepartureDate)
            {
                ModelState.AddModelError("ReceptionDate", "La date de réception ne peut pas être antérieure à la date de départ.");
            }

            if (!ModelState.IsValid)
            {
                // Reload data for view
                voyage = await _context.Voyages
                    .Include(v => v.Booking)
                    .Include(v => v.SocietyPrincipale)
                    .Include(v => v.SocietySecondaire)
                    .Include(v => v.CamionFirst)
                    .Include(v => v.CamionSecond)
                    .FirstOrDefaultAsync(v => v.VoyageId == model.VoyageId);

                model.BookingReference = voyage.Booking?.BookingReference;
                model.SocietyPrincipaleName = voyage.SocietyPrincipale?.SocietyName;
                model.SocietySecondaireName = voyage.SocietySecondaire?.SocietyName;
                model.CamionFirstMatricule = voyage.CamionFirst?.CamionMatricule;
                model.CamionSecondMatricule = voyage.CamionSecond?.CamionMatricule;
                model.DepartureCity = voyage.DepartureCity;
                model.DepartureDate = voyage.DepartureDate;
                model.DepartureTime = voyage.DepartureTime;
                model.DepartureType = voyage.DepartureType;

                return View(model);
            }

            voyage.ReceptionDate = model.ReceptionDate;
            voyage.ReceptionTime = model.ReceptionTime;
            voyage.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Voyage {VoyageId} reception recorded", model.VoyageId);

            TempData["SuccessMessage"] = $"Réception du voyage #{model.VoyageNumber} à Dakhla enregistrée avec succès.";
            return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
        }

        // GET: Voyage/ReturnDepart/5
        public async Task<IActionResult> ReturnDepart(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var voyage = await _context.Voyages
                .Include(v => v.Booking)
                .Include(v => v.SocietyPrincipale)
                .Include(v => v.SocietySecondaire)
                .Include(v => v.CamionFirst)
                .Include(v => v.CamionSecond)
                .FirstOrDefaultAsync(v => v.VoyageId == id);

            if (voyage == null)
            {
                return NotFound();
            }

            if (voyage.VoyageStatus != "InProgress")
            {
                TempData["ErrorMessage"] = "Seuls les voyages en cours peuvent enregistrer un départ retour.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
            }

            if (!voyage.ReceptionDate.HasValue)
            {
                TempData["ErrorMessage"] = "La réception à Dakhla doit d'abord être enregistrée.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
            }

            var viewModel = new VoyageViewModel
            {
                VoyageId = voyage.VoyageId,
                BookingId = voyage.BookingId,
                VoyageNumber = voyage.VoyageNumber,
                Numero_TC = voyage.Numero_TC,
                BookingReference = voyage.Booking?.BookingReference,
                SocietyPrincipaleName = voyage.SocietyPrincipale?.SocietyName,
                SocietySecondaireName = voyage.SocietySecondaire?.SocietyName,
                CamionFirstDepart = voyage.CamionFirstDepart,
                CamionFirstMatricule = voyage.CamionFirst?.CamionMatricule,
                CamionSecondDepart = voyage.CamionSecondDepart,
                CamionSecondMatricule = voyage.CamionSecond?.CamionMatricule,
                DepartureCity = voyage.DepartureCity,
                DepartureType = voyage.DepartureType,
                DepartureDate = voyage.DepartureDate,
                ReceptionDate = voyage.ReceptionDate,
                ReturnDepartureDate = DateTime.Today,
                ReturnDepartureTime = TimeSpan.FromHours(DateTime.Now.Hour).Add(TimeSpan.FromMinutes(DateTime.Now.Minute)),
                SocietiesTransp = await _context.SocietiesTransp.Where(s => s.IsActive).OrderBy(s => s.SocietyTranspName).ToListAsync(),
                Camions = new List<Camion>(), // Will be loaded via AJAX
                DepartureCities = new List<string> { "Agadir", "Casablanca" }
            };

            return View(viewModel);
        }

        // POST: Voyage/ReturnDepart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnDepart(VoyageViewModel model)
        {
            var voyage = await _context.Voyages
                .Include(v => v.Booking)
                .FirstOrDefaultAsync(v => v.VoyageId == model.VoyageId);

            if (voyage == null)
            {
                return NotFound();
            }

            if (voyage.VoyageStatus != "InProgress")
            {
                TempData["ErrorMessage"] = "Seuls les voyages en cours peuvent enregistrer un départ retour.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
            }

            if (!model.ReturnDepartureDate.HasValue)
            {
                ModelState.AddModelError("ReturnDepartureDate", "La date de départ retour est requise.");
            }

            if (string.IsNullOrEmpty(model.ReturnArrivalCity))
            {
                ModelState.AddModelError("ReturnArrivalCity", "La ville d'arrivée est requise.");
            }

            // Validate camion: either from society or externe
            if (model.IsSecondDepartExterne)
            {
                if (string.IsNullOrWhiteSpace(model.ExterneSocietyTranspName_Second))
                {
                    ModelState.AddModelError("ExterneSocietyTranspName_Second", "Le nom de la société de transport est requis.");
                }
                if (string.IsNullOrWhiteSpace(model.ExterneCamionMatricule_Second))
                {
                    ModelState.AddModelError("ExterneCamionMatricule_Second", "Le matricule du camion est requis.");
                }
                if (string.IsNullOrWhiteSpace(model.ExterneDriverName_Second))
                {
                    ModelState.AddModelError("ExterneDriverName_Second", "Le nom du chauffeur est requis.");
                }
                if (string.IsNullOrWhiteSpace(model.ExterneDriverPhone_Second))
                {
                    ModelState.AddModelError("ExterneDriverPhone_Second", "Le téléphone du chauffeur est requis.");
                }
            }
            else
            {
                if (!model.CamionSecondDepart.HasValue)
                {
                    ModelState.AddModelError("CamionSecondDepart", "Le camion pour le deuxième départ est requis.");
                }
            }

            if (model.ReturnDepartureDate.HasValue && voyage.ReceptionDate.HasValue && model.ReturnDepartureDate < voyage.ReceptionDate)
            {
                ModelState.AddModelError("ReturnDepartureDate", "La date de départ retour ne peut pas être antérieure à la date de réception.");
            }

            if (!ModelState.IsValid)
            {
                // Reload data for view
                voyage = await _context.Voyages
                    .Include(v => v.Booking)
                    .Include(v => v.SocietyPrincipale)
                    .Include(v => v.SocietySecondaire)
                    .Include(v => v.CamionFirst)
                    .Include(v => v.CamionSecond)
                    .FirstOrDefaultAsync(v => v.VoyageId == model.VoyageId);

                model.BookingReference = voyage.Booking?.BookingReference;
                model.SocietyPrincipaleName = voyage.SocietyPrincipale?.SocietyName;
                model.SocietySecondaireName = voyage.SocietySecondaire?.SocietyName;
                model.CamionFirstMatricule = voyage.CamionFirst?.CamionMatricule;
                model.CamionSecondMatricule = voyage.CamionSecond?.CamionMatricule;
                model.DepartureCity = voyage.DepartureCity;
                model.DepartureDate = voyage.DepartureDate;
                model.ReceptionDate = voyage.ReceptionDate;
                model.SocietiesTransp = await _context.SocietiesTransp.Where(s => s.IsActive).OrderBy(s => s.SocietyTranspName).ToListAsync();
                model.Camions = model.SocietyTranspSecondId.HasValue
                    ? await _context.Camions.Where(c => c.IsActive && c.SocietyTranspId == model.SocietyTranspSecondId).OrderBy(c => c.CamionMatricule).ToListAsync()
                    : new List<Camion>();
                model.DepartureCities = new List<string> { "Agadir", "Casablanca" };

                return View(model);
            }

            // Save camion info based on type (externe or from society)
            if (model.IsSecondDepartExterne)
            {
                // Create or get SocietyTransp
                var societyTransp = await _context.SocietiesTransp
                    .FirstOrDefaultAsync(s => s.SocietyTranspName == model.ExterneSocietyTranspName_Second);

                if (societyTransp == null)
                {
                    societyTransp = new SocietyTransp
                    {
                        SocietyTranspName = model.ExterneSocietyTranspName_Second,
                        Address = "N/A",
                        City = "N/A",
                        Phone = "N/A",
                        Email = "N/A",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.SocietiesTransp.Add(societyTransp);
                    await _context.SaveChangesAsync();
                }

                // Create Camion
                var camion = new Camion
                {
                    CamionMatricule = model.ExterneCamionMatricule_Second,
                    DriverName = model.ExterneDriverName_Second,
                    DriverPhone = model.ExterneDriverPhone_Second,
                    CamionType = "EXTERNE",
                    SocietyTranspId = societyTransp.SocietyTranspId,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.Camions.Add(camion);
                await _context.SaveChangesAsync();

                voyage.CamionSecondDepart = camion.CamionId;
            }
            else
            {
                voyage.CamionSecondDepart = model.CamionSecondDepart;
            }

            voyage.ReturnDepartureDate = model.ReturnDepartureDate;
            voyage.ReturnDepartureTime = model.ReturnDepartureTime;
            voyage.ReturnArrivalCity = model.ReturnArrivalCity;
            voyage.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var camionInfo = model.IsSecondDepartExterne
                ? $"externe ({model.ExterneCamionMatricule_Second})"
                : $"ID {model.CamionSecondDepart}";
            _logger.LogInformation("Voyage {VoyageId} return departure recorded with truck {CamionInfo}", model.VoyageId, camionInfo);

            TempData["SuccessMessage"] = $"Départ retour du voyage #{model.VoyageNumber} de Dakhla enregistré avec succès.";
            return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
        }

        // GET: Voyage/ReturnArrival/5
        public async Task<IActionResult> ReturnArrival(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var voyage = await _context.Voyages
                .Include(v => v.Booking)
                .Include(v => v.SocietyPrincipale)
                .Include(v => v.SocietySecondaire)
                .Include(v => v.CamionFirst)
                .Include(v => v.CamionSecond)
                .FirstOrDefaultAsync(v => v.VoyageId == id);

            if (voyage == null)
            {
                return NotFound();
            }

            if (voyage.VoyageStatus != "InProgress")
            {
                TempData["ErrorMessage"] = "Seuls les voyages en cours peuvent enregistrer une arrivée retour.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
            }

            if (!voyage.ReturnDepartureDate.HasValue)
            {
                TempData["ErrorMessage"] = "Le départ retour de Dakhla doit d'abord être enregistré.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
            }

            var viewModel = new VoyageViewModel
            {
                VoyageId = voyage.VoyageId,
                BookingId = voyage.BookingId,
                VoyageNumber = voyage.VoyageNumber,
                Numero_TC = voyage.Numero_TC,
                BookingReference = voyage.Booking?.BookingReference,
                SocietyPrincipaleName = voyage.SocietyPrincipale?.SocietyName,
                SocietySecondaireName = voyage.SocietySecondaire?.SocietyName,
                CamionFirstMatricule = voyage.CamionFirst?.CamionMatricule,
                CamionSecondMatricule = voyage.CamionSecond?.CamionMatricule,
                DepartureCity = voyage.DepartureCity,
                DepartureType = voyage.DepartureType,
                ReturnArrivalCity = voyage.ReturnArrivalCity,
                ReturnDepartureDate = voyage.ReturnDepartureDate,
                ReturnArrivalDate = DateTime.Today,
                ReturnArrivalTime = TimeSpan.FromHours(DateTime.Now.Hour).Add(TimeSpan.FromMinutes(DateTime.Now.Minute)),
                HasSecondaryPrice = voyage.SocietySecondaireId.HasValue,
                Currency = voyage.Currency ?? "MAD"
            };

            return View(viewModel);
        }

        // POST: Voyage/ReturnArrival
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnArrival(VoyageViewModel model)
        {
            var voyage = await _context.Voyages
                .Include(v => v.Booking)
                .FirstOrDefaultAsync(v => v.VoyageId == model.VoyageId);

            if (voyage == null)
            {
                return NotFound();
            }

            if (voyage.VoyageStatus != "InProgress")
            {
                TempData["ErrorMessage"] = "Seuls les voyages en cours peuvent enregistrer une arrivée retour.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
            }

            if (!model.ReturnArrivalDate.HasValue)
            {
                ModelState.AddModelError("ReturnArrivalDate", "La date d'arrivée est requise.");
            }

            if (model.ReturnArrivalDate.HasValue && voyage.ReturnDepartureDate.HasValue && model.ReturnArrivalDate < voyage.ReturnDepartureDate)
            {
                ModelState.AddModelError("ReturnArrivalDate", "La date d'arrivée ne peut pas être antérieure à la date de départ retour.");
            }

            if (!ModelState.IsValid)
            {
                // Reload data for view
                voyage = await _context.Voyages
                    .Include(v => v.Booking)
                    .Include(v => v.SocietyPrincipale)
                    .Include(v => v.SocietySecondaire)
                    .Include(v => v.CamionFirst)
                    .Include(v => v.CamionSecond)
                    .FirstOrDefaultAsync(v => v.VoyageId == model.VoyageId);

                model.BookingReference = voyage.Booking?.BookingReference;
                model.SocietyPrincipaleName = voyage.SocietyPrincipale?.SocietyName;
                model.SocietySecondaireName = voyage.SocietySecondaire?.SocietyName;
                model.CamionFirstMatricule = voyage.CamionFirst?.CamionMatricule;
                model.CamionSecondMatricule = voyage.CamionSecond?.CamionMatricule;
                model.DepartureCity = voyage.DepartureCity;
                model.DepartureType = voyage.DepartureType;
                model.ReturnArrivalCity = voyage.ReturnArrivalCity;
                model.ReturnDepartureDate = voyage.ReturnDepartureDate;
                model.HasSecondaryPrice = voyage.SocietySecondaireId.HasValue;
                model.Currency = voyage.Currency ?? "MAD";

                return View(model);
            }

            voyage.ReturnArrivalDate = model.ReturnArrivalDate;
            voyage.ReturnArrivalTime = model.ReturnArrivalTime;
            voyage.VoyageStatus = "Completed"; // Mark as completed
            voyage.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Voyage {VoyageId} completed - return arrival recorded", model.VoyageId);

            TempData["SuccessMessage"] = $"Arrivée retour du voyage #{model.VoyageNumber} enregistrée avec succès. Voyage terminé.";
            return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
        }

        // POST: Voyage/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var voyage = await _context.Voyages
                .Include(v => v.Booking)
                .FirstOrDefaultAsync(v => v.VoyageId == id);

            if (voyage == null)
            {
                return NotFound();
            }

            if (voyage.VoyageStatus != "Planned")
            {
                TempData["ErrorMessage"] = "Seuls les voyages planifiés peuvent être supprimés.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
            }

            var bookingId = voyage.BookingId;

            _context.Voyages.Remove(voyage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Voyage {VoyageId} deleted", id);

            TempData["SuccessMessage"] = "Voyage supprimé avec succès.";
            return RedirectToAction(nameof(AssignVoyages), new { bookingId });
        }

        // GET: Voyage/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var voyage = await _context.Voyages
                .Include(v => v.Booking)
                .Include(v => v.SocietyPrincipale)
                .FirstOrDefaultAsync(v => v.VoyageId == id);

            if (voyage == null)
            {
                return NotFound();
            }

            // Only allow editing planned voyages
            if (voyage.VoyageStatus != "Planned")
            {
                TempData["ErrorMessage"] = "Seuls les voyages planifiés peuvent être modifiés.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
            }

            var viewModel = new VoyageViewModel
            {
                VoyageId = voyage.VoyageId,
                BookingId = voyage.BookingId,
                VoyageNumber = voyage.VoyageNumber,
                Numero_TC = voyage.Numero_TC,
                SocietyPrincipaleId = voyage.SocietyPrincipaleId,
                BookingReference = voyage.Booking?.BookingReference,
                SocietyPrincipaleName = voyage.SocietyPrincipale?.SocietyName
            };

            return View(viewModel);
        }

        // POST: Voyage/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(VoyageViewModel model)
        {
            var voyage = await _context.Voyages
                .Include(v => v.Booking)
                .FirstOrDefaultAsync(v => v.VoyageId == model.VoyageId);

            if (voyage == null)
            {
                return NotFound();
            }

            // Only allow editing planned voyages
            if (voyage.VoyageStatus != "Planned")
            {
                TempData["ErrorMessage"] = "Seuls les voyages planifiés peuvent être modifiés.";
                return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
            }

            // Get booking to validate
            var booking = await _context.Bookings
                .Include(b => b.Society)
                .FirstOrDefaultAsync(b => b.BookingId == voyage.BookingId);

            if (booking == null)
            {
                return NotFound();
            }

            // Auto-set SocietyPrincipaleId from booking
            model.SocietyPrincipaleId = booking.SocietyId;

            if (!ModelState.IsValid)
            {
                model.BookingReference = booking.BookingReference;
                model.SocietyPrincipaleName = booking.Society?.SocietyName;
                return View(model);
            }

            // Update voyage fields
            voyage.VoyageNumber = model.VoyageNumber;
            voyage.Numero_TC = model.Numero_TC;
            voyage.SocietyPrincipaleId = model.SocietyPrincipaleId;
            voyage.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Voyage {VoyageId} updated", voyage.VoyageId);

            TempData["SuccessMessage"] = $"Voyage #{model.VoyageNumber} modifié avec succès.";
            return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
        }

        // GET: Voyage/AssignPrices/5
        public async Task<IActionResult> AssignPrices(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var voyage = await _context.Voyages
                .Include(v => v.Booking)
                .Include(v => v.SocietyPrincipale)
                .Include(v => v.SocietySecondaire)
                .Include(v => v.CamionFirst)
                .Include(v => v.CamionSecond)
                .FirstOrDefaultAsync(v => v.VoyageId == id);

            if (voyage == null)
            {
                return NotFound();
            }

            var viewModel = new VoyageViewModel
            {
                VoyageId = voyage.VoyageId,
                Numero_TC = voyage.Numero_TC,
                BookingId = voyage.BookingId,
                VoyageNumber = voyage.VoyageNumber,
                BookingReference = voyage.Booking?.BookingReference,
                SocietyPrincipaleId = voyage.SocietyPrincipaleId,
                SocietyPrincipaleName = voyage.SocietyPrincipale?.SocietyName,
                SocietySecondaireId = voyage.SocietySecondaireId,
                SocietySecondaireName = voyage.SocietySecondaire?.SocietyName,
                CamionFirstMatricule = voyage.CamionFirst?.CamionMatricule,
                CamionSecondMatricule = voyage.CamionSecond?.CamionMatricule,
                DepartureCity = voyage.DepartureCity,
                DepartureType = voyage.DepartureType,
                DepartureDate = voyage.DepartureDate,
                VoyageStatus = voyage.VoyageStatus,
                PricePrincipale = voyage.PricePrincipale,
                PriceSecondaire = voyage.PriceSecondaire,
                Currency = voyage.Currency ?? "MAD",
                HasSecondaryPrice = voyage.SocietySecondaireId.HasValue
            };

            return View(viewModel);
        }

        // POST: Voyage/AssignPrices
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPrices(VoyageViewModel model)
        {
            var voyage = await _context.Voyages
                .Include(v => v.Booking)
                .Include(v => v.SocietyPrincipale)
                .Include(v => v.SocietySecondaire)
                .FirstOrDefaultAsync(v => v.VoyageId == model.VoyageId);

            if (voyage == null)
            {
                return NotFound();
            }

            // Validate: PricePrincipale is optional but if provided must be positive
            if (model.PricePrincipale.HasValue && model.PricePrincipale.Value < 0)
            {
                ModelState.AddModelError("PricePrincipale", "Le prix ne peut pas être négatif.");
            }

            // Validate: PriceSecondaire only if SocietySecondaireId exists
            if (!voyage.SocietySecondaireId.HasValue && model.PriceSecondaire.HasValue)
            {
                ModelState.AddModelError("PriceSecondaire", "Ce voyage n'a pas de société secondaire.");
            }

            if (model.PriceSecondaire.HasValue && model.PriceSecondaire.Value < 0)
            {
                ModelState.AddModelError("PriceSecondaire", "Le prix ne peut pas être négatif.");
            }

            if (!ModelState.IsValid)
            {
                // Reload data for view
                model.BookingReference = voyage.Booking?.BookingReference;
                model.VoyageNumber = voyage.VoyageNumber;
                model.BookingId = voyage.BookingId;
                model.SocietyPrincipaleName = voyage.SocietyPrincipale?.SocietyName;
                model.SocietySecondaireName = voyage.SocietySecondaire?.SocietyName;
                model.CamionFirstMatricule = voyage.CamionFirst?.CamionMatricule;
                model.CamionSecondMatricule = voyage.CamionSecond?.CamionMatricule;
                model.DepartureCity = voyage.DepartureCity;
                model.DepartureType = voyage.DepartureType;
                model.DepartureDate = voyage.DepartureDate;
                model.VoyageStatus = voyage.VoyageStatus;
                model.HasSecondaryPrice = voyage.SocietySecondaireId.HasValue;

                return View(model);
            }

            // Update prices
            voyage.PricePrincipale = model.PricePrincipale;
            voyage.PriceSecondaire = model.PriceSecondaire;
            voyage.Currency = model.Currency ?? "MAD";
            voyage.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Prices assigned to voyage {VoyageId}: Principal={PricePrincipale}, Secondary={PriceSecondaire}",
                voyage.VoyageId, model.PricePrincipale, model.PriceSecondaire);

            TempData["SuccessMessage"] = $"Prix pour le voyage #{model.VoyageNumber} enregistrés avec succès.";
            return RedirectToAction(nameof(AssignVoyages), new { bookingId = voyage.BookingId });
        }

        // GET: Voyage/GetCamionsBySociety?societyTranspId=1
        [HttpGet]
        public async Task<IActionResult> GetCamionsBySociety(int? societyTranspId)
        {
            if (!societyTranspId.HasValue)
            {
                return Json(new List<object>());
            }

            var camions = await _context.Camions
                .Where(c => c.IsActive && c.SocietyTranspId == societyTranspId)
                .OrderBy(c => c.CamionMatricule)
                .Select(c => new
                {
                    camionId = c.CamionId,
                    camionMatricule = c.CamionMatricule,
                    driverName = c.DriverName
                })
                .ToListAsync();

            return Json(camions);
        }
    }
}
