using GESTION_LTIPN.Data;
using GESTION_LTIPN.Models.Stock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using ClosedXML.Excel;

namespace GESTION_LTIPN.Controllers
{
    public class EtatSuiviTransportController : Controller
    {
        private readonly StockDbContext _stockContext;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public EtatSuiviTransportController(StockDbContext stockContext, IConfiguration configuration)
        {
            _stockContext = stockContext;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("StockConnection") ?? "";
        }

        // GET: EtatSuiviTransport
        public async Task<IActionResult> Index(DateTime? dateDebut, DateTime? dateFin, int? camionId)
        {
            var viewModel = new EtatSuiviTransportViewModel
            {
                DateDebut = dateDebut ?? DateTime.Now.AddDays(-7),
                DateFin = dateFin ?? DateTime.Now,
                CamionId = camionId
            };

            // Load camions for dropdown
            viewModel.Camions = await _stockContext.Camions
                .Where(c => c.Actif)
                .OrderBy(c => c.NumeroCamion)
                .ToListAsync();

            // Load transport data
            var transports = await GetTransportData(viewModel.DateDebut, viewModel.DateFin, camionId);
            viewModel.Transports = transports;

            // Calculate statistics
            viewModel.TotalDeparts = transports.Count;
            viewModel.EnTransit = transports.Count(t => t.EtatTransfert == "En Transit");
            viewModel.Livres = transports.Count(t => t.EtatTransfert == "Livré");
            viewModel.TotalPalettes = transports.Sum(t => t.NombrePalettes ?? 0);

            return View(viewModel);
        }

