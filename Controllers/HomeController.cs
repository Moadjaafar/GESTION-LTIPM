using System.Diagnostics;
using GESTION_LTIPN.Data;
using GESTION_LTIPN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GESTION_LTIPN.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel
            {
                // Overall Statistics
                TotalBookings = await _context.Bookings.CountAsync(),
                TotalVoyages = await _context.Voyages.CountAsync(),
                TotalSocieties = await _context.Societies.CountAsync(s => s.IsActive),
                TotalCamions = await _context.Camions.CountAsync(c => c.IsActive),
                TotalUsers = await _context.Users.CountAsync(u => u.IsActive),

                // Booking Statistics
                PendingBookings = await _context.Bookings.CountAsync(b => b.BookingStatus == "Pending"),
                ValidatedBookings = await _context.Bookings.CountAsync(b => b.BookingStatus == "Validated"),

                // Voyage Statistics
                PlannedVoyages = await _context.Voyages.CountAsync(v => v.VoyageStatus == "Planned"),
                InProgressVoyages = await _context.Voyages.CountAsync(v => v.VoyageStatus == "InProgress"),
                CompletedVoyages = await _context.Voyages.CountAsync(v => v.VoyageStatus == "Completed"),

                // Active Societies
                ActiveSocieties = await _context.Societies.CountAsync(s => s.IsActive),

                // Camion Statistics
                ActiveCamions = await _context.Camions.CountAsync(c => c.IsActive),
            };

            // Bookings by Status
            viewModel.BookingsByStatus = await _context.Bookings
                .GroupBy(b => b.BookingStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            // Bookings by Type Voyage
            viewModel.BookingsByTypeVoyage = await _context.Bookings
                .GroupBy(b => b.TypeVoyage)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);

            // Voyages by Status
            viewModel.VoyagesByStatus = await _context.Voyages
                .GroupBy(v => v.VoyageStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            // Voyages by Departure City (exclude voyages without departure city)
            viewModel.VoyagesByDepartureCity = await _context.Voyages
                .Where(v => v.DepartureCity != null)
                .GroupBy(v => v.DepartureCity)
                .Select(g => new { City = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.City!, x => x.Count);

            // Voyages by Departure Type
            viewModel.VoyagesByDepartureType = await _context.Voyages
                .GroupBy(v => v.DepartureType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);

            // Camions by Type
            viewModel.CamionsByType = await _context.Camions
                .Where(c => !string.IsNullOrEmpty(c.CamionType))
                .GroupBy(c => c.CamionType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type!, x => x.Count);

            // Camions in use (with active voyages)
            var camionsInUse = await _context.Voyages
                .Where(v => v.VoyageStatus == "InProgress" && (v.CamionFirstDepart != null || v.CamionSecondDepart != null))
                .SelectMany(v => new[] { v.CamionFirstDepart, v.CamionSecondDepart }.Where(c => c.HasValue).Select(c => c!.Value))
                .Distinct()
                .CountAsync();

            viewModel.CamionsInUse = camionsInUse;
            viewModel.AvailableCamions = viewModel.ActiveCamions - camionsInUse;

            // Recent Bookings
            viewModel.RecentBookings = await _context.Bookings
                .Include(b => b.Society)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new RecentBooking
                {
                    BookingReference = b.BookingReference,
                    SocietyName = b.Society!.SocietyName,
                    TypeVoyage = b.TypeVoyage,
                    NbrLTC = b.Nbr_LTC,
                    CreatedAt = b.CreatedAt,
                    BookingStatus = b.BookingStatus
                })
                .ToListAsync();

            // Recent Voyages
            viewModel.RecentVoyages = await _context.Voyages
                .Include(v => v.Booking)
                .OrderByDescending(v => v.CreatedAt)
                .Take(5)
                .Select(v => new RecentVoyage
                {
                    VoyageId = v.VoyageId,
                    VoyageNumber = v.VoyageNumber,
                    BookingReference = v.Booking!.BookingReference,
                    DepartureCity = v.DepartureCity,
                    DepartureDate = v.DepartureDate,
                    VoyageStatus = v.VoyageStatus
                })
                .ToListAsync();

            // Top Societies
            viewModel.TopSocieties = await _context.Societies
                .Where(s => s.IsActive)
                .Select(s => new TopSociety
                {
                    SocietyName = s.SocietyName,
                    BookingCount = _context.Bookings.Count(b => b.SocietyId == s.SocietyId),
                    VoyageCount = _context.Voyages.Count(v => v.SocietyPrincipaleId == s.SocietyId)
                })
                .OrderByDescending(s => s.BookingCount)
                .Take(5)
                .ToListAsync();

            // Monthly Trends (last 6 months)
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);

            var bookingsByMonth = await _context.Bookings
                .Where(b => b.CreatedAt >= sixMonthsAgo)
                .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            viewModel.BookingsByMonth = bookingsByMonth
                .ToDictionary(
                    x => $"{x.Year}-{x.Month:D2}",
                    x => x.Count
                );

            var voyagesByMonth = await _context.Voyages
                .Where(v => v.CreatedAt >= sixMonthsAgo)
                .GroupBy(v => new { v.CreatedAt.Year, v.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            viewModel.VoyagesByMonth = voyagesByMonth
                .ToDictionary(
                    x => $"{x.Year}-{x.Month:D2}",
                    x => x.Count
                );

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
