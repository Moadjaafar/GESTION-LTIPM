using GESTION_LTIPN.Data;
using GESTION_LTIPN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace GESTION_LTIPN.Controllers
{
    [Authorize]
    public class EtatSuiviVoyageController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EtatSuiviVoyageController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: EtatSuiviVoyage
        public async Task<IActionResult> Index(DateTime? dateDebut, DateTime? dateFin, string? bookingReference,
            int? societyId, string? voyageStatus, string? typeVoyage, string? departureCity)
        {
            var viewModel = new EtatSuiviVoyageViewModel
            {
                DateDebut = dateDebut,
                DateFin = dateFin,
                BookingReference = bookingReference,
                SocietyId = societyId,
                VoyageStatus = voyageStatus,
                TypeVoyage = typeVoyage,
                DepartureCity = departureCity
            };

            // Load filter dropdowns
            viewModel.Societies = await _context.Societies
                .Where(s => s.IsActive)
                .OrderBy(s => s.SocietyName)
                .ToListAsync();

            // If filters are applied, load data
            if (dateDebut.HasValue || dateFin.HasValue || !string.IsNullOrEmpty(bookingReference) ||
                societyId.HasValue || !string.IsNullOrEmpty(voyageStatus) || !string.IsNullOrEmpty(typeVoyage) ||
                !string.IsNullOrEmpty(departureCity))
            {
                viewModel.Voyages = await GetFilteredVoyages(dateDebut, dateFin, bookingReference,
                    societyId, voyageStatus, typeVoyage, departureCity);
                viewModel.FilteredTitle = BuildFilterTitle(dateDebut, dateFin, bookingReference,
                    societyId, voyageStatus, typeVoyage, departureCity);
            }

            return View(viewModel);
        }

        private async Task<List<VoyageItemViewModel>> GetFilteredVoyages(DateTime? dateDebut, DateTime? dateFin,
            string? bookingReference, int? societyId, string? voyageStatus, string? typeVoyage, string? departureCity)
        {
            var query = _context.Voyages
                .Include(v => v.Booking)
                .Include(v => v.SocietyPrincipale)
                .Include(v => v.SocietySecondaire)
                .Include(v => v.CamionFirst)
                .Include(v => v.CamionSecond)
                .AsQueryable();

            // Apply filters
            if (dateDebut.HasValue && dateFin.HasValue)
            {
                query = query.Where(v => v.DepartureDate >= dateDebut && v.DepartureDate <= dateFin);
            }
            else if (dateDebut.HasValue)
            {
                query = query.Where(v => v.DepartureDate >= dateDebut);
            }

            if (!string.IsNullOrEmpty(bookingReference))
            {
                query = query.Where(v => v.Booking!.BookingReference.Contains(bookingReference));
            }

            if (societyId.HasValue)
            {
                query = query.Where(v => v.SocietyPrincipaleId == societyId || v.SocietySecondaireId == societyId);
            }

            if (!string.IsNullOrEmpty(voyageStatus))
            {
                query = query.Where(v => v.VoyageStatus == voyageStatus);
            }

            if (!string.IsNullOrEmpty(typeVoyage))
            {
                query = query.Where(v => v.Booking!.TypeVoyage == typeVoyage);
            }

            if (!string.IsNullOrEmpty(departureCity))
            {
                query = query.Where(v => v.DepartureCity == departureCity);
            }

            var voyages = await query
                .OrderByDescending(v => v.DepartureDate)
                .ThenBy(v => v.VoyageNumber)
                .ToListAsync();

            var voyageItems = new List<VoyageItemViewModel>();

            foreach (var voyage in voyages)
            {
                // Calculate durations
                string? dureeAllerDakhla = null;
                string? dureeSejourDakhla = null;
                string? dureeRetour = null;
                string? dureeTotale = null;

                if (voyage.DepartureDate.HasValue && voyage.ReceptionDate.HasValue)
                {
                    var departureDateTime = voyage.DepartureDate.Value.Add(voyage.DepartureTime ?? TimeSpan.Zero);
                    var receptionDateTime = voyage.ReceptionDate.Value.Add(voyage.ReceptionTime ?? TimeSpan.Zero);
                    var duree = receptionDateTime - departureDateTime;
                    dureeAllerDakhla = $"{(int)duree.TotalHours}h {duree.Minutes}m";
                }

                if (voyage.ReceptionDate.HasValue && voyage.ReturnDepartureDate.HasValue)
                {
                    var receptionDateTime = voyage.ReceptionDate.Value.Add(voyage.ReceptionTime ?? TimeSpan.Zero);
                    var returnDepartureDateTime = voyage.ReturnDepartureDate.Value.Add(voyage.ReturnDepartureTime ?? TimeSpan.Zero);
                    var duree = returnDepartureDateTime - receptionDateTime;
                    dureeSejourDakhla = $"{(int)duree.TotalHours}h {duree.Minutes}m";
                }

                if (voyage.ReturnDepartureDate.HasValue && voyage.ReturnArrivalDate.HasValue)
                {
                    var returnDepartureDateTime = voyage.ReturnDepartureDate.Value.Add(voyage.ReturnDepartureTime ?? TimeSpan.Zero);
                    var returnArrivalDateTime = voyage.ReturnArrivalDate.Value.Add(voyage.ReturnArrivalTime ?? TimeSpan.Zero);
                    var duree = returnArrivalDateTime - returnDepartureDateTime;
                    dureeRetour = $"{(int)duree.TotalHours}h {duree.Minutes}m";
                }

                if (voyage.DepartureDate.HasValue && voyage.ReturnArrivalDate.HasValue)
                {
                    var departureDateTime = voyage.DepartureDate.Value.Add(voyage.DepartureTime ?? TimeSpan.Zero);
                    var returnArrivalDateTime = voyage.ReturnArrivalDate.Value.Add(voyage.ReturnArrivalTime ?? TimeSpan.Zero);
                    var duree = returnArrivalDateTime - departureDateTime;
                    dureeTotale = $"{(int)duree.TotalHours}h {duree.Minutes}m";
                }

                // Get truck matricules
                var camionFirstMatricule = voyage.CamionFirst?.CamionMatricule ?? "-";
                var camionSecondMatricule = voyage.CamionSecond?.CamionMatricule ?? "-";

                // If voyage has secondary society (Emballage), create TWO separate operations
                if (voyage.SocietySecondaireId.HasValue && voyage.SocietySecondaire != null)
                {
                    // Operation 1: For Principal Society
                    voyageItems.Add(new VoyageItemViewModel
                    {
                        VoyageId = voyage.VoyageId,
                        VoyageNumber = voyage.VoyageNumber,
                        Numero_TC = voyage.Numero_TC,
                        VoyageStatus = voyage.VoyageStatus,
                        BookingId = voyage.BookingId,
                        BookingReference = voyage.Booking?.BookingReference,
                        TypeVoyage = "EMBALLAGE",
                        SocietyPrincipale = voyage.SocietyPrincipale?.SocietyName,
                        SocietySecondaire = null, // Each operation shows only one society
                        DepartureType = voyage.DepartureType,
                        Type_Emballage = voyage.Type_Emballage,
                        DepartureCity = voyage.DepartureCity,
                        DepartureDate = voyage.DepartureDate?.ToString("dd/MM/yyyy"),
                        DepartureTime = voyage.DepartureTime?.ToString(@"hh\:mm"),
                        CamionFirstMatricule = camionFirstMatricule,
                        CamionFirstDriver = voyage.CamionFirst?.DriverName,
                        ReceptionDate = voyage.ReceptionDate?.ToString("dd/MM/yyyy"),
                        ReceptionTime = voyage.ReceptionTime?.ToString(@"hh\:mm"),
                        ReturnDepartureDate = voyage.ReturnDepartureDate?.ToString("dd/MM/yyyy"),
                        ReturnDepartureTime = voyage.ReturnDepartureTime?.ToString(@"hh\:mm"),
                        CamionSecondMatricule = camionSecondMatricule,
                        CamionSecondDriver = voyage.CamionSecond?.DriverName,
                        ReturnArrivalCity = voyage.ReturnArrivalCity,
                        ReturnArrivalDate = voyage.ReturnArrivalDate?.ToString("dd/MM/yyyy"),
                        ReturnArrivalTime = voyage.ReturnArrivalTime?.ToString(@"hh\:mm"),
                        PricePrincipale = voyage.PricePrincipale, // Show principal price
                        PriceSecondaire = null, // Don't show secondary price here
                        Currency = voyage.Currency,
                        DureeAllerDakhla = dureeAllerDakhla,
                        DureeSejourDakhla = dureeSejourDakhla,
                        DureeRetour = dureeRetour,
                        DureeTotale = dureeTotale,
                        CreatedAt = voyage.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                    });

                    // Operation 2: For Secondary Society
                    voyageItems.Add(new VoyageItemViewModel
                    {
                        VoyageId = voyage.VoyageId,
                        VoyageNumber = voyage.VoyageNumber,
                        Numero_TC = voyage.Numero_TC,
                        VoyageStatus = voyage.VoyageStatus,
                        BookingId = voyage.BookingId,
                        BookingReference = voyage.Booking?.BookingReference,
                        TypeVoyage = "EMBALLAGE",
                        SocietyPrincipale = voyage.SocietySecondaire?.SocietyName, // Show as principal for this operation
                        SocietySecondaire = null,
                        DepartureType = voyage.DepartureType,
                        Type_Emballage = voyage.Type_Emballage,
                        DepartureCity = voyage.DepartureCity,
                        DepartureDate = voyage.DepartureDate?.ToString("dd/MM/yyyy"),
                        DepartureTime = voyage.DepartureTime?.ToString(@"hh\:mm"),
                        CamionFirstMatricule = camionFirstMatricule,
                        CamionFirstDriver = voyage.CamionFirst?.DriverName,
                        ReceptionDate = voyage.ReceptionDate?.ToString("dd/MM/yyyy"),
                        ReceptionTime = voyage.ReceptionTime?.ToString(@"hh\:mm"),
                        ReturnDepartureDate = voyage.ReturnDepartureDate?.ToString("dd/MM/yyyy"),
                        ReturnDepartureTime = voyage.ReturnDepartureTime?.ToString(@"hh\:mm"),
                        CamionSecondMatricule = camionSecondMatricule,
                        CamionSecondDriver = voyage.CamionSecond?.DriverName,
                        ReturnArrivalCity = voyage.ReturnArrivalCity,
                        ReturnArrivalDate = voyage.ReturnArrivalDate?.ToString("dd/MM/yyyy"),
                        ReturnArrivalTime = voyage.ReturnArrivalTime?.ToString(@"hh\:mm"),
                        PricePrincipale = voyage.PriceSecondaire, // Show secondary price as principal for this operation
                        PriceSecondaire = null,
                        Currency = voyage.Currency,
                        DureeAllerDakhla = dureeAllerDakhla,
                        DureeSejourDakhla = dureeSejourDakhla,
                        DureeRetour = dureeRetour,
                        DureeTotale = dureeTotale,
                        CreatedAt = voyage.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                    });
                }
                else
                {
                    // Single operation for voyages without secondary society
                    voyageItems.Add(new VoyageItemViewModel
                    {
                        VoyageId = voyage.VoyageId,
                        VoyageNumber = voyage.VoyageNumber,
                        Numero_TC = voyage.Numero_TC,
                        VoyageStatus = voyage.VoyageStatus,
                        BookingId = voyage.BookingId,
                        BookingReference = voyage.Booking?.BookingReference,
                        TypeVoyage = voyage.Booking?.TypeVoyage,
                        SocietyPrincipale = voyage.SocietyPrincipale?.SocietyName,
                        SocietySecondaire = null,
                        DepartureType = voyage.DepartureType,
                        Type_Emballage = voyage.Type_Emballage,
                        DepartureCity = voyage.DepartureCity,
                        DepartureDate = voyage.DepartureDate?.ToString("dd/MM/yyyy"),
                        DepartureTime = voyage.DepartureTime?.ToString(@"hh\:mm"),
                        CamionFirstMatricule = camionFirstMatricule,
                        CamionFirstDriver = voyage.CamionFirst?.DriverName,
                        ReceptionDate = voyage.ReceptionDate?.ToString("dd/MM/yyyy"),
                        ReceptionTime = voyage.ReceptionTime?.ToString(@"hh\:mm"),
                        ReturnDepartureDate = voyage.ReturnDepartureDate?.ToString("dd/MM/yyyy"),
                        ReturnDepartureTime = voyage.ReturnDepartureTime?.ToString(@"hh\:mm"),
                        CamionSecondMatricule = camionSecondMatricule,
                        CamionSecondDriver = voyage.CamionSecond?.DriverName,
                        ReturnArrivalCity = voyage.ReturnArrivalCity,
                        ReturnArrivalDate = voyage.ReturnArrivalDate?.ToString("dd/MM/yyyy"),
                        ReturnArrivalTime = voyage.ReturnArrivalTime?.ToString(@"hh\:mm"),
                        PricePrincipale = voyage.PricePrincipale,
                        PriceSecondaire = null,
                        Currency = voyage.Currency,
                        DureeAllerDakhla = dureeAllerDakhla,
                        DureeSejourDakhla = dureeSejourDakhla,
                        DureeRetour = dureeRetour,
                        DureeTotale = dureeTotale,
                        CreatedAt = voyage.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                    });
                }
            }

            return voyageItems;
        }

        private string BuildFilterTitle(DateTime? dateDebut, DateTime? dateFin, string? bookingReference,
            int? societyId, string? voyageStatus, string? typeVoyage, string? departureCity)
        {
            var titleParts = new List<string> { "État de suivi des voyages" };

            if (dateDebut.HasValue && dateFin.HasValue)
            {
                titleParts.Add($"entre {dateDebut.Value:dd/MM/yyyy} et {dateFin.Value:dd/MM/yyyy}");
            }
            else if (dateDebut.HasValue)
            {
                titleParts.Add($"à partir du {dateDebut.Value:dd/MM/yyyy}");
            }

            if (!string.IsNullOrEmpty(bookingReference))
            {
                titleParts.Add($"Booking: {bookingReference}");
            }

            if (societyId.HasValue)
            {
                var society = _context.Societies.Find(societyId.Value);
                if (society != null)
                {
                    titleParts.Add($"Société: {society.SocietyName}");
                }
            }

            if (!string.IsNullOrEmpty(voyageStatus))
            {
                titleParts.Add($"Statut: {voyageStatus}");
            }

            if (!string.IsNullOrEmpty(typeVoyage))
            {
                titleParts.Add($"Type: {typeVoyage}");
            }

            if (!string.IsNullOrEmpty(departureCity))
            {
                titleParts.Add($"Départ: {departureCity}");
            }

            return string.Join(" - ", titleParts);
        }

        // GET: EtatSuiviVoyage/ExportExcel
        public async Task<IActionResult> ExportExcel(DateTime? dateDebut, DateTime? dateFin, string? bookingReference,
            int? societyId, string? voyageStatus, string? typeVoyage, string? departureCity)
        {
            var voyages = await GetFilteredVoyages(dateDebut, dateFin, bookingReference,
                societyId, voyageStatus, typeVoyage, departureCity);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("État Suivi Voyages");

                // Headers
                var headers = new[] {
                    "ID Voyage", "N° Voyage", "N° TC", "Statut Voyage",
                    "Référence Booking", "Type Voyage",
                    "Société Principale", "Société Secondaire",
                    "Type Départ", "Type Emballage",
                    "Ville Départ", "Date Départ", "Heure Départ",
                    "Camion Départ", "Chauffeur Départ",
                    "Date Réception", "Heure Réception",
                    "Date Départ Retour", "Heure Départ Retour",
                    "Camion Retour", "Chauffeur Retour",
                    "Ville Arrivée", "Date Arrivée", "Heure Arrivée",
                    "Prix Principal", "Prix Secondaire", "Devise",
                    "Durée Aller Dakhla", "Durée Séjour Dakhla", "Durée Retour", "Durée Totale",
                    "Date Création"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    var headerCell = worksheet.Cell(1, i + 1);
                    headerCell.Value = headers[i];
                    headerCell.Style.Font.Bold = true;
                    headerCell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // Data rows
                int currentRow = 2;
                foreach (var voyage in voyages)
                {
                    worksheet.Cell(currentRow, 1).Value = voyage.VoyageId;
                    worksheet.Cell(currentRow, 2).Value = voyage.VoyageNumber;
                    worksheet.Cell(currentRow, 3).Value = voyage.Numero_TC ?? "";
                    worksheet.Cell(currentRow, 4).Value = voyage.VoyageStatus ?? "";
                    worksheet.Cell(currentRow, 5).Value = voyage.BookingReference ?? "";
                    worksheet.Cell(currentRow, 6).Value = voyage.TypeVoyage ?? "";
                    worksheet.Cell(currentRow, 7).Value = voyage.SocietyPrincipale ?? "";
                    worksheet.Cell(currentRow, 8).Value = voyage.SocietySecondaire ?? "";
                    worksheet.Cell(currentRow, 9).Value = voyage.DepartureType ?? "";
                    worksheet.Cell(currentRow, 10).Value = voyage.Type_Emballage ?? "";
                    worksheet.Cell(currentRow, 11).Value = voyage.DepartureCity ?? "";
                    worksheet.Cell(currentRow, 12).Value = voyage.DepartureDate ?? "";
                    worksheet.Cell(currentRow, 13).Value = voyage.DepartureTime ?? "";
                    worksheet.Cell(currentRow, 14).Value = voyage.CamionFirstMatricule ?? "";
                    worksheet.Cell(currentRow, 15).Value = voyage.CamionFirstDriver ?? "";
                    worksheet.Cell(currentRow, 16).Value = voyage.ReceptionDate ?? "";
                    worksheet.Cell(currentRow, 17).Value = voyage.ReceptionTime ?? "";
                    worksheet.Cell(currentRow, 18).Value = voyage.ReturnDepartureDate ?? "";
                    worksheet.Cell(currentRow, 19).Value = voyage.ReturnDepartureTime ?? "";
                    worksheet.Cell(currentRow, 20).Value = voyage.CamionSecondMatricule ?? "";
                    worksheet.Cell(currentRow, 21).Value = voyage.CamionSecondDriver ?? "";
                    worksheet.Cell(currentRow, 22).Value = voyage.ReturnArrivalCity ?? "";
                    worksheet.Cell(currentRow, 23).Value = voyage.ReturnArrivalDate ?? "";
                    worksheet.Cell(currentRow, 24).Value = voyage.ReturnArrivalTime ?? "";
                    worksheet.Cell(currentRow, 25).Value = voyage.PricePrincipale ?? 0;
                    worksheet.Cell(currentRow, 26).Value = voyage.PriceSecondaire ?? 0;
                    worksheet.Cell(currentRow, 27).Value = voyage.Currency ?? "";
                    worksheet.Cell(currentRow, 28).Value = voyage.DureeAllerDakhla ?? "";
                    worksheet.Cell(currentRow, 29).Value = voyage.DureeSejourDakhla ?? "";
                    worksheet.Cell(currentRow, 30).Value = voyage.DureeRetour ?? "";
                    worksheet.Cell(currentRow, 31).Value = voyage.DureeTotale ?? "";
                    worksheet.Cell(currentRow, 32).Value = voyage.CreatedAt ?? "";

                    currentRow++;
                }

                worksheet.ColumnsUsed().AdjustToContents();

                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var title = BuildFilterTitle(dateDebut, dateFin, bookingReference, societyId, voyageStatus, typeVoyage, departureCity);
                string fileName = $"{title}_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}
