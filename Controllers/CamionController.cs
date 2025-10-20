using GESTION_LTIPN.Data;
using GESTION_LTIPN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GESTION_LTIPN.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CamionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CamionController> _logger;

        public CamionController(ApplicationDbContext context, ILogger<CamionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Camion/Index
        public async Task<IActionResult> Index(string? filter, int? societyTranspId)
        {
            var query = _context.Camions
                .Include(c => c.SocietyTransp)
                .AsQueryable();

            // Filter by status
            if (filter == "active")
            {
                query = query.Where(c => c.IsActive);
            }
            else if (filter == "inactive")
            {
                query = query.Where(c => !c.IsActive);
            }

            // Filter by transport society
            if (societyTranspId.HasValue)
            {
                query = query.Where(c => c.SocietyTranspId == societyTranspId);
            }

            var camions = await query
                .OrderBy(c => c.CamionMatricule)
                .ToListAsync();

            // For transport society dropdown filter
            ViewBag.SocietiesTransp = await _context.SocietiesTransp
                .Where(s => s.IsActive)
                .OrderBy(s => s.SocietyTranspName)
                .ToListAsync();

            ViewBag.Filter = filter ?? "all";
            ViewBag.SelectedSocietyTranspId = societyTranspId;

            return View(camions);
        }

        // GET: Camion/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var camion = await _context.Camions
                .Include(c => c.SocietyTransp)
                .FirstOrDefaultAsync(m => m.CamionId == id);

            if (camion == null)
            {
                return NotFound();
            }

            return View(camion);
        }

        // GET: Camion/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.SocietiesTransp = new SelectList(
                await _context.SocietiesTransp.Where(s => s.IsActive).OrderBy(s => s.SocietyTranspName).ToListAsync(),
                "SocietyTranspId",
                "SocietyTranspName"
            );

            ViewBag.CamionTypes = new List<string>
            {
                "INTERN",
                "EXTERN"
            };

            return View();
        }

        // POST: Camion/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Camion camion)
        {
            if (ModelState.IsValid)
            {
                // Check if matricule already exists
                var existingCamion = await _context.Camions
                    .FirstOrDefaultAsync(c => c.CamionMatricule == camion.CamionMatricule);

                if (existingCamion != null)
                {
                    ModelState.AddModelError("CamionMatricule", "Un camion avec cette matricule existe déjà.");
                    await LoadCreateViewData();
                    return View(camion);
                }

                camion.CreatedAt = DateTime.Now;
                camion.UpdatedAt = DateTime.Now;

                _context.Camions.Add(camion);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Camion {CamionId} created: {CamionMatricule}", camion.CamionId, camion.CamionMatricule);

                TempData["SuccessMessage"] = $"Camion '{camion.CamionMatricule}' créé avec succès.";
                return RedirectToAction(nameof(Index));
            }

            await LoadCreateViewData();
            return View(camion);
        }

        // GET: Camion/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var camion = await _context.Camions.FindAsync(id);

            if (camion == null)
            {
                return NotFound();
            }

            await LoadEditViewData();
            return View(camion);
        }

        // POST: Camion/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Camion camion)
        {
            if (id != camion.CamionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if matricule already exists (excluding current camion)
                    var existingCamion = await _context.Camions
                        .FirstOrDefaultAsync(c => c.CamionMatricule == camion.CamionMatricule && c.CamionId != id);

                    if (existingCamion != null)
                    {
                        ModelState.AddModelError("CamionMatricule", "Un camion avec cette matricule existe déjà.");
                        await LoadEditViewData();
                        return View(camion);
                    }

                    camion.UpdatedAt = DateTime.Now;

                    _context.Update(camion);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Camion {CamionId} updated: {CamionMatricule}", camion.CamionId, camion.CamionMatricule);

                    TempData["SuccessMessage"] = $"Camion '{camion.CamionMatricule}' mis à jour avec succès.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await CamionExists(camion.CamionId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await LoadEditViewData();
            return View(camion);
        }

        // POST: Camion/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var camion = await _context.Camions.FindAsync(id);

            if (camion == null)
            {
                return NotFound();
            }

            camion.IsActive = !camion.IsActive;
            camion.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var status = camion.IsActive ? "activé" : "désactivé";
            _logger.LogInformation("Camion {CamionId} status changed to {IsActive}", camion.CamionId, camion.IsActive);

            TempData["SuccessMessage"] = $"Camion '{camion.CamionMatricule}' {status} avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Camion/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var camion = await _context.Camions
                .FirstOrDefaultAsync(c => c.CamionId == id);

            if (camion == null)
            {
                return NotFound();
            }

            // Check if camion has voyages (as first or second truck)
            var hasVoyages = await _context.Voyages
                .AnyAsync(v => v.CamionFirstDepart == id || v.CamionSecondDepart == id);

            if (hasVoyages)
            {
                TempData["ErrorMessage"] = $"Impossible de supprimer le camion '{camion.CamionMatricule}' car il a des voyages associés. Désactivez-le plutôt.";
                return RedirectToAction(nameof(Index));
            }

            _context.Camions.Remove(camion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Camion {CamionId} deleted: {CamionMatricule}", id, camion.CamionMatricule);

            TempData["SuccessMessage"] = $"Camion '{camion.CamionMatricule}' supprimé avec succès.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> CamionExists(int id)
        {
            return await _context.Camions.AnyAsync(e => e.CamionId == id);
        }

        private async Task LoadCreateViewData()
        {
            ViewBag.SocietiesTransp = new SelectList(
                await _context.SocietiesTransp.Where(s => s.IsActive).OrderBy(s => s.SocietyTranspName).ToListAsync(),
                "SocietyTranspId",
                "SocietyTranspName"
            );

            ViewBag.CamionTypes = new List<string>
            {
                "INTERN",
                "EXTERN"
            };
        }

        private async Task LoadEditViewData()
        {
            ViewBag.SocietiesTransp = new SelectList(
                await _context.SocietiesTransp.Where(s => s.IsActive).OrderBy(s => s.SocietyTranspName).ToListAsync(),
                "SocietyTranspId",
                "SocietyTranspName"
            );

            ViewBag.CamionTypes = new List<string>
            {
                "INTERN",
                "EXTERN"
            };
        }
    }
}
