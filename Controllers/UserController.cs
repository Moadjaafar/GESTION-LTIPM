using GESTION_LTIPN.Data;
using GESTION_LTIPN.Models;
using GESTION_LTIPN.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GESTION_LTIPN.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserController> _logger;
        private readonly IEmailService _emailService;

        public UserController(ApplicationDbContext context, ILogger<UserController> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        // GET: User/Index
        public async Task<IActionResult> Index(string? filter, string? role)
        {
            var query = _context.Users
                .Include(u => u.Society)
                .AsQueryable();

            // Filter by status
            if (filter == "active")
            {
                query = query.Where(u => u.IsActive);
            }
            else if (filter == "inactive")
            {
                query = query.Where(u => !u.IsActive);
            }

            // Filter by role
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role);
            }

            var users = await query
                .OrderBy(u => u.Username)
                .ToListAsync();

            ViewBag.Filter = filter ?? "all";
            ViewBag.SelectedRole = role;

            return View(users);
        }

        // GET: User/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Society)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: User/Create
        public async Task<IActionResult> Create()
        {
            await LoadViewData();
            return View();
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            if (ModelState.IsValid)
            {
                // Check if username already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == user.Username);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Ce nom d'utilisateur existe déjà.");
                    await LoadViewData();
                    return View(user);
                }

                user.CreatedAt = DateTime.Now;
                user.UpdatedAt = DateTime.Now;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} created: {Username} with role {Role}", user.UserId, user.Username, user.Role);

                // Send account creation email
                if (!string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendAccountCreatedEmailAsync(user.Email, user);
                }

                TempData["SuccessMessage"] = $"Utilisateur '{user.Username}' créé avec succès.";
                return RedirectToAction(nameof(Index));
            }

            await LoadViewData();
            return View(user);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            await LoadViewData();
            return View(user);
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if username already exists (excluding current user)
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == user.Username && u.UserId != id);

                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Username", "Ce nom d'utilisateur existe déjà.");
                        await LoadViewData();
                        return View(user);
                    }

                    user.UpdatedAt = DateTime.Now;

                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserId} updated: {Username}", user.UserId, user.Username);

                    TempData["SuccessMessage"] = $"Utilisateur '{user.Username}' mis à jour avec succès.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await UserExists(user.UserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await LoadViewData();
            return View(user);
        }

        // POST: User/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var status = user.IsActive ? "activé" : "désactivé";
            _logger.LogInformation("User {UserId} status changed to {IsActive}", user.UserId, user.IsActive);

            TempData["SuccessMessage"] = $"Utilisateur '{user.Username}' {status} avec succès.";
            return RedirectToAction(nameof(Index));
        }

        // POST: User/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            {
                TempData["ErrorMessage"] = "Le mot de passe doit contenir au moins 4 caractères.";
                return RedirectToAction(nameof(Details), new { id });
            }

            user.Password = newPassword;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} password reset", user.UserId);

            TempData["SuccessMessage"] = $"Mot de passe de '{user.Username}' réinitialisé avec succès.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: User/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Prevent deleting the last admin
            if (user.Role == "Admin")
            {
                var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin" && u.IsActive);
                if (adminCount <= 1)
                {
                    TempData["ErrorMessage"] = "Impossible de supprimer le dernier administrateur actif.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Check if user has created bookings
            var hasBookings = await _context.Bookings.AnyAsync(b => b.CreatedByUserId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = $"Impossible de supprimer '{user.Username}' car cet utilisateur a créé des réservations. Désactivez-le plutôt.";
                return RedirectToAction(nameof(Index));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deleted: {Username}", id, user.Username);

            TempData["SuccessMessage"] = $"Utilisateur '{user.Username}' supprimé avec succès.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> UserExists(int id)
        {
            return await _context.Users.AnyAsync(e => e.UserId == id);
        }

        private async Task LoadViewData()
        {
            // Roles dropdown
            ViewBag.Roles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Admin", Text = "Administrateur" },
                new SelectListItem { Value = "Booking_Agent", Text = "Agent de Réservation" },
                new SelectListItem { Value = "Trans_Respo", Text = "Responsable Transport" }
            };

            // Societies dropdown
            ViewBag.Societies = new SelectList(
                await _context.Societies.Where(s => s.IsActive).OrderBy(s => s.SocietyName).ToListAsync(),
                "SocietyId",
                "SocietyName"
            );
        }
    }
}