        private async Task<List<TransportItemViewModel>> GetTransportData(DateTime? dateDebut, DateTime? dateFin, int? camionId)
        {
            var transports = new List<TransportItemViewModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT
                        dc.IdDepart,
                        dc.NumeroBon,
                        c.NumeroCamion,
                        c.Chauffeur,
                        c.TypeCamion,
                        dc.DateDepart,
                        CONVERT(varchar(5), dc.HeureDepart, 108) AS HeureDepart,
                        rc.DateReception,
                        CONVERT(varchar(5), rc.HeureReception, 108) AS HeureReception,
                        dc.NombrePalettes,
                        rc.NombrePalettesRecues,
                        dc.Destination,
                        lis.DateHeureArrivee,
                        lis.DateHeureFin,
                        CASE
                            WHEN lis.DateHeureArrivee IS NOT NULL AND lis.DateHeureFin IS NOT NULL THEN
                                FORMAT(DATEDIFF(MINUTE, lis.DateHeureArrivee, lis.DateHeureFin) / 60, '0') + ':' +
                                FORMAT(DATEDIFF(MINUTE, lis.DateHeureArrivee, lis.DateHeureFin) % 60, '00')
                            ELSE
                                '--'
                        END AS DureeTransfert,
                        CASE
                            WHEN rc.DateReception IS NULL THEN 'En Transit'
                            ELSE 'Livré'
                        END AS EtatTransfert,
                        rc.Observations,
                        ISNULL(lis.PriceBooking, rc.PriceBooking) AS PriceBooking,
                        rc.IdReception
                    FROM DepartsCamions dc
                    INNER JOIN Camions c ON dc.IdCamion = c.IdCamion
                    LEFT JOIN ReceptionsCamions rc ON dc.IdDepart = rc.IdDepart
                    LEFT JOIN LogistiqueInfoStock lis ON dc.IdDepart = lis.IdDepart
                    WHERE dc.DateDepart IS NOT NULL";

                if (dateDebut.HasValue)
                    query += " AND dc.DateDepart >= @DateDebut";

                if (dateFin.HasValue)
                    query += " AND dc.DateDepart <= @DateFin";

                if (camionId.HasValue)
                    query += " AND dc.IdCamion = @CamionId";

                query += " ORDER BY dc.DateDepart DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    if (dateDebut.HasValue)
                        command.Parameters.AddWithValue("@DateDebut", dateDebut.Value);

                    if (dateFin.HasValue)
                        command.Parameters.AddWithValue("@DateFin", dateFin.Value.AddDays(1));

                    if (camionId.HasValue)
                        command.Parameters.AddWithValue("@CamionId", camionId.Value);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            transports.Add(new TransportItemViewModel
                            {
                                IdDepart = reader.GetInt32(reader.GetOrdinal("IdDepart")),
                                NumeroBon = reader.IsDBNull(reader.GetOrdinal("NumeroBon")) ? null : reader.GetValue(reader.GetOrdinal("NumeroBon")).ToString(),
                                NumeroCamion = reader.IsDBNull(reader.GetOrdinal("NumeroCamion")) ? null : reader.GetValue(reader.GetOrdinal("NumeroCamion")).ToString(),
                                Chauffeur = reader.IsDBNull(reader.GetOrdinal("Chauffeur")) ? null : reader.GetValue(reader.GetOrdinal("Chauffeur")).ToString(),
                                TypeCamion = reader.IsDBNull(reader.GetOrdinal("TypeCamion")) ? null : reader.GetValue(reader.GetOrdinal("TypeCamion")).ToString(),
                                DateDepart = reader.IsDBNull(reader.GetOrdinal("DateDepart")) ? null : reader.GetDateTime(reader.GetOrdinal("DateDepart")),
                                HeureDepart = reader.IsDBNull(reader.GetOrdinal("HeureDepart")) ? null : reader.GetValue(reader.GetOrdinal("HeureDepart")).ToString(),
                                DateReception = reader.IsDBNull(reader.GetOrdinal("DateReception")) ? null : reader.GetDateTime(reader.GetOrdinal("DateReception")),
                                HeureReception = reader.IsDBNull(reader.GetOrdinal("HeureReception")) ? null : reader.GetValue(reader.GetOrdinal("HeureReception")).ToString(),
                                NombrePalettes = reader.IsDBNull(reader.GetOrdinal("NombrePalettes")) ? null : reader.GetInt32(reader.GetOrdinal("NombrePalettes")),
                                NombrePalettesRecues = reader.IsDBNull(reader.GetOrdinal("NombrePalettesRecues")) ? null : reader.GetInt32(reader.GetOrdinal("NombrePalettesRecues")),
                                Destination = reader.IsDBNull(reader.GetOrdinal("Destination")) ? null : reader.GetValue(reader.GetOrdinal("Destination")).ToString(),
                                DateHeureArrivee = reader.IsDBNull(reader.GetOrdinal("DateHeureArrivee")) ? null : reader.GetDateTime(reader.GetOrdinal("DateHeureArrivee")),
                                DateHeureFin = reader.IsDBNull(reader.GetOrdinal("DateHeureFin")) ? null : reader.GetDateTime(reader.GetOrdinal("DateHeureFin")),
                                DureeTransfert = reader.IsDBNull(reader.GetOrdinal("DureeTransfert")) ? null : reader.GetValue(reader.GetOrdinal("DureeTransfert")).ToString(),
                                EtatTransfert = reader.GetValue(reader.GetOrdinal("EtatTransfert")).ToString() ?? "",
                                Observations = reader.IsDBNull(reader.GetOrdinal("Observations")) ? null : reader.GetValue(reader.GetOrdinal("Observations")).ToString(),
                                PriceBooking = reader.IsDBNull(reader.GetOrdinal("PriceBooking")) ? null : Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("PriceBooking"))),
                                IdReception = reader.IsDBNull(reader.GetOrdinal("IdReception")) ? null : reader.GetInt32(reader.GetOrdinal("IdReception"))
                            });
                        }
                    }
                }
            }

            return transports;
        }

        // GET: Get logistique info for modal
        [HttpGet]
        public async Task<IActionResult> GetLogistiqueInfo(int idDepart)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                        SELECT id, idDepart, DateHeureArrivee, DateHeureFin, PriceBooking, datetimeSaisie
                        FROM LogistiqueInfoStock
                        WHERE idDepart = @IdDepart";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IdDepart", idDepart);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var info = new
                                {
                                    id = reader.GetInt32(reader.GetOrdinal("id")),
                                    idDepart = reader.GetInt32(reader.GetOrdinal("idDepart")),
                                    dateHeureArrivee = reader.IsDBNull(reader.GetOrdinal("DateHeureArrivee")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateHeureArrivee")),
                                    dateHeureFin = reader.IsDBNull(reader.GetOrdinal("DateHeureFin")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateHeureFin")),
                                    priceBooking = reader.IsDBNull(reader.GetOrdinal("PriceBooking")) ? (double?)null : reader.GetDouble(reader.GetOrdinal("PriceBooking")),
                                    datetimeSaisie = reader.GetDateTime(reader.GetOrdinal("datetimeSaisie"))
                                };

                                return Json(new { success = true, info });
                            }
                        }
                    }
                }

                return Json(new { success = true, info = (object?)null });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // POST: Save logistique info
        [HttpPost]
        public async Task<IActionResult> SaveLogistiqueInfo([FromBody] LogistiqueInfoStock model)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    if (model.Id > 0)
                    {
                        // Update existing record
                        var updateQuery = @"
                            UPDATE LogistiqueInfoStock
                            SET DateHeureArrivee = @DateHeureArrivee,
                                DateHeureFin = @DateHeureFin,
                                PriceBooking = @PriceBooking,
                                datetimeSaisie = GETDATE()
                            WHERE id = @Id";

                        using (var command = new SqlCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("@DateHeureArrivee", (object?)model.DateHeureArrivee ?? DBNull.Value);
                            command.Parameters.AddWithValue("@DateHeureFin", (object?)model.DateHeureFin ?? DBNull.Value);
                            command.Parameters.AddWithValue("@PriceBooking", (object?)model.PriceBooking ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Id", model.Id);

                            await command.ExecuteNonQueryAsync();
                        }

                        return Json(new { success = true, message = "Informations mises à jour avec succès!" });
                    }
                    else
                    {
                        // Insert new record
                        var insertQuery = @"
                            INSERT INTO LogistiqueInfoStock (idDepart, DateHeureArrivee, DateHeureFin, PriceBooking, datetimeSaisie)
                            VALUES (@IdDepart, @DateHeureArrivee, @DateHeureFin, @PriceBooking, GETDATE())";

                        using (var command = new SqlCommand(insertQuery, connection))
                        {
                            command.Parameters.AddWithValue("@IdDepart", model.IdDepart);
                            command.Parameters.AddWithValue("@DateHeureArrivee", (object?)model.DateHeureArrivee ?? DBNull.Value);
                            command.Parameters.AddWithValue("@DateHeureFin", (object?)model.DateHeureFin ?? DBNull.Value);
                            command.Parameters.AddWithValue("@PriceBooking", (object?)model.PriceBooking ?? DBNull.Value);

                            await command.ExecuteNonQueryAsync();
                        }

                        return Json(new { success = true, message = "Informations enregistrées avec succès!" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erreur lors de l'enregistrement: {ex.Message}" });
            }
        }

        // GET: Export to Excel
        public async Task<IActionResult> ExportToExcel(DateTime? dateDebut, DateTime? dateFin, int? camionId)
        {
            var transports = await GetTransportData(dateDebut, dateFin, camionId);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("État des Transports");

                // Headers
                var headers = new string[]
                {
                    "ID Départ", "N° Bon", "N° Camion", "Chauffeur", "Type Camion",
                    "Date Départ", "Heure Départ", "Date Réception", "Heure Réception",
                    "Palettes Envoyées", "Palettes Reçues", "Destination",
                    "Date/Heure Arrivée", "Date/Heure Fin",
                    "Durée (h:m)", "État", "Prix Booking (DH)", "Observations"
                };

                // Create headers
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
                foreach (var transport in transports)
                {
                    worksheet.Cell(currentRow, 1).Value = transport.IdDepart;
                    worksheet.Cell(currentRow, 2).Value = transport.NumeroBon ?? "";
                    worksheet.Cell(currentRow, 3).Value = transport.NumeroCamion ?? "";
                    worksheet.Cell(currentRow, 4).Value = transport.Chauffeur ?? "";
                    worksheet.Cell(currentRow, 5).Value = transport.TypeCamion ?? "";

                    if (transport.DateDepart.HasValue)
                    {
                        worksheet.Cell(currentRow, 6).Value = transport.DateDepart.Value;
                        worksheet.Cell(currentRow, 6).Style.DateFormat.Format = "dd/MM/yyyy";
                    }

                    worksheet.Cell(currentRow, 7).Value = transport.HeureDepart ?? "";

                    if (transport.DateReception.HasValue)
                    {
                        worksheet.Cell(currentRow, 8).Value = transport.DateReception.Value;
                        worksheet.Cell(currentRow, 8).Style.DateFormat.Format = "dd/MM/yyyy";
                    }

                    worksheet.Cell(currentRow, 9).Value = transport.HeureReception ?? "";
                    worksheet.Cell(currentRow, 10).Value = transport.NombrePalettes ?? 0;
                    worksheet.Cell(currentRow, 11).Value = transport.NombrePalettesRecues ?? 0;
                    worksheet.Cell(currentRow, 12).Value = transport.Destination ?? "";

                    // Date/Heure Arrivée
                    if (transport.DateHeureArrivee.HasValue)
                    {
                        worksheet.Cell(currentRow, 13).Value = transport.DateHeureArrivee.Value;
                        worksheet.Cell(currentRow, 13).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                    }

                    // Date/Heure Fin
                    if (transport.DateHeureFin.HasValue)
                    {
                        worksheet.Cell(currentRow, 14).Value = transport.DateHeureFin.Value;
                        worksheet.Cell(currentRow, 14).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                    }

                    worksheet.Cell(currentRow, 15).Value = transport.DureeTransfert ?? "0:00";
                    worksheet.Cell(currentRow, 16).Value = transport.EtatTransfert ?? "";

                    if (transport.PriceBooking.HasValue)
                    {
                        worksheet.Cell(currentRow, 17).Value = transport.PriceBooking.Value;
                        worksheet.Cell(currentRow, 17).Style.NumberFormat.Format = "#,##0.00";
                    }

                    worksheet.Cell(currentRow, 18).Value = transport.Observations ?? "";

                    currentRow++;
                }

                // Auto-fit columns
                worksheet.ColumnsUsed().AdjustToContents();

                // Export
                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                string fileName = $"EtatTransports_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // Helper method to check if user is admin
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin";
        }
    }
}
