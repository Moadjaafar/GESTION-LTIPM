using GESTION_LTIPN.Data;
using GESTION_LTIPN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GESTION_LTIPN.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SocietyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SocietyController> _logger;

        public SocietyController(ApplicationDbContext context, ILogger<SocietyController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Society/Index
        public async Task<IActionResult> Index(string? type, string? filter)
        {
            // Type: regular (default) or transport
            ViewBag.Type = type ?? "regular";
            ViewBag.Filter = filter ?? "all";

            if (type == "transport")
            {
                var queryTransp = _context.SocietiesTransp
                    .Include(s => s.Camions)
                    .AsQueryable();

                // Filter: all, active, inactive
                if (filter == "active")
                {
                    queryTransp = queryTransp.Where(s => s.IsActive);
                }
                else if (filter == "inactive")
                {
                    queryTransp = queryTransp.Where(s => !s.IsActive);
                }

                var societiesTransp = await queryTransp
                    .OrderBy(s => s.SocietyTranspName)
                    .ToListAsync();

                return View("IndexTransport", societiesTransp);
            }
            else
            {
                var query = _context.Societies.AsQueryable();

                // Filter: all, active, inactive
                if (filter == "active")
                {
                    query = query.Where(s => s.IsActive);
                }
                else if (filter == "inactive")
                {
                    query = query.Where(s => !s.IsActive);
                }

                var societies = await query
                    .OrderBy(s => s.SocietyName)
                    .ToListAsync();

                return View(societies);
            }
        }

        // GET: Society/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var society = await _context.Societies
                .Include(s => s.Users)
                .FirstOrDefaultAsync(m => m.SocietyId == id);

            if (society == null)
            {
                return NotFound();
            }

            return View(society);
        }

        // GET: Society/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Society/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Society society)
        {
            if (ModelState.IsValid)
            {
                // Check if society name already exists
                var existingSociety = await _context.Societies
                    .FirstOrDefaultAsync(s => s.SocietyName == society.SocietyName);

                if (existingSociety != null)
                {
                    ModelState.AddModelError("SocietyName", "Une société avec ce nom existe déjà.");
                    return View(society);
                }

                society.CreatedAt = DateTime.Now;
                society.UpdatedAt = DateTime.Now;

                _context.Societies.Add(society);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Society {SocietyId} created: {SocietyName}", society.SocietyId, society.SocietyName);

                TempData["SuccessMessage"] = $"Société '{society.SocietyName}' créée avec succès.";
                return RedirectToAction(nameof(Index));
            }

            return View(society);
        }

        // GET: Society/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var society = await _context.Societies.FindAsync(id);

            if (society == null)
            {
                return NotFound();
            }

            return View(society);
        }

        // POST: Society/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Society society)
        {
            if (id != society.SocietyId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if society name already exists (excluding current society)
                    var existingSociety = await _context.Societies
                        .FirstOrDefaultAsync(s => s.SocietyName == society.SocietyName && s.SocietyId != id);

                    if (existingSociety != null)
                    {
                        ModelState.AddModelError("SocietyName", "Une société avec ce nom existe déjà.");
                        return View(society);
                    }

                    society.UpdatedAt = DateTime.Now;

                    _context.Update(society);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Society {SocietyId} updated: {SocietyName}", society.SocietyId, society.SocietyName);

                    TempData["SuccessMessage"] = $"Société '{society.SocietyName}' mise à jour avec succès.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await SocietyExists(society.SocietyId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(society);
        }

        // POST: Society/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var society = await _context.Societies.FindAsync(id);

            if (society == null)
            {
                return NotFound();
            }

            society.IsActive = !society.IsActive;
            society.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var status = society.IsActive ? "activée" : "désactivée";
            _logger.LogInformation("Society {SocietyId} status changed to {IsActive}", society.SocietyId, society.IsActive);

            TempData["SuccessMessage"] = $"Société '{society.SocietyName}' {status} avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Society/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var society = await _context.Societies
                .Include(s => s.Users)
                .FirstOrDefaultAsync(s => s.SocietyId == id);

            if (society == null)
            {
                return NotFound();
            }

            // Check if society has users
            if (society.Users.Any())
            {
                TempData["ErrorMessage"] = $"Impossible de supprimer '{society.SocietyName}' car elle a des utilisateurs associés. Désactivez-la plutôt.";
                return RedirectToAction(nameof(Index));
            }

            // Check if society has bookings
            var hasBookings = await _context.Bookings.AnyAsync(b => b.SocietyId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = $"Impossible de supprimer '{society.SocietyName}' car elle a des réservations associées. Désactivez-la plutôt.";
                return RedirectToAction(nameof(Index));
            }

            _context.Societies.Remove(society);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Society {SocietyId} deleted: {SocietyName}", id, society.SocietyName);

            TempData["SuccessMessage"] = $"Société '{society.SocietyName}' supprimée avec succès.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> SocietyExists(int id)
        {
            return await _context.Societies.AnyAsync(e => e.SocietyId == id);
        }

        // =====================================================
        // TRANSPORT SOCIETIES METHODS
        // =====================================================

        // GET: Society/CreateTransport
        public IActionResult CreateTransport()
        {
            return View();
        }

        // POST: Society/CreateTransport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTransport(SocietyTransp society)
        {
            if (ModelState.IsValid)
            {
                // Check if transport society name already exists
                var existingSociety = await _context.SocietiesTransp
                    .FirstOrDefaultAsync(s => s.SocietyTranspName == society.SocietyTranspName);

                if (existingSociety != null)
                {
                    ModelState.AddModelError("SocietyTranspName", "Une société de transport avec ce nom existe déjà.");
                    return View(society);
                }

                society.CreatedAt = DateTime.Now;
                society.UpdatedAt = DateTime.Now;

                _context.SocietiesTransp.Add(society);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Transport Society {SocietyTranspId} created: {SocietyTranspName}",
                    society.SocietyTranspId, society.SocietyTranspName);

                TempData["SuccessMessage"] = $"Société de transport '{society.SocietyTranspName}' créée avec succès.";
                return RedirectToAction(nameof(Index), new { type = "transport" });
            }

            return View(society);
        }

        // GET: Society/EditTransport/5
        public async Task<IActionResult> EditTransport(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var society = await _context.SocietiesTransp.FindAsync(id);

            if (society == null)
            {
                return NotFound();
            }

            return View(society);
        }

        // POST: Society/EditTransport/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTransport(int id, SocietyTransp society)
        {
            if (id != society.SocietyTranspId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if transport society name already exists (excluding current society)
                    var existingSociety = await _context.SocietiesTransp
                        .FirstOrDefaultAsync(s => s.SocietyTranspName == society.SocietyTranspName
                                                && s.SocietyTranspId != id);

                    if (existingSociety != null)
                    {
                        ModelState.AddModelError("SocietyTranspName", "Une société de transport avec ce nom existe déjà.");
                        return View(society);
                    }

                    society.UpdatedAt = DateTime.Now;

                    _context.Update(society);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Transport Society {SocietyTranspId} updated: {SocietyTranspName}",
                        society.SocietyTranspId, society.SocietyTranspName);

                    TempData["SuccessMessage"] = $"Société de transport '{society.SocietyTranspName}' mise à jour avec succès.";
                    return RedirectToAction(nameof(Index), new { type = "transport" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await SocietyTranspExists(society.SocietyTranspId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(society);
        }

        // POST: Society/ToggleStatusTransport/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatusTransport(int id)
        {
            var society = await _context.SocietiesTransp.FindAsync(id);

            if (society == null)
            {
                return NotFound();
            }

            society.IsActive = !society.IsActive;
            society.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var status = society.IsActive ? "activée" : "désactivée";
            _logger.LogInformation("Transport Society {SocietyTranspId} status changed to {IsActive}",
                society.SocietyTranspId, society.IsActive);

            TempData["SuccessMessage"] = $"Société de transport '{society.SocietyTranspName}' {status} avec succès.";
            return RedirectToAction(nameof(Index), new { type = "transport" });
        }

        // POST: Society/DeleteTransport/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTransport(int id)
        {
            var society = await _context.SocietiesTransp
                .Include(s => s.Camions)
                .FirstOrDefaultAsync(s => s.SocietyTranspId == id);

            if (society == null)
            {
                return NotFound();
            }

            // Check if society has trucks
            if (society.Camions.Any())
            {
                TempData["ErrorMessage"] = $"Impossible de supprimer '{society.SocietyTranspName}' car elle a des camions associés. Désactivez-la plutôt.";
                return RedirectToAction(nameof(Index), new { type = "transport" });
            }

            _context.SocietiesTransp.Remove(society);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Transport Society {SocietyTranspId} deleted: {SocietyTranspName}",
                id, society.SocietyTranspName);

            TempData["SuccessMessage"] = $"Société de transport '{society.SocietyTranspName}' supprimée avec succès.";
            return RedirectToAction(nameof(Index), new { type = "transport" });
        }

        // GET: Society/DetailsTransport/5
        public async Task<IActionResult> DetailsTransport(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var society = await _context.SocietiesTransp
                .Include(s => s.Camions)
                .FirstOrDefaultAsync(m => m.SocietyTranspId == id);

            if (society == null)
            {
                return NotFound();
            }

            return View(society);
        }

        private async Task<bool> SocietyTranspExists(int id)
        {
            return await _context.SocietiesTransp.AnyAsync(e => e.SocietyTranspId == id);
        }
    }
}
