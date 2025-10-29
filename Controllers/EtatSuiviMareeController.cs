using GESTION_LTIPN.Data;
using GESTION_LTIPN.Models.Stock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using ClosedXML.Excel;

namespace GESTION_LTIPN.Controllers
{
    [Authorize]
    public class EtatSuiviMareeController : Controller
    {
        private readonly StockDbContext _stockContext;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public EtatSuiviMareeController(StockDbContext stockContext, IConfiguration configuration)
        {
            _stockContext = stockContext;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("StockConnection") ?? "";
        }

        // GET: EtatSuiviMaree
        public async Task<IActionResult> Index(DateTime? dateDebut, DateTime? dateFin, string? annee, string? bateauFilter, string? especeFilter, string? numeroBon, string? matriculeCamionFilter)
        {
            var viewModel = new EtatSuiviMareeViewModel
            {
                DateDebut = dateDebut,
                DateFin = dateFin,
                Annee = annee,
                BateauFilter = bateauFilter,
                EspeceFilter = especeFilter,
                NumeroBon = numeroBon,
                MatriculeCamionFilter = matriculeCamionFilter
            };

            // Load filter dropdowns
            viewModel.Bateaux = await GetBateauxList();
            viewModel.Especes = await GetEspecesList();

            // Load matricules list only if any filter is applied (based on filtered results)
            if (dateDebut.HasValue || dateFin.HasValue || !string.IsNullOrEmpty(annee) ||
                !string.IsNullOrEmpty(bateauFilter) || !string.IsNullOrEmpty(especeFilter) || !string.IsNullOrEmpty(numeroBon))
            {
                viewModel.MatriculesCamion = await GetMatriculesCamionList(dateDebut, dateFin, annee, bateauFilter, especeFilter, numeroBon);
            }

            // If filters are applied, load data
            if (dateDebut.HasValue || dateFin.HasValue || !string.IsNullOrEmpty(annee) ||
                !string.IsNullOrEmpty(bateauFilter) || !string.IsNullOrEmpty(especeFilter) || !string.IsNullOrEmpty(numeroBon))
            {
                viewModel.Marees = await GetFilteredMarees(dateDebut, dateFin, annee, bateauFilter, especeFilter, numeroBon, matriculeCamionFilter);
                viewModel.FilteredTitle = BuildFilterTitle(dateDebut, dateFin, annee, bateauFilter, especeFilter, numeroBon, matriculeCamionFilter);
            }

            return View(viewModel);
        }

        private async Task<List<string>> GetBateauxList()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT DISTINCT Bateau FROM SuiveemareeRsw WHERE Bateau IS NOT NULL AND Bateau != '' AND Suppression = 0 ORDER BY Bateau";

                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var bateaux = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        bateaux.Add(reader.GetString(0));
                    }
                    return bateaux;
                }
            }
        }

        private async Task<List<string>> GetEspecesList()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT DISTINCT Espece FROM SuiveemareeRsw WHERE Espece IS NOT NULL AND Espece != '' AND Suppression = 0";

                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var especesSet = new HashSet<string>();
                    while (await reader.ReadAsync())
                    {
                        var especesMultiples = reader.GetString(0);
                        if (!string.IsNullOrEmpty(especesMultiples))
                        {
                            var especes = especesMultiples.Split(',');
                            foreach (var espece in especes)
                            {
                                var especeTrimmed = espece.Trim();
                                if (!string.IsNullOrEmpty(especeTrimmed))
                                {
                                    especesSet.Add(especeTrimmed);
                                }
                            }
                        }
                    }
                    return especesSet.OrderBy(e => e).ToList();
                }
            }
        }

        private async Task<List<string>> GetMatriculesCamionList(DateTime? dateDebut, DateTime? dateFin,
            string? annee, string? bateauFilter, string? especeFilter, string? numeroBon)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var baseQuery = "SELECT DISTINCT MatriculeCamion FROM SuiveemareeRsw WHERE MatriculeCamion IS NOT NULL AND MatriculeCamion != '' AND Suppression = 0";
                var conditions = new List<string>();

                // Apply the same filters as the main query
                if (dateDebut.HasValue && dateFin.HasValue)
                {
                    conditions.Add("dateProductionDebutTraitement BETWEEN @DateDebut AND @DateFin");
                }
                else if (dateDebut.HasValue)
                {
                    conditions.Add("dateProductionDebutTraitement = @DateDebut");
                }

                if (!string.IsNullOrEmpty(annee))
                {
                    conditions.Add("YEAR(dateProductionDebutTraitement) = @Annee");
                }

                if (!string.IsNullOrEmpty(bateauFilter))
                {
                    conditions.Add("Bateau = @Bateau");
                }

                if (!string.IsNullOrEmpty(especeFilter))
                {
                    conditions.Add("(Espece = @Espece OR Espece LIKE @EspeceStart OR Espece LIKE @EspeceMiddle OR Espece LIKE @EspeceEnd)");
                }

                if (!string.IsNullOrEmpty(numeroBon))
                {
                    conditions.Add("numerobon = @NumeroBon");
                }

                var finalQuery = baseQuery;
                if (conditions.Count > 0)
                {
                    finalQuery += " AND " + string.Join(" AND ", conditions);
                }
                finalQuery += " ORDER BY MatriculeCamion";

                using (var command = new SqlCommand(finalQuery, connection))
                {
                    if (dateDebut.HasValue)
                        command.Parameters.AddWithValue("@DateDebut", dateDebut.Value);
                    if (dateFin.HasValue)
                        command.Parameters.AddWithValue("@DateFin", dateFin.Value);
                    if (!string.IsNullOrEmpty(annee))
                        command.Parameters.AddWithValue("@Annee", annee);
                    if (!string.IsNullOrEmpty(bateauFilter))
                        command.Parameters.AddWithValue("@Bateau", bateauFilter);
                    if (!string.IsNullOrEmpty(especeFilter))
                    {
                        command.Parameters.AddWithValue("@Espece", especeFilter);
                        command.Parameters.AddWithValue("@EspeceStart", especeFilter + ",%");
                        command.Parameters.AddWithValue("@EspeceMiddle", "%," + especeFilter + ",%");
                        command.Parameters.AddWithValue("@EspeceEnd", "%," + especeFilter);
                    }
                    if (!string.IsNullOrEmpty(numeroBon))
                        command.Parameters.AddWithValue("@NumeroBon", numeroBon);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var matricules = new List<string>();
                        while (await reader.ReadAsync())
                        {
                            matricules.Add(reader.GetString(0));
                        }
                        return matricules;
                    }
                }
            }
        }

        private async Task<List<MareeItemViewModel>> GetFilteredMarees(DateTime? dateDebut, DateTime? dateFin,
            string? annee, string? bateauFilter, string? especeFilter, string? numeroBon, string? matriculeCamionFilter)
        {
            var marees = new List<MareeItemViewModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var baseQuery = @"
                    SELECT idsvrsw as[ID],numerobon as[Numero Bon],MatriculeCamion as[Matricule Camion],
                    Num_Citerne as[Numero Citerne],Poids,TemperaturePort as[Temperature Port],
                    TemperatureCiterne as[Temperature Citerne],Bateau,maree as[Maree],Cuve,Espece,Observation,
                    FORMAT(DateSortiePort, 'dd/MM/yyyy') as[Date de sortie Port],HeureSortiePort as[Heure de sortie Port],
                    FORMAT(DateArrivage, 'dd/MM/yyyy') as[Date d''arrivée],HeureArrivage as[Heure d''arrivée],
                    ResponsableArrivage as[Responsable d''arrivage],
                    FORMAT(DateDebutDecharge, 'dd/MM/yyyy') as[Date Debut Decharge],HeureDebutDecharge as[Heure Debut Decharge],
                    ResponsableDebutDecha as[Responsable Debut Decharge],SalleDecharge as[Salle],BassinDecharge as[Bassin],
                    FORMAT(DateFinDecharge, 'dd/MM/yyyy') as[Date Fin Decharge],HeureFinDecharge as[Heure Fin Decharge],
                    ResponsableFinDecha as[Responsable Fin Decharge],
                    FORMAT(DateDebutTraitement, 'dd/MM/yyyy') as[Date Debut Traitement],
                    HeureDebutTraitement as[Heure Debut Traitement],ResponsableDebutTrait as[Responsable Debut Traitement],
                    dateProductionDebutTraitement as[Date Debut Traitement / Prod],
                    FORMAT(Date_Fin_Traitement, 'dd/MM/yyyy') as[Date fin Traitement],
                    Heure_Fin_Traitement as[Heure fin Traitement],
                    Responsable_Fin_Traitement as[Responsable fin Traitement],
                    CASE
                        WHEN DateDebutDecharge IS NULL THEN NULL
                        ELSE CONCAT(
                            DATEDIFF(MINUTE, CONCAT(DateArrivage, ' ', HeureArrivage), CONCAT(DateDebutDecharge, ' ', HeureDebutDecharge)) / 60,
                            ':',
                            DATEDIFF(MINUTE, CONCAT(DateArrivage, ' ', HeureArrivage), CONCAT(DateDebutDecharge, ' ', HeureDebutDecharge)) % 60,
                            ':00'
                        )
                    END AS [Temps d''attente Citerne],
                    CASE
                        WHEN DateDebutTraitement IS NULL THEN NULL
                        ELSE CONCAT(
                            DATEDIFF(MINUTE, CONCAT(DateDebutDecharge, ' ', HeureDebutDecharge), CONCAT(DateDebutTraitement, ' ', HeureDebutTraitement)) / 60,
                            ':',
                            DATEDIFF(MINUTE, CONCAT(DateDebutDecharge, ' ', HeureDebutDecharge), CONCAT(DateDebutTraitement, ' ', HeureDebutTraitement)) % 60,
                            ':00'
                        )
                    END AS [Temps d''attente Bassin],
                    CASE
                        WHEN Date_Fin_Traitement IS NULL THEN NULL
                        ELSE CONCAT(
                            DATEDIFF(MINUTE, CONCAT(DateDebutTraitement, ' ', HeureDebutTraitement), CONCAT(Date_Fin_Traitement, ' ', Heure_Fin_Traitement)) / 60,
                            ':',
                            DATEDIFF(MINUTE, CONCAT(DateDebutTraitement, ' ', HeureDebutTraitement), CONCAT(Date_Fin_Traitement, ' ', Heure_Fin_Traitement)) % 60,
                            ':00'
                        )
                    END AS [Temps d''Attente du Traitement]
                    FROM SuiveemareeRsw
                    WHERE Suppression = 0";

                var conditions = new List<string>();

                // Apply filters
                if (dateDebut.HasValue && dateFin.HasValue)
                {
                    conditions.Add("dateProductionDebutTraitement BETWEEN @DateDebut AND @DateFin");
                }
                else if (dateDebut.HasValue)
                {
                    conditions.Add("dateProductionDebutTraitement = @DateDebut");
                }

                if (!string.IsNullOrEmpty(annee))
                {
                    conditions.Add("YEAR(dateProductionDebutTraitement) = @Annee");
                }

                if (!string.IsNullOrEmpty(bateauFilter))
                {
                    conditions.Add("Bateau = @Bateau");
                }

                if (!string.IsNullOrEmpty(especeFilter))
                {
                    conditions.Add("(Espece = @Espece OR Espece LIKE @EspeceStart OR Espece LIKE @EspeceMiddle OR Espece LIKE @EspeceEnd)");
                }

                if (!string.IsNullOrEmpty(numeroBon))
                {
                    conditions.Add("numerobon = @NumeroBon");
                }

                if (!string.IsNullOrEmpty(matriculeCamionFilter))
                {
                    conditions.Add("MatriculeCamion = @MatriculeCamion");
                }

                var finalQuery = baseQuery;
                if (conditions.Count > 0)
                {
                    finalQuery += " AND " + string.Join(" AND ", conditions);
                }
                finalQuery += " ORDER BY DateArrivage DESC";

                using (var command = new SqlCommand(finalQuery, connection))
                {
                    if (dateDebut.HasValue)
                        command.Parameters.AddWithValue("@DateDebut", dateDebut.Value);
                    if (dateFin.HasValue)
                        command.Parameters.AddWithValue("@DateFin", dateFin.Value);
                    if (!string.IsNullOrEmpty(annee))
                        command.Parameters.AddWithValue("@Annee", annee);
                    if (!string.IsNullOrEmpty(bateauFilter))
                        command.Parameters.AddWithValue("@Bateau", bateauFilter);
                    if (!string.IsNullOrEmpty(especeFilter))
                    {
                        command.Parameters.AddWithValue("@Espece", especeFilter);
                        command.Parameters.AddWithValue("@EspeceStart", especeFilter + ",%");
                        command.Parameters.AddWithValue("@EspeceMiddle", "%," + especeFilter + ",%");
                        command.Parameters.AddWithValue("@EspeceEnd", "%," + especeFilter);
                    }
                    if (!string.IsNullOrEmpty(numeroBon))
                        command.Parameters.AddWithValue("@NumeroBon", numeroBon);
                    if (!string.IsNullOrEmpty(matriculeCamionFilter))
                        command.Parameters.AddWithValue("@MatriculeCamion", matriculeCamionFilter);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            marees.Add(new MareeItemViewModel
                            {
                                ID = reader.GetInt32(0),
                                NumeroBon = reader.IsDBNull(1) ? null : reader.GetValue(1)?.ToString(),
                                MatriculeCamion = reader.IsDBNull(2) ? null : reader.GetString(2),
                                NumeroCiterne = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Poids = reader.IsDBNull(4) ? null : Convert.ToDecimal(reader.GetValue(4)),
                                TemperaturePort = reader.IsDBNull(5) ? null : Convert.ToDecimal(reader.GetValue(5)),
                                TemperatureCiterne = reader.IsDBNull(6) ? null : Convert.ToDecimal(reader.GetValue(6)),
                                Bateau = reader.IsDBNull(7) ? null : reader.GetString(7),
                                Maree = reader.IsDBNull(8) ? null : reader.GetString(8),
                                Cuve = reader.IsDBNull(9) ? null : reader.GetString(9),
                                Espece = reader.IsDBNull(10) ? null : reader.GetString(10),
                                Observation = reader.IsDBNull(11) ? null : reader.GetString(11),
                                DateSortiePort = reader.IsDBNull(12) ? null : reader.GetString(12),
                                HeureSortiePort = reader.IsDBNull(13) ? null : reader.GetFieldValue<TimeSpan>(13).ToString(@"hh\:mm"),
                                DateArrivee = reader.IsDBNull(14) ? null : reader.GetString(14),
                                HeureArrivee = reader.IsDBNull(15) ? null : reader.GetFieldValue<TimeSpan>(15).ToString(@"hh\:mm"),
                                ResponsableArrivage = reader.IsDBNull(16) ? null : reader.GetString(16),
                                DateDebutDecharge = reader.IsDBNull(17) ? null : reader.GetString(17),
                                HeureDebutDecharge = reader.IsDBNull(18) ? null : reader.GetFieldValue<TimeSpan>(18).ToString(@"hh\:mm"),
                                ResponsableDebutDecharge = reader.IsDBNull(19) ? null : reader.GetString(19),
                                Salle = reader.IsDBNull(20) ? null : reader.GetString(20),
                                Bassin = reader.IsDBNull(21) ? null : reader.GetString(21),
                                DateFinDecharge = reader.IsDBNull(22) ? null : reader.GetString(22),
                                HeureFinDecharge = reader.IsDBNull(23) ? null : reader.GetFieldValue<TimeSpan>(23).ToString(@"hh\:mm"),
                                ResponsableFinDecharge = reader.IsDBNull(24) ? null : reader.GetString(24),
                                DateDebutTraitement = reader.IsDBNull(25) ? null : reader.GetString(25),
                                HeureDebutTraitement = reader.IsDBNull(26) ? null : reader.GetFieldValue<TimeSpan>(26).ToString(@"hh\:mm"),
                                ResponsableDebutTraitement = reader.IsDBNull(27) ? null : reader.GetString(27),
                                DateDebutTraitementProd = reader.IsDBNull(28) ? null : reader.GetDateTime(28).ToString("dd/MM/yyyy"),
                                DateFinTraitement = reader.IsDBNull(29) ? null : reader.GetString(29),
                                HeureFinTraitement = reader.IsDBNull(30) ? null : reader.GetFieldValue<TimeSpan>(30).ToString(@"hh\:mm"),
                                ResponsableFinTraitement = reader.IsDBNull(31) ? null : reader.GetString(31),
                                TempsAttenteCiterne = reader.IsDBNull(32) ? null : reader.GetString(32),
                                TempsAttenteBassin = reader.IsDBNull(33) ? null : reader.GetString(33),
                                TempsAttenteTraitement = reader.IsDBNull(34) ? null : reader.GetString(34)
                            });
                        }
                    }
                }
            }

            return marees;
        }

        private string BuildFilterTitle(DateTime? dateDebut, DateTime? dateFin, string? annee,
            string? bateauFilter, string? especeFilter, string? numeroBon, string? matriculeCamionFilter)
        {
            var titleParts = new List<string> { "État de suivi des marées RSW" };

            if (dateDebut.HasValue && dateFin.HasValue)
            {
                titleParts.Add($"entre {dateDebut.Value:dd/MM/yyyy} et {dateFin.Value:dd/MM/yyyy}");
            }
            else if (dateDebut.HasValue)
            {
                titleParts.Add($"du {dateDebut.Value:dd/MM/yyyy}");
            }

            if (!string.IsNullOrEmpty(annee))
            {
                titleParts.Add($"Année {annee}");
            }

            if (!string.IsNullOrEmpty(bateauFilter))
            {
                titleParts.Add($"Bateau: {bateauFilter}");
            }

            if (!string.IsNullOrEmpty(especeFilter))
            {
                titleParts.Add($"Espèce: {especeFilter}");
            }

            if (!string.IsNullOrEmpty(numeroBon))
            {
                titleParts.Add($"Bon N° {numeroBon}");
            }

            if (!string.IsNullOrEmpty(matriculeCamionFilter))
            {
                titleParts.Add($"Matricule: {matriculeCamionFilter}");
            }

            return string.Join(" - ", titleParts);
        }

        /* ACTIONS REMOVED - NO LONGER NEEDED
        // GET: EtatSuiviMaree/GetAnalyses/5
        [HttpGet]
        public async Task<IActionResult> GetAnalyses(int id)
        {
            var analyses = new List<AnalyseDetailViewModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"SELECT FORMAT(DateDemande, 'dd/MM/yyyy') as[date demande],HeureDemande as[heure demande],
                            ResponDemande as[Responsable demande],Espece,
                            CAST(ValHistamine AS VARCHAR(15)) + ' ppm' as[Histamine],
                            CAST(ValAbvt AS VARCHAR(15)) + ' mg-N/100g' as[Abvt],
                            ValIF as[IF],Observation,Emplacement,
                            FORMAT(DateAnnalyse, 'dd/MM/yyyy') as[Date d''Analyse],
                            HeureAnnalyse as[Heure d''Analyse],ResponAnnalyse as[Responsable d''analyse]
                            FROM AnalyseRSW WHERE idsvrsw=@Id";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            analyses.Add(new AnalyseDetailViewModel
                            {
                                DateDemande = reader.IsDBNull(0) ? null : reader.GetString(0),
                                HeureDemande = reader.IsDBNull(1) ? null : reader.GetFieldValue<TimeSpan>(1).ToString(@"hh\:mm"),
                                ResponsableDemande = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Espece = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Histamine = reader.IsDBNull(4) ? null : reader.GetString(4),
                                ABVT = reader.IsDBNull(5) ? null : reader.GetString(5),
                                IF = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Observation = reader.IsDBNull(7) ? null : reader.GetString(7),
                                Emplacement = reader.IsDBNull(8) ? null : reader.GetString(8),
                                DateAnalyse = reader.IsDBNull(9) ? null : reader.GetString(9),
                                HeureAnalyse = reader.IsDBNull(10) ? null : reader.GetFieldValue<TimeSpan>(10).ToString(@"hh\:mm"),
                                ResponsableAnalyse = reader.IsDBNull(11) ? null : reader.GetString(11)
                            });
                        }
                    }
                }
            }

            return Json(new { success = true, analyses });
        }

        // POST: EtatSuiviMaree/Delete
        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] int id)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "UPDATE SuiveemareeRsw SET Suppression=1, deletePar=@DeletePar WHERE idsvrsw=@Id";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@DeletePar", userName);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                TempData["SuccessMessage"] = "Ligne supprimée avec succès.";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: EtatSuiviMaree/GetMareeForEdit/5
        [HttpGet]
        public async Task<IActionResult> GetMareeForEdit(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"SELECT idsvrsw, numerobon, MatriculeCamion, Num_Citerne, Poids, TemperaturePort,
                                TemperatureCiterne, Bateau, maree, Cuve, Espece, Observation, SalleDecharge, BassinDecharge,
                                DateSortiePort, HeureSortiePort, DateArrivage, HeureArrivage,
                                DateDebutDecharge, HeureDebutDecharge, DateFinDecharge, HeureFinDecharge,
                                DateDebutTraitement, HeureDebutTraitement, Date_Fin_Traitement, Heure_Fin_Traitement
                                FROM SuiveemareeRsw WHERE idsvrsw=@Id AND Suppression=0";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var model = new
                                {
                                    idSVRSW = reader.GetInt32(0),
                                    numeroBon = reader.IsDBNull(1) ? null : reader.GetValue(1)?.ToString(),
                                    matriculeCamion = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    matriculeCiterne = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    poids = reader.IsDBNull(4) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(4)),
                                    temperaturePort = reader.IsDBNull(5) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(5)),
                                    temperatureCiterne = reader.IsDBNull(6) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(6)),
                                    bateau = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    maree = reader.IsDBNull(8) ? null : reader.GetString(8),
                                    cuve = reader.IsDBNull(9) ? null : reader.GetString(9),
                                    espece = reader.IsDBNull(10) ? null : reader.GetString(10),
                                    observation = reader.IsDBNull(11) ? null : reader.GetString(11),
                                    salle = reader.IsDBNull(12) ? null : reader.GetString(12),
                                    bassin = reader.IsDBNull(13) ? null : reader.GetString(13),
                                    dateSortiePort = reader.IsDBNull(14) ? (DateTime?)null : reader.GetDateTime(14),
                                    heureSortiePort = reader.IsDBNull(15) ? null : reader.GetFieldValue<TimeSpan>(15).ToString(@"hh\:mm"),
                                    dateArrivee = reader.IsDBNull(16) ? (DateTime?)null : reader.GetDateTime(16),
                                    heureArrivee = reader.IsDBNull(17) ? null : reader.GetFieldValue<TimeSpan>(17).ToString(@"hh\:mm"),
                                    dateDebutDecharge = reader.IsDBNull(18) ? (DateTime?)null : reader.GetDateTime(18),
                                    heureDebutDecharge = reader.IsDBNull(19) ? null : reader.GetFieldValue<TimeSpan>(19).ToString(@"hh\:mm"),
                                    dateFinDecharge = reader.IsDBNull(20) ? (DateTime?)null : reader.GetDateTime(20),
                                    heureFinDecharge = reader.IsDBNull(21) ? null : reader.GetFieldValue<TimeSpan>(21).ToString(@"hh\:mm"),
                                    dateDebutTraitement = reader.IsDBNull(22) ? (DateTime?)null : reader.GetDateTime(22),
                                    heureDebutTraitement = reader.IsDBNull(23) ? null : reader.GetFieldValue<TimeSpan>(23).ToString(@"hh\:mm"),
                                    dateFinTraitement = reader.IsDBNull(24) ? (DateTime?)null : reader.GetDateTime(24),
                                    heureFinTraitement = reader.IsDBNull(25) ? null : reader.GetFieldValue<TimeSpan>(25).ToString(@"hh\:mm")
                                };

                                return Json(new { success = true, data = model });
                            }
                        }
                    }
                }

                return Json(new { success = false, message = "Record not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: EtatSuiviMaree/GetBateauxForDropdown
        [HttpGet]
        public async Task<IActionResult> GetBateauxForDropdown()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT NomBateau, NbrQrint FROM Bateau WHERE TypePeche='RSW' ORDER BY NomBateau";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var bateaux = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            bateaux.Add(new
                            {
                                text = reader.GetString(0),
                                value = reader.GetInt32(1).ToString()
                            });
                        }
                        return Json(new { success = true, data = bateaux });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: EtatSuiviMaree/GetEspecesForDropdown
        [HttpGet]
        public async Task<IActionResult> GetEspecesForDropdown()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT NomCategorie FROM CategorieSm ORDER BY NomCategorie";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var especes = new List<string>();
                        while (await reader.ReadAsync())
                        {
                            especes.Add(reader.GetString(0));
                        }
                        return Json(new { success = true, data = especes });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: EtatSuiviMaree/GetSallesForDropdown
        [HttpGet]
        public async Task<IActionResult> GetSallesForDropdown()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT DISTINCT Salle FROM SalleBassin ORDER BY Salle";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var salles = new List<string>();
                        while (await reader.ReadAsync())
                        {
                            salles.Add(reader.GetString(0));
                        }
                        return Json(new { success = true, data = salles });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: EtatSuiviMaree/GetBassinsForDropdown
        [HttpGet]
        public async Task<IActionResult> GetBassinsForDropdown(string salle)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT Bassin FROM SalleBassin WHERE Salle=@Salle ORDER BY Bassin";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Salle", salle);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var bassins = new List<string>();
                            while (await reader.ReadAsync())
                            {
                                bassins.Add(reader.GetString(0));
                            }
                            return Json(new { success = true, data = bassins });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: EtatSuiviMaree/Update
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateMareeViewModel model)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Update query
                    var updateQuery = @"
                        UPDATE SuiveemareeRsw
                        SET numerobon=@NumeroBon, MatriculeCamion=@MatriculeCamion, Num_Citerne=@MatriculeCiterne,
                            Poids=@Poids, TemperaturePort=@TemperaturePort, TemperatureCiterne=@TemperatureCiterne,
                            Bateau=@Bateau, maree=@Maree, Cuve=@Cuve, Espece=@Espece, Observation=@Observation,
                            SalleDecharge=@Salle, BassinDecharge=@Bassin,
                            DateSortiePort=@DateSortiePort, HeureSortiePort=@HeureSortiePort,
                            DateArrivage=@DateArrivee, HeureArrivage=@HeureArrivee,
                            DateDebutDecharge=@DateDebutDecharge, HeureDebutDecharge=@HeureDebutDecharge,
                            DateFinDecharge=@DateFinDecharge, HeureFinDecharge=@HeureFinDecharge,
                            DateDebutTraitement=@DateDebutTraitement, HeureDebutTraitement=@HeureDebutTraitement,
                            Date_Fin_Traitement=@DateFinTraitement, Heure_Fin_Traitement=@HeureFinTraitement
                        WHERE idsvrsw=@Id";

                    using (var command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Id", model.IdSVRSW);
                        command.Parameters.AddWithValue("@NumeroBon", string.IsNullOrEmpty(model.NumeroBon) ? DBNull.Value : model.NumeroBon);
                        command.Parameters.AddWithValue("@MatriculeCamion", (object?)model.MatriculeCamion ?? DBNull.Value);
                        command.Parameters.AddWithValue("@MatriculeCiterne", (object?)model.MatriculeCiterne ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Poids", (object?)model.Poids ?? DBNull.Value);
                        command.Parameters.AddWithValue("@TemperaturePort", (object?)model.TemperaturePort ?? DBNull.Value);
                        command.Parameters.AddWithValue("@TemperatureCiterne", (object?)model.TemperatureCiterne ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Bateau", (object?)model.Bateau ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Maree", (object?)model.Maree ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Cuve", (object?)model.Cuve ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Espece", (object?)model.Espece ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Observation", (object?)model.Observation ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Salle", (object?)model.Salle ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Bassin", (object?)model.Bassin ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DateSortiePort", (object?)model.DateSortiePort ?? DBNull.Value);
                        command.Parameters.AddWithValue("@HeureSortiePort", (object?)model.HeureSortiePort ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DateArrivee", (object?)model.DateArrivee ?? DBNull.Value);
                        command.Parameters.AddWithValue("@HeureArrivee", (object?)model.HeureArrivee ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DateDebutDecharge", (object?)model.DateDebutDecharge ?? DBNull.Value);
                        command.Parameters.AddWithValue("@HeureDebutDecharge", (object?)model.HeureDebutDecharge ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DateFinDecharge", (object?)model.DateFinDecharge ?? DBNull.Value);
                        command.Parameters.AddWithValue("@HeureFinDecharge", (object?)model.HeureFinDecharge ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DateDebutTraitement", (object?)model.DateDebutTraitement ?? DBNull.Value);
                        command.Parameters.AddWithValue("@HeureDebutTraitement", (object?)model.HeureDebutTraitement ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DateFinTraitement", (object?)model.DateFinTraitement ?? DBNull.Value);
                        command.Parameters.AddWithValue("@HeureFinTraitement", (object?)model.HeureFinTraitement ?? DBNull.Value);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true, message = "Modification effectuée avec succès!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        END OF REMOVED ACTIONS */

        // GET: EtatSuiviMaree/ExportExcel
        public async Task<IActionResult> ExportExcel(DateTime? dateDebut, DateTime? dateFin, string? annee,
            string? bateauFilter, string? especeFilter, string? numeroBon, string? matriculeCamionFilter)
        {
            var marees = await GetFilteredMarees(dateDebut, dateFin, annee, bateauFilter, especeFilter, numeroBon, matriculeCamionFilter);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("État Suivi Marée RSW");

                // Headers
                var headers = new[] { "ID", "Numéro Bon", "Matricule Camion", "Numéro Citerne", "Poids",
                    "Temperature Port", "Temperature Citerne", "Bateau", "Marée", "Cuve", "Espèce", "Observation",
                    "Date de sortie Port", "Heure de sortie Port", "Date d'arrivée", "Heure d'arrivée",
                    "Responsable d'arrivage", "Date Debut Decharge", "Heure Debut Decharge",
                    "Responsable Debut Decharge", "Salle", "Bassin", "Date Fin Decharge", "Heure Fin Decharge",
                    "Responsable Fin Decharge", "Date Debut Traitement", "Heure Debut Traitement",
                    "Responsable Debut Traitement", "Date Debut Traitement / Prod", "Date fin Traitement",
                    "Heure fin Traitement", "Responsable fin Traitement", "Temps d'attente Citerne",
                    "Temps d'attente Bassin", "Temps d'Attente du Traitement" };

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
                foreach (var maree in marees)
                {
                    worksheet.Cell(currentRow, 1).Value = maree.ID;
                    worksheet.Cell(currentRow, 2).Value = maree.NumeroBon ?? "";
                    worksheet.Cell(currentRow, 3).Value = maree.MatriculeCamion ?? "";
                    worksheet.Cell(currentRow, 4).Value = maree.NumeroCiterne ?? "";
                    worksheet.Cell(currentRow, 5).Value = maree.Poids ?? 0;
                    worksheet.Cell(currentRow, 6).Value = maree.TemperaturePort ?? 0;
                    worksheet.Cell(currentRow, 7).Value = maree.TemperatureCiterne ?? 0;
                    worksheet.Cell(currentRow, 8).Value = maree.Bateau ?? "";
                    worksheet.Cell(currentRow, 9).Value = maree.Maree ?? "";
                    worksheet.Cell(currentRow, 10).Value = maree.Cuve ?? "";
                    worksheet.Cell(currentRow, 11).Value = maree.Espece ?? "";
                    worksheet.Cell(currentRow, 12).Value = maree.Observation ?? "";
                    worksheet.Cell(currentRow, 13).Value = maree.DateSortiePort ?? "";
                    worksheet.Cell(currentRow, 14).Value = maree.HeureSortiePort ?? "";
                    worksheet.Cell(currentRow, 15).Value = maree.DateArrivee ?? "";
                    worksheet.Cell(currentRow, 16).Value = maree.HeureArrivee ?? "";
                    worksheet.Cell(currentRow, 17).Value = maree.ResponsableArrivage ?? "";
                    worksheet.Cell(currentRow, 18).Value = maree.DateDebutDecharge ?? "";
                    worksheet.Cell(currentRow, 19).Value = maree.HeureDebutDecharge ?? "";
                    worksheet.Cell(currentRow, 20).Value = maree.ResponsableDebutDecharge ?? "";
                    worksheet.Cell(currentRow, 21).Value = maree.Salle ?? "";
                    worksheet.Cell(currentRow, 22).Value = maree.Bassin ?? "";
                    worksheet.Cell(currentRow, 23).Value = maree.DateFinDecharge ?? "";
                    worksheet.Cell(currentRow, 24).Value = maree.HeureFinDecharge ?? "";
                    worksheet.Cell(currentRow, 25).Value = maree.ResponsableFinDecharge ?? "";
                    worksheet.Cell(currentRow, 26).Value = maree.DateDebutTraitement ?? "";
                    worksheet.Cell(currentRow, 27).Value = maree.HeureDebutTraitement ?? "";
                    worksheet.Cell(currentRow, 28).Value = maree.ResponsableDebutTraitement ?? "";
                    worksheet.Cell(currentRow, 29).Value = maree.DateDebutTraitementProd ?? "";
                    worksheet.Cell(currentRow, 30).Value = maree.DateFinTraitement ?? "";
                    worksheet.Cell(currentRow, 31).Value = maree.HeureFinTraitement ?? "";
                    worksheet.Cell(currentRow, 32).Value = maree.ResponsableFinTraitement ?? "";
                    worksheet.Cell(currentRow, 33).Value = maree.TempsAttenteCiterne ?? "";
                    worksheet.Cell(currentRow, 34).Value = maree.TempsAttenteBassin ?? "";
                    worksheet.Cell(currentRow, 35).Value = maree.TempsAttenteTraitement ?? "";

                    currentRow++;
                }

                worksheet.ColumnsUsed().AdjustToContents();

                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var title = BuildFilterTitle(dateDebut, dateFin, annee, bateauFilter, especeFilter, numeroBon, matriculeCamionFilter);
                string fileName = $"{title}_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}
